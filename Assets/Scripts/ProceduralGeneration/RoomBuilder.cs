using System.Collections.Generic;
using UnityEngine;
using BunsKun.Rooms;
using BunsKun.Enemies;
using BunsKun.Ingredients;
using BunsKun.Buns;
using BunsKun.Pickups;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>
    /// Turns a generated room graph (MapGenerator.GeneratedArea) into physical rooms:
    /// floors, walls with door gaps, internal platforms, enemies, hazards and pickups.
    /// Everything is built from primitives at runtime, so there is nothing here that can
    /// end up as a broken prefab or scene reference.
    /// </summary>
    public static class RoomBuilder
    {
        private const float WallThickness = 1f;
        private const float GapSize = 4.2f;
        private static readonly Color WallColor = new Color(0.35f, 0.25f, 0.2f);
        private static readonly Color FloorColor = new Color(0.45f, 0.32f, 0.22f);
        private static readonly Color PlatformColor = new Color(0.6f, 0.45f, 0.3f);
        private static readonly Color BackgroundColor = new Color(0.16f, 0.14f, 0.18f);

        public class BuiltArea
        {
            public GameObject Root;
            public Dictionary<RoomNode, Room> Rooms = new Dictionary<RoomNode, Room>();
            public Vector3 StartSpawnPosition;
        }

        public static BuiltArea Build(MapGenerator.GeneratedArea area, Vector3 worldOrigin, System.Random rng, int areaDepth)
        {
            var built = new BuiltArea();
            built.Root = new GameObject("Area_Depth" + areaDepth);

            foreach (var node in area.Nodes.Values)
            {
                Vector3 roomCenter = worldOrigin + new Vector3(node.GridPos.x * MapGenerator.RoomWidth, node.GridPos.y * MapGenerator.RoomHeight, 0f);
                var room = BuildRoom(node, roomCenter, built.Root.transform, rng, areaDepth);
                built.Rooms[node] = room;

                if (node.Type == RoomType.Start)
                {
                    built.StartSpawnPosition = roomCenter;
                }
            }

            foreach (var edge in area.Edges)
            {
                BuildGapAndBarrier(edge, worldOrigin, built);
            }

            return built;
        }

        private static Room BuildRoom(RoomNode node, Vector3 center, Transform parent, System.Random rng, int areaDepth)
        {
            var roomGO = new GameObject("Room_" + node.Type + "_" + node.GridPos);
            roomGO.transform.SetParent(parent, true);
            roomGO.transform.position = Vector3.zero;

            var room = roomGO.AddComponent<Room>();
            bool requiresClear = node.Type == RoomType.Combat || node.Type == RoomType.Boss || node.Type == RoomType.Optional;
            room.Setup(node, requiresClear);

            // Background panel
            var bg = TerrainBuilder.CreateSolidBlock(center, new Vector2(MapGenerator.RoomWidth, MapGenerator.RoomHeight), roomGO.transform, BackgroundColor, "Background");
            var bgCol = bg.GetComponent<Collider2D>();
            if (bgCol != null) Object.Destroy(bgCol);
            bg.GetComponent<SpriteRenderer>().sortingOrder = -2;

            float halfW = MapGenerator.RoomWidth / 2f;
            float halfH = MapGenerator.RoomHeight / 2f;
            bool hasRight = node.Connections.ContainsKey(Direction.Right);
            bool hasLeft = node.Connections.ContainsKey(Direction.Left);
            bool hasUp = node.Connections.ContainsKey(Direction.Up);
            bool hasDown = node.Connections.ContainsKey(Direction.Down);

            BuildHorizontalWallWithGap(center + new Vector3(0, -halfH, 0), MapGenerator.RoomWidth, hasDown, roomGO.transform, FloorColor, "Floor");
            BuildHorizontalWallWithGap(center + new Vector3(0, halfH, 0), MapGenerator.RoomWidth, hasUp, roomGO.transform, WallColor, "Ceiling");
            BuildVerticalWallWithGap(center + new Vector3(-halfW, 0, 0), MapGenerator.RoomHeight, hasLeft, roomGO.transform, WallColor, "WallLeft");
            BuildVerticalWallWithGap(center + new Vector3(halfW, 0, 0), MapGenerator.RoomHeight, hasRight, roomGO.transform, WallColor, "WallRight");

            float floorY = center.y - halfH + WallThickness / 2f;

            if (hasUp)
            {
                BuildStaircase(roomGO.transform, center, floorY, center.y + halfH - WallThickness / 2f, rng);
            }

            // A couple of extra random platforms for combat variety, avoiding the very center.
            if (node.Type == RoomType.Combat || node.Type == RoomType.Optional)
            {
                int extra = rng.Next(1, 3);
                for (int i = 0; i < extra; i++)
                {
                    float px = center.x + (float)(rng.NextDouble() * (MapGenerator.RoomWidth - 6) - (MapGenerator.RoomWidth - 6) / 2f);
                    float py = floorY + 2f + (float)rng.NextDouble() * 3.5f;
                    TerrainBuilder.CreatePlatform(new Vector3(px, py, 0), 3.5f, roomGO.transform, PlatformColor);
                }
            }

            PopulateRoomContents(node, room, roomGO.transform, center, floorY, rng, areaDepth);

            return room;
        }

        private static void PopulateRoomContents(RoomNode node, Room room, Transform parent, Vector3 center, float floorY, System.Random rng, int areaDepth)
        {
            float hpScale = 1f + areaDepth * 0.35f;
            float dmgScale = 1f + areaDepth * 0.25f;

            switch (node.Type)
            {
                case RoomType.Combat:
                    {
                        var pool = EnemyDatabase.GetForAreaDepth(areaDepth);
                        int count = rng.Next(2, 5);
                        for (int i = 0; i < count; i++)
                        {
                            var data = pool[rng.Next(pool.Count)];
                            Vector3 pos = RandomFloorPosition(center, floorY, rng);
                            var enemy = EnemySpawner.Spawn(data, pos, parent, hpScale, dmgScale);
                            room.RegisterEnemy(enemy);
                        }
                        break;
                    }
                case RoomType.Optional:
                    {
                        var pool = EnemyDatabase.GetForAreaDepth(areaDepth);
                        float eliteHp = node.IsElite ? hpScale * 1.6f : hpScale;
                        float eliteDmg = node.IsElite ? dmgScale * 1.3f : dmgScale;
                        int count = node.IsElite ? rng.Next(2, 4) : rng.Next(1, 3);
                        for (int i = 0; i < count; i++)
                        {
                            var data = pool[rng.Next(pool.Count)];
                            Vector3 pos = RandomFloorPosition(center, floorY, rng);
                            var enemy = EnemySpawner.Spawn(data, pos, parent, eliteHp, eliteDmg);
                            room.RegisterEnemy(enemy);
                        }

                        IngredientData ing = IngredientDatabase.GetRandom(rng);
                        PickupFactory.SpawnIngredientPickup(ing, center + new Vector3(0, 1f, 0), parent);
                        if (node.IsElite)
                        {
                            BunData bun = BunDatabase.GetRandomNonStarter(rng);
                            PickupFactory.SpawnBunPickup(bun, center + new Vector3(1.5f, 1f, 0), parent);
                        }
                        else if (rng.NextDouble() < 0.35)
                        {
                            var hazardGO = new GameObject("Hazard");
                            hazardGO.transform.SetParent(parent, true);
                            hazardGO.transform.position = center + new Vector3((float)(rng.NextDouble() * 4 - 2), floorY + 0.3f, 0);
                            var col = hazardGO.AddComponent<BoxCollider2D>();
                            col.isTrigger = true;
                            col.size = new Vector2(2.5f, 0.6f);
                            var hz = hazardGO.AddComponent<HazardZone>();
                            hz.Configure(6f, 0.7f);
                            var sr = hazardGO.AddComponent<SpriteRenderer>();
                            sr.sprite = BunsKun.Game.SpriteFactory.Triangle(new Color(0.9f, 0.2f, 0.2f));
                            sr.sortingOrder = 2;
                        }
                        break;
                    }
                case RoomType.Reward:
                    {
                        IngredientData ing = IngredientDatabase.GetRandom(rng);
                        PickupFactory.SpawnIngredientPickup(ing, center + new Vector3(-1.2f, 1f, 0), parent);
                        if (rng.NextDouble() < 0.4)
                        {
                            BunData bun = BunDatabase.GetRandomNonStarter(rng);
                            PickupFactory.SpawnBunPickup(bun, center + new Vector3(1.2f, 1f, 0), parent);
                        }
                        break;
                    }
                case RoomType.Boss:
                    {
                        Vector3 pos = center + new Vector3(0, 1.5f, 0);
                        var boss = EnemySpawner.Spawn(EnemyDatabase.Boss, pos, parent, 1f, 1f);
                        room.RegisterEnemy(boss);
                        break;
                    }
            }
        }

        private static Vector3 RandomFloorPosition(Vector3 center, float floorY, System.Random rng)
        {
            float x = center.x + (float)(rng.NextDouble() * (MapGenerator.RoomWidth - 6) - (MapGenerator.RoomWidth - 6) / 2f);
            return new Vector3(x, floorY + 1.5f, 0f);
        }

        private static void BuildStaircase(Transform parent, Vector3 roomCenter, float floorY, float ceilingY, System.Random rng)
        {
            float startY = floorY + 1.6f;
            float targetY = ceilingY - 1.4f;
            int steps = Mathf.Max(2, Mathf.CeilToInt((targetY - startY) / 1.5f));
            float stepHeight = (targetY - startY) / steps;
            float side = rng.NextDouble() < 0.5 ? -1f : 1f;
            float curX = roomCenter.x - side * 4f;

            for (int i = 0; i <= steps; i++)
            {
                float y = startY + stepHeight * i;
                float halfRange = MapGenerator.RoomWidth / 2f - 2.5f;
                float x = Mathf.Clamp(curX, roomCenter.x - halfRange, roomCenter.x + halfRange);
                TerrainBuilder.CreatePlatform(new Vector3(x, y, 0), 3f, parent, PlatformColor);
                curX += side * 2.2f;
                side *= -1f;
            }
        }

        private static void BuildHorizontalWallWithGap(Vector3 center, float fullWidth, bool hasGap, Transform parent, Color color, string label)
        {
            if (!hasGap)
            {
                TerrainBuilder.CreateSolidBlock(center, new Vector2(fullWidth, WallThickness), parent, color, label);
                return;
            }
            float segmentWidth = (fullWidth - GapSize) / 2f;
            TerrainBuilder.CreateSolidBlock(center + new Vector3(-(GapSize / 2f + segmentWidth / 2f), 0, 0), new Vector2(segmentWidth, WallThickness), parent, color, label + "_L");
            TerrainBuilder.CreateSolidBlock(center + new Vector3(GapSize / 2f + segmentWidth / 2f, 0, 0), new Vector2(segmentWidth, WallThickness), parent, color, label + "_R");
        }

        private static void BuildVerticalWallWithGap(Vector3 center, float fullHeight, bool hasGap, Transform parent, Color color, string label)
        {
            if (!hasGap)
            {
                TerrainBuilder.CreateSolidBlock(center, new Vector2(WallThickness, fullHeight), parent, color, label);
                return;
            }
            float segmentHeight = (fullHeight - GapSize) / 2f;
            TerrainBuilder.CreateSolidBlock(center + new Vector3(0, -(GapSize / 2f + segmentHeight / 2f), 0), new Vector2(WallThickness, segmentHeight), parent, color, label + "_B");
            TerrainBuilder.CreateSolidBlock(center + new Vector3(0, GapSize / 2f + segmentHeight / 2f, 0), new Vector2(WallThickness, segmentHeight), parent, color, label + "_T");
        }

        private static void BuildGapAndBarrier(MapGenerator.Edge edge, Vector3 worldOrigin, BuiltArea built)
        {
            if (!edge.Parent.RequiresClearLock()) return;

            Vector3 parentCenter = worldOrigin + new Vector3(edge.Parent.GridPos.x * MapGenerator.RoomWidth, edge.Parent.GridPos.y * MapGenerator.RoomHeight, 0f);
            Vector3 gapCenter = GapWorldPosition(parentCenter, edge.DirFromParent);
            Vector2 gapSize = (edge.DirFromParent == Direction.Left || edge.DirFromParent == Direction.Right)
                ? new Vector2(WallThickness * 1.2f, GapSize)
                : new Vector2(GapSize, WallThickness * 1.2f);

            var parentRoom = built.Rooms[edge.Parent];
            var barrier = TerrainBuilder.CreateSolidBlock(gapCenter, gapSize, parentRoom.transform, new Color(0.2f, 0.2f, 0.25f, 0.9f), "LockBarrier");
            parentRoom.AddLockBarrier(barrier);
        }

        private static Vector3 GapWorldPosition(Vector3 roomCenter, Direction dir)
        {
            float halfW = MapGenerator.RoomWidth / 2f;
            float halfH = MapGenerator.RoomHeight / 2f;
            switch (dir)
            {
                case Direction.Right: return roomCenter + new Vector3(halfW, 0, 0);
                case Direction.Left: return roomCenter + new Vector3(-halfW, 0, 0);
                case Direction.Up: return roomCenter + new Vector3(0, halfH, 0);
                default: return roomCenter + new Vector3(0, -halfH, 0);
            }
        }
    }
}
