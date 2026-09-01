using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BunsKun.Rooms;
using BunsKun.Enemies;
using BunsKun.Ingredients;
using BunsKun.Buns;
using BunsKun.Upgrades;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>
    /// Builds one complete layer of the dungeon from a single seed, in stages:
    ///
    ///   1. room graph            (MapGenerator)
    ///   2. cave carving          cellular automata + chambers + ground, per room
    ///   2b. skeleton             doorways, room clearings, walkable chain through each room
    ///   3. platforms             scaffolding, guaranteed open volume and floor
    ///   4. reachability + repair measured with the player's real movement limits
    ///   5-7. entities            enemies, pickups and room rewards on reachable ground
    ///   8. validation            warnings if anything is still wrong
    ///
    /// The same seed always produces the same layer: every random decision comes from the
    /// System.Random created here, and repairs are deterministic rather than re-rolls.
    /// </summary>
    public static class LayerGenerator
    {
        private const int MinAirRegion = 20;
        private const int MinRoomFloorCells = 25;
        private const int MinRoomOpenTiles = 140;
        private const int RepairBudget = 60;

        public static LayerPlan Generate(int seed, int depth, bool isFinalLayer, int mainPathLength,
            int optionalBranches, PlayerMovementProfile profile)
        {
            var rng = new System.Random(seed);
            var plan = new LayerPlan
            {
                Seed = seed,
                Depth = depth,
                IsFinalLayer = isFinalLayer
            };

            // ---------------------------------------------------------- stage 1
            plan.Graph = MapGenerator.Generate(rng, mainPathLength, isFinalLayer, optionalBranches);

            int minGX = plan.Graph.Nodes.Keys.Min(k => k.x);
            int minGY = plan.Graph.Nodes.Keys.Min(k => k.y);
            int maxGX = plan.Graph.Nodes.Keys.Max(k => k.x);
            int maxGY = plan.Graph.Nodes.Keys.Max(k => k.y);

            int width = (maxGX - minGX + 1) * MapGenerator.RoomWidthTiles;
            int height = (maxGY - minGY + 1) * MapGenerator.RoomHeightTiles;
            plan.Tiles = new TileMap(width, height, Vector2.zero);

            foreach (var node in OrderedNodes(plan.Graph))
            {
                int x0 = (node.GridPos.x - minGX) * MapGenerator.RoomWidthTiles;
                int y0 = (node.GridPos.y - minGY) * MapGenerator.RoomHeightTiles;
                var bounds = new RectInt(x0, y0, MapGenerator.RoomWidthTiles - 1, MapGenerator.RoomHeightTiles - 1);
                plan.Rooms[node] = new RoomPlan
                {
                    Node = node,
                    Bounds = bounds,
                    Interior = new RectInt(x0 + 1, y0 + 1, bounds.width - 2, bounds.height - 2),
                    Hub = new Vector2Int(x0 + MapGenerator.RoomWidthTiles / 2, y0 + 6)
                };
            }

            var rooms = plan.Rooms.Values.OrderBy(r => r.Node.GridPos.y).ThenBy(r => r.Node.GridPos.x).ToList();

            // ---------------------------------------------------------- stage 2
            foreach (var room in rooms) CarveRoomCave(plan.Tiles, rng, room, profile);
            foreach (var room in rooms) SealRoomBorder(plan.Tiles, room);
            LockWorldBoundary(plan.Tiles);

            CarveDoors(plan, rng);
            CarveSkeleton(plan, rng);

            // ---------------------------------------------------------- stage 3
            GuaranteeRoomSpace(plan, rooms, profile);

            // ---------------------------------------------------------- stage 4
            var analyzer = new ReachabilityAnalyzer(plan.Tiles, profile);
            var startRoom = plan.Rooms[plan.Graph.Start];
            var spawn = analyzer.SnapToStanding(startRoom.Hub);
            var goalRoomPlan = plan.Rooms[plan.Graph.End];
            var goal = analyzer.SnapToStanding(goalRoomPlan.Hub);
            if (spawn == null || goal == null)
            {
                // Should not happen: the hub clearing always leaves floor. Fail loudly but safely.
                plan.ValidationWarnings.Add("no standing cell at spawn or goal; carving a fallback floor");
                CarveFallbackFloor(plan.Tiles, startRoom);
                CarveFallbackFloor(plan.Tiles, goalRoomPlan);
                spawn = analyzer.SnapToStanding(startRoom.Hub) ?? startRoom.Hub;
                goal = analyzer.SnapToStanding(goalRoomPlan.Hub) ?? goalRoomPlan.Hub;
            }
            plan.SpawnTile = spawn.Value;
            plan.GoalTile = goal.Value;
            plan.GoalRoom = plan.Graph.End;

            var reachable = RepairUntilReachable(plan, analyzer, rooms);

            // ---------------------------------------------------------- stages 5-7
            var flyReachable = analyzer.ComputeReachable(plan.SpawnTile, true);
            PlaceEntities(plan, rng, rooms, reachable, flyReachable, analyzer);

            // ---------------------------------------------------------- stage 8
            Validate(plan, analyzer, rooms, reachable, flyReachable);
            return plan;
        }

        private static IEnumerable<RoomNode> OrderedNodes(MapGenerator.GeneratedArea graph)
        {
            return graph.Nodes.OrderBy(kv => kv.Key.y).ThenBy(kv => kv.Key.x).Select(kv => kv.Value);
        }

        // ------------------------------------------------------------ stage 2

        private static void CarveRoomCave(TileMap tiles, System.Random rng, RoomPlan room,
            PlayerMovementProfile profile)
        {
            RectInt interior = room.Interior;
            int w = interior.width + 1;
            int h = interior.height + 1;

            bool[,] cave = TerrainCarver.CellularCave(rng, w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!cave[x, y]) tiles.Carve(interior.xMin + x, interior.yMin + y);
                }
            }

            // A few chambers: the big halls and small pockets that make a cave read as a cave.
            int chambers = rng.Next(2, 5);
            for (int i = 0; i < chambers; i++)
            {
                int cx = rng.Next(interior.xMin + 5, interior.xMax - 4);
                int cy = rng.Next(interior.yMin + 4, interior.yMax - 2);
                TerrainCarver.CarveEllipse(tiles, cx, cy, rng.Next(3, 9), rng.Next(2, 6), interior);
            }

            // Rolling ground along the bottom of the room.
            int level = rng.Next(2, 5);
            for (int x = interior.xMin; x <= interior.xMax; x++)
            {
                level = Mathf.Clamp(level + rng.Next(-1, 2), 1, 6);
                for (int y = room.Bounds.yMin; y <= room.Bounds.yMin + level; y++)
                {
                    tiles.Fill(x, y, TileKind.Rock);
                }
            }

            // Drop unreachable pockets, connect the larger ones into one open volume.
            var regions = TerrainCarver.AirRegions(tiles, interior);
            if (regions.Count > 0)
            {
                regions.Sort((a, b) => b.Count.CompareTo(a.Count));
                var main = regions[0];
                for (int i = 1; i < regions.Count; i++)
                {
                    var region = regions[i];
                    if (region.Count < MinAirRegion)
                    {
                        foreach (var c in region) tiles.Fill(c.x, c.y, TileKind.Rock);
                        continue;
                    }
                    Vector2Int src = region[0];
                    Vector2Int dst = main[0];
                    int best = int.MaxValue;
                    foreach (var a in region)
                    {
                        foreach (var b in main)
                        {
                            int dist = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                            if (dist < best) { best = dist; src = a; dst = b; }
                        }
                    }
                    TerrainCarver.ConnectCells(tiles, src, dst, interior);
                }
            }

            TerrainCarver.ScaffoldLedges(tiles, rng, interior, profile.MaxStepUpTiles);
        }

        private static void SealRoomBorder(TileMap tiles, RoomPlan room)
        {
            for (int x = room.Bounds.xMin; x <= room.Bounds.xMax; x++)
            {
                tiles.Fill(x, room.Bounds.yMin);
                tiles.Fill(x, room.Bounds.yMax);
            }
            for (int y = room.Bounds.yMin; y <= room.Bounds.yMax; y++)
            {
                tiles.Fill(room.Bounds.xMin, y);
                tiles.Fill(room.Bounds.xMax, y);
            }
        }

        /// <summary>The outer frame is locked solid so nothing can ever open a hole out of the world.</summary>
        private static void LockWorldBoundary(TileMap tiles)
        {
            for (int x = 0; x < tiles.Width; x++)
            {
                for (int y = 0; y < tiles.Height; y++)
                {
                    if (x < 2 || y < 2 || x >= tiles.Width - 2 || y >= tiles.Height - 2)
                    {
                        tiles.Fill(x, y, TileKind.Rock, true);
                    }
                }
            }
        }

        private static void CarveDoors(LayerPlan plan, System.Random rng)
        {
            var tiles = plan.Tiles;
            foreach (var edge in plan.Graph.Edges)
            {
                var parent = plan.Rooms[edge.Parent];
                var child = plan.Rooms[edge.Child];
                var door = new DoorPlan
                {
                    Parent = edge.Parent,
                    Child = edge.Child,
                    DirFromParent = edge.DirFromParent
                };

                if (edge.DirFromParent == Direction.Left || edge.DirFromParent == Direction.Right)
                {
                    bool toRight = edge.DirFromParent == Direction.Right;
                    int bx = toRight ? parent.Bounds.xMax : parent.Bounds.xMin;
                    int doorY = parent.Bounds.yMin + rng.Next(3, 9);

                    for (int x = bx - 3; x <= bx + 3; x++)
                    {
                        for (int k = 0; k < 4; k++) tiles.Carve(x, doorY + k, true);
                        tiles.Fill(x, doorY - 1, TileKind.Built, true);
                    }

                    door.BarrierRect = new RectInt(bx, doorY, 0, 3);
                    door.ParentAnchor = new Vector2Int(toRight ? bx - 4 : bx + 4, doorY);
                    door.ChildAnchor = new Vector2Int(toRight ? bx + 4 : bx - 4, doorY);
                }
                else
                {
                    // Stacked rooms are joined by a switchback ramp cut through the shared floor,
                    // so the player can walk down it and climb back up it.
                    bool parentIsUpper = edge.DirFromParent == Direction.Down;
                    var upper = parentIsUpper ? parent : child;
                    var lower = parentIsUpper ? child : parent;

                    int cx = upper.Bounds.xMin + rng.Next(9, MapGenerator.RoomWidthTiles - 9);
                    int yLow = lower.Bounds.yMax - 7;
                    int yHigh = upper.Bounds.yMin + 7;
                    int xLo = Mathf.Max(upper.Bounds.xMin + 2, cx - 6);
                    int xHi = Mathf.Min(upper.Bounds.xMax - 2, cx + 6);

                    var ramp = TerrainCarver.SwitchbackCells(xLo, xHi, yLow, yHigh, cx);
                    TerrainCarver.CarveCorridor(tiles, ramp, true);

                    int boundaryY = upper.Bounds.yMin;
                    int minX = int.MaxValue, maxX = int.MinValue;
                    foreach (var c in ramp)
                    {
                        if (c.y >= boundaryY - 1 && c.y <= boundaryY + 1)
                        {
                            minX = Mathf.Min(minX, c.x);
                            maxX = Mathf.Max(maxX, c.x);
                        }
                    }
                    if (minX > maxX) { minX = xLo; maxX = xHi; }
                    door.BarrierRect = new RectInt(minX, boundaryY - 1, maxX - minX, 3);

                    Vector2Int upperAnchor = ramp[ramp.Count - 1];
                    Vector2Int lowerAnchor = ramp[0];
                    door.ParentAnchor = parentIsUpper ? upperAnchor : lowerAnchor;
                    door.ChildAnchor = parentIsUpper ? lowerAnchor : upperAnchor;
                }

                plan.Doors.Add(door);
            }
        }

        /// <summary>Records a doorway anchor against its room, clamped inside that room.</summary>
        private static void AddAnchor(LayerPlan plan, Dictionary<RoomNode, List<Vector2Int>> anchorsByRoom,
            RoomNode node, Vector2Int anchor)
        {
            RectInt rect = plan.Rooms[node].Interior;
            anchorsByRoom[node].Add(new Vector2Int(
                Mathf.Clamp(anchor.x, rect.xMin, rect.xMax),
                Mathf.Clamp(anchor.y, rect.yMin, rect.yMax)));
        }

        private static void CarveSkeleton(LayerPlan plan, System.Random rng)
        {
            var tiles = plan.Tiles;
            var anchorsByRoom = new Dictionary<RoomNode, List<Vector2Int>>();
            foreach (var room in plan.Rooms.Keys) anchorsByRoom[room] = new List<Vector2Int>();

            foreach (var door in plan.Doors)
            {
                AddAnchor(plan, anchorsByRoom, door.Parent, door.ParentAnchor);
                AddAnchor(plan, anchorsByRoom, door.Child, door.ChildAnchor);
            }

            foreach (var room in plan.Rooms.Values.OrderBy(r => r.Node.GridPos.y).ThenBy(r => r.Node.GridPos.x))
            {
                RectInt rect = room.Interior;

                // Every room gets a real clearing with a floor: an arena to fight in.
                TerrainCarver.CarveEllipse(tiles, room.Hub.x, room.Hub.y + 3, 8, 4, rect);
                for (int i = -8; i <= 8; i++)
                {
                    int fx = room.Hub.x + i;
                    if (fx >= rect.xMin && fx <= rect.xMax) tiles.Fill(fx, room.Hub.y - 1, TileKind.Rock);
                }
                var hubStrip = new List<Vector2Int>();
                for (int i = -7; i <= 7; i++) hubStrip.Add(new Vector2Int(room.Hub.x + i, room.Hub.y));
                TerrainCarver.CarveCorridor(tiles, hubStrip);

                // Walkable chain: hub -> every doorway of this room.
                var chain = new List<Vector2Int> { room.Hub };
                chain.AddRange(anchorsByRoom[room.Node].OrderBy(a => a.x).ThenBy(a => a.y));
                for (int i = 0; i + 1 < chain.Count; i++)
                {
                    TerrainCarver.ConnectCells(tiles, chain[i], chain[i + 1], rect);
                }
            }
        }

        // ------------------------------------------------------------ stage 3

        private static void GuaranteeRoomSpace(LayerPlan plan, List<RoomPlan> rooms,
            PlayerMovementProfile profile)
        {
            var tiles = plan.Tiles;
            var analyzer = new ReachabilityAnalyzer(tiles, profile);

            foreach (var room in rooms)
            {
                RectInt rect = room.Interior;

                for (int extra = 0; extra < 4; extra++)
                {
                    if (CountOpenTiles(tiles, rect) >= MinRoomOpenTiles) break;
                    int offset = extra % 2 == 0 ? -6 : 6;
                    TerrainCarver.CarveEllipse(tiles, room.Hub.x + offset, room.Hub.y + 4 + extra * 2,
                        7, 4, rect);
                }

                int attempts = 0;
                while (analyzer.StandingCellsIn(rect).Count < MinRoomFloorCells && attempts < 4)
                {
                    attempts++;
                    int y = rect.yMin + 3 + attempts * 4;
                    if (y >= rect.yMax - 1) break;
                    int xFrom = rect.xMin + 2 + (attempts % 2) * 6;
                    int xTo = Mathf.Min(rect.xMax - 2, xFrom + 14);
                    for (int x = xFrom; x <= xTo; x++)
                    {
                        tiles.Fill(x, y, TileKind.Rock);
                        tiles.Carve(x, y + 1);
                        tiles.Carve(x, y + 2);
                    }
                }
            }
        }

        private static int CountOpenTiles(TileMap tiles, RectInt rect)
        {
            int count = 0;
            for (int y = rect.yMin; y <= rect.yMax; y++)
            {
                for (int x = rect.xMin; x <= rect.xMax; x++)
                {
                    if (tiles.IsAir(x, y)) count++;
                }
            }
            return count;
        }

        private static void CarveFallbackFloor(TileMap tiles, RoomPlan room)
        {
            RectInt rect = room.Interior;
            int y = room.Hub.y;
            for (int x = rect.xMin + 2; x <= rect.xMax - 2; x++)
            {
                tiles.Fill(x, y - 1, TileKind.Built);
                tiles.Carve(x, y);
                tiles.Carve(x, y + 1);
                tiles.Carve(x, y + 2);
            }
        }

        // ------------------------------------------------------------ stage 4

        /// <summary>
        /// Bookkeeping for the repair pass: carving a connection changes what is reachable,
        /// so the reachable set and the remaining budget travel together.
        /// </summary>
        private class RepairState
        {
            public LayerPlan Plan;
            public ReachabilityAnalyzer Analyzer;
            public HashSet<Vector2Int> Reachable;
            public int Budget;

            /// <summary>Carves a traversable connection from the closest reachable cell to a target.</summary>
            public void Bridge(Vector2Int target, RectInt rect)
            {
                if (Budget <= 0) return;

                List<Vector2Int> pool = SortedCells(Reachable, rect);
                if (pool.Count == 0) pool = SortedCells(Reachable, null);
                if (pool.Count == 0) return;

                Vector2Int src = pool[0];
                int best = int.MaxValue;
                for (int i = 0; i < pool.Count; i++)
                {
                    int distance = Mathf.Abs(pool[i].x - target.x) + Mathf.Abs(pool[i].y - target.y);
                    if (distance < best)
                    {
                        best = distance;
                        src = pool[i];
                    }
                }

                TerrainCarver.ConnectCells(Plan.Tiles, src, target, rect);
                Plan.RepairCount++;
                Budget--;
                Reachable = Analyzer.ComputeReachable(Plan.SpawnTile, false);
            }
        }

        private static HashSet<Vector2Int> RepairUntilReachable(LayerPlan plan,
            ReachabilityAnalyzer analyzer, List<RoomPlan> rooms)
        {
            // Points that MUST be reachable by jumping alone - no jetpack assumed.
            var required = new List<KeyValuePair<Vector2Int, RoomPlan>>();
            foreach (var door in plan.Doors)
            {
                required.Add(new KeyValuePair<Vector2Int, RoomPlan>(door.ParentAnchor, plan.Rooms[door.Parent]));
                required.Add(new KeyValuePair<Vector2Int, RoomPlan>(door.ChildAnchor, plan.Rooms[door.Child]));
            }
            required.Add(new KeyValuePair<Vector2Int, RoomPlan>(plan.GoalTile, plan.Rooms[plan.GoalRoom]));

            var state = new RepairState
            {
                Plan = plan,
                Analyzer = analyzer,
                Reachable = analyzer.ComputeReachable(plan.SpawnTile, false),
                Budget = RepairBudget
            };

            // Repair inside the owning room first, which keeps the caves intact.
            for (int attempt = 0; attempt < 30 && state.Budget > 0; attempt++)
            {
                int index = FirstUnreachable(required, analyzer, state.Reachable);
                if (index < 0) break;
                Vector2Int anchor = required[index].Key;
                Vector2Int? snapped = analyzer.SnapToStanding(anchor);
                state.Bridge(snapped ?? anchor, required[index].Value.Interior);
            }

            // Last resort: connect straight through, ignoring room bounds. ConnectCells can
            // link any two cells, so this always terminates the guarantee.
            var worldRect = new RectInt(2, 2, plan.Tiles.Width - 5, plan.Tiles.Height - 5);
            for (int attempt = 0; attempt < 12 && state.Budget > 0; attempt++)
            {
                int index = FirstUnreachable(required, analyzer, state.Reachable);
                if (index < 0) break;
                Vector2Int anchor = required[index].Key;
                Vector2Int? snapped = analyzer.SnapToStanding(anchor);
                state.Bridge(snapped ?? anchor, worldRect);
            }

            // Every room needs some walkable floor for enemies and pickups to sit on.
            // Targets are spread across the room so one bridge does not just re-open the
            // same corner over and over.
            const int coverageAttempts = 6;
            foreach (var room in rooms)
            {
                for (int attempt = 0; attempt < coverageAttempts && state.Budget > 0; attempt++)
                {
                    if (SortedCells(state.Reachable, room.Interior).Count >= MinRoomFloorCells) break;

                    var candidates = new List<Vector2Int>();
                    foreach (var cell in analyzer.StandingCellsIn(room.Interior))
                    {
                        if (!state.Reachable.Contains(cell)) candidates.Add(cell);
                    }
                    if (candidates.Count == 0) break;

                    int pick = (candidates.Count - 1) * attempt / Mathf.Max(1, coverageAttempts - 1);
                    state.Bridge(candidates[pick], room.Interior);
                }
            }

            return state.Reachable;
        }

        /// <summary>Index of the first required point that is not reachable yet, or -1.</summary>
        private static int FirstUnreachable(List<KeyValuePair<Vector2Int, RoomPlan>> required,
            ReachabilityAnalyzer analyzer, HashSet<Vector2Int> reachable)
        {
            for (int i = 0; i < required.Count; i++)
            {
                if (!analyzer.AreaReachable(required[i].Key, reachable)) return i;
            }
            return -1;
        }

        /// <summary>Reachable cells in a stable order - never iterate a HashSet for decisions.</summary>
        private static List<Vector2Int> SortedCells(HashSet<Vector2Int> cells, RectInt? rect)
        {
            IEnumerable<Vector2Int> source = cells;
            if (rect.HasValue)
            {
                RectInt r = rect.Value;
                source = source.Where(c => c.x >= r.xMin && c.x <= r.xMax && c.y >= r.yMin && c.y <= r.yMax);
            }
            return source.OrderBy(c => c.x).ThenBy(c => c.y).ToList();
        }

        // ---------------------------------------------------------- stages 5-7

        private static void PlaceEntities(LayerPlan plan, System.Random rng, List<RoomPlan> rooms,
            HashSet<Vector2Int> reachable, HashSet<Vector2Int> flyReachable, ReachabilityAnalyzer analyzer)
        {
            float hpScale = 1f + plan.Depth * 0.28f;
            float dmgScale = 1f + plan.Depth * 0.2f;
            var enemyPool = EnemyDatabase.GetForAreaDepth(plan.Depth);

            foreach (var room in rooms)
            {
                var rect = new RectInt(room.Bounds.xMin + 2, room.Bounds.yMin + 1,
                    room.Bounds.width - 4, room.Bounds.height - 3);
                List<Vector2Int> walkable = SortedCells(reachable, rect);
                List<Vector2Int> explorable = SortedCells(flyReachable, rect);
                if (walkable.Count == 0) continue;
                if (explorable.Count == 0) explorable = walkable;

                RoomNode node = room.Node;
                if (node.Type == RoomType.Combat)
                {
                    AddEnemies(plan, rng, node, enemyPool, walkable, rng.Next(2, 5), hpScale, dmgScale);
                }
                else if (node.Type == RoomType.Optional)
                {
                    float eliteHp = node.IsElite ? hpScale * 1.6f : hpScale;
                    float eliteDmg = node.IsElite ? dmgScale * 1.3f : dmgScale;
                    int count = node.IsElite ? rng.Next(2, 4) : rng.Next(1, 3);
                    AddEnemies(plan, rng, node, enemyPool, walkable, count, eliteHp, eliteDmg);

                    plan.Pickups.Add(new PickupPlacement
                    {
                        Room = node,
                        Ingredient = PickIngredient(rng),
                        Tile = explorable[rng.Next(explorable.Count)]
                    });

                    if (node.IsElite)
                    {
                        plan.Pickups.Add(new PickupPlacement
                        {
                            Room = node,
                            Bun = BunDatabase.GetRandomNonStarter(rng),
                            Tile = walkable[rng.Next(walkable.Count)]
                        });
                    }
                    else if (rng.NextDouble() < 0.35)
                    {
                        plan.Hazards.Add(new HazardPlacement
                        {
                            Room = node,
                            Tile = walkable[rng.Next(walkable.Count)],
                            Width = 3
                        });
                    }
                }
                else if (node.Type == RoomType.Reward)
                {
                    plan.Pickups.Add(new PickupPlacement
                    {
                        Room = node,
                        Ingredient = PickIngredient(rng),
                        Tile = walkable[rng.Next(walkable.Count)]
                    });
                    if (rng.NextDouble() < 0.4)
                    {
                        plan.Pickups.Add(new PickupPlacement
                        {
                            Room = node,
                            Bun = BunDatabase.GetRandomNonStarter(rng),
                            Tile = walkable[rng.Next(walkable.Count)]
                        });
                    }
                }
                else if (node.Type == RoomType.Start)
                {
                    // The first room always hands out an ingredient so a build starts forming early.
                    plan.Pickups.Add(new PickupPlacement
                    {
                        Room = node,
                        Ingredient = PickIngredient(rng),
                        Tile = walkable[rng.Next(walkable.Count)]
                    });
                }

                if (node.RequiresClearLock() && node.Type != RoomType.Boss)
                {
                    plan.RoomRewards[node] = UpgradeDatabase.RollChoices(rng, 3);
                }
            }
        }

        private static void AddEnemies(LayerPlan plan, System.Random rng, RoomNode node,
            List<EnemyData> pool, List<Vector2Int> cells, int count, float hpScale, float dmgScale)
        {
            if (pool.Count == 0 || cells.Count == 0) return;
            for (int i = 0; i < count; i++)
            {
                plan.Enemies.Add(new EnemyPlacement
                {
                    Room = node,
                    Data = pool[rng.Next(pool.Count)],
                    Tile = cells[rng.Next(cells.Count)],
                    HealthScale = hpScale,
                    DamageScale = dmgScale
                });
            }
        }

        private static IngredientData PickIngredient(System.Random rng)
        {
            return IngredientDatabase.GetRandomFindable(rng);
        }

        // ------------------------------------------------------------ stage 8

        private static void Validate(LayerPlan plan, ReachabilityAnalyzer analyzer, List<RoomPlan> rooms,
            HashSet<Vector2Int> reachable, HashSet<Vector2Int> flyReachable)
        {
            foreach (var door in plan.Doors)
            {
                if (!analyzer.AreaReachable(door.ParentAnchor, reachable))
                    plan.ValidationWarnings.Add($"door anchor unreachable at {door.ParentAnchor}");
                if (!analyzer.AreaReachable(door.ChildAnchor, reachable))
                    plan.ValidationWarnings.Add($"door anchor unreachable at {door.ChildAnchor}");
            }
            if (!analyzer.AreaReachable(plan.GoalTile, reachable))
                plan.ValidationWarnings.Add("goal (exit/boss) unreachable by jumping");

            foreach (var enemy in plan.Enemies)
            {
                if (!reachable.Contains(enemy.Tile))
                    plan.ValidationWarnings.Add($"enemy on unreachable ground {enemy.Tile}");
            }
            foreach (var pickup in plan.Pickups)
            {
                if (!flyReachable.Contains(pickup.Tile))
                    plan.ValidationWarnings.Add($"pickup unreachable {pickup.Tile}");
            }
            foreach (var room in rooms)
            {
                int walkable = SortedCells(reachable, room.Interior).Count;
                if (walkable < 10)
                    plan.ValidationWarnings.Add($"room {room.Node.GridPos} {room.Node.Type}: only {walkable} walkable cells");
            }
        }
    }
}
