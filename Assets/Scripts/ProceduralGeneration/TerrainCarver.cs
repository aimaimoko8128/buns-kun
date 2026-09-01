using System;
using System.Collections.Generic;
using UnityEngine;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>
    /// The terrain primitives every generation stage is built from.
    ///
    /// Two rules make the output reliably traversable:
    ///   1. A connection is carved in ONE pass - all air first, floors afterwards - so one
    ///      part of a path can never remove the floor another part stands on.
    ///   2. Vertical connections are switchback ramps whose legs are 1:1 slopes sharing
    ///      their turning cell, so they can be walked up, not only fallen down.
    /// </summary>
    public static class TerrainCarver
    {
        public const int CorridorHeight = 3;

        // ------------------------------------------------------------------ paths

        /// <summary>
        /// A path that changes height by at most one tile per horizontal step and visits
        /// each column at most once. Requires |dy| &lt;= |dx| to stay a pure slope.
        /// </summary>
        public static List<Vector2Int> SlopePath(Vector2Int start, Vector2Int goal)
        {
            var cells = new List<Vector2Int> { start };
            int x = start.x, y = start.y;
            int guard = 0;
            while ((x != goal.x || y != goal.y) && guard < 4000)
            {
                guard++;
                if (x != goal.x)
                {
                    x += goal.x > x ? 1 : -1;
                    if (y != goal.y) y += goal.y > y ? 1 : -1;
                }
                else if (y != goal.y)
                {
                    y += goal.y > y ? 1 : -1;
                }
                cells.Add(new Vector2Int(x, y));
            }
            return cells;
        }

        /// <summary>
        /// Cells of a zig-zag ramp climbing from yLow to yHigh inside a horizontal span.
        /// Consecutive legs share their turning cell, so the ramp is continuous.
        /// </summary>
        public static List<Vector2Int> SwitchbackCells(int xLo, int xHi, int yLow, int yHigh, int startX)
        {
            if (xLo > xHi) (xLo, xHi) = (xHi, xLo);
            if (xHi - xLo < 3) xHi = xLo + 3;

            int span = xHi - xLo;
            int x = Mathf.Clamp(startX, xLo, xHi);
            int y = yLow;
            int direction = x <= (xLo + xHi) / 2 ? 1 : -1;

            var cells = new List<Vector2Int> { new Vector2Int(x, y) };
            int guard = 0;
            while (y < yHigh && guard < 64)
            {
                guard++;
                int leg = Mathf.Min(span, yHigh - y);
                int endX = x + direction * leg;
                if (endX < xLo || endX > xHi)
                {
                    direction = -direction;
                    endX = Mathf.Clamp(x + direction * leg, xLo, xHi);
                    leg = Mathf.Abs(endX - x);
                    if (leg == 0) break;
                }
                var legCells = SlopePath(new Vector2Int(x, y), new Vector2Int(endX, y + leg));
                for (int i = 1; i < legCells.Count; i++) cells.Add(legCells[i]);
                x = endX;
                y += leg;
                direction = -direction;
            }
            return cells;
        }

        // -------------------------------------------------------------- carving

        /// <summary>
        /// Carves a walkable corridor along the given cells: air for the body, then floors
        /// underneath - but never a floor where another part of the same corridor needs air.
        /// </summary>
        public static void CarveCorridor(TileMap tiles, List<Vector2Int> cells, bool lockAir = false)
        {
            var airCells = new HashSet<Vector2Int>();
            foreach (var c in cells)
            {
                for (int k = 0; k < CorridorHeight; k++)
                {
                    airCells.Add(new Vector2Int(c.x, c.y + k));
                }
            }
            foreach (var c in airCells)
            {
                tiles.Carve(c.x, c.y, lockAir);
            }
            foreach (var c in cells)
            {
                var below = new Vector2Int(c.x, c.y - 1);
                if (!airCells.Contains(below))
                {
                    tiles.Fill(below.x, below.y, TileKind.Built);
                }
            }
        }

        /// <summary>
        /// Connects two standing cells with terrain the player can actually traverse:
        /// a slope where there is horizontal room, otherwise a switchback ramp.
        /// </summary>
        public static void ConnectCells(TileMap tiles, Vector2Int src, Vector2Int goal, RectInt rect,
            bool lockAir = false)
        {
            List<Vector2Int> cells;
            if (Mathf.Abs(goal.y - src.y) <= Mathf.Abs(goal.x - src.x))
            {
                cells = SlopePath(src, goal);
            }
            else
            {
                Vector2Int low = src.y < goal.y ? src : goal;
                Vector2Int high = src.y < goal.y ? goal : src;
                const int span = 7;
                int xLo = Mathf.Max(rect.xMin + 1, Mathf.Min(low.x, high.x) - span);
                int xHi = Mathf.Min(rect.xMax - 1, Mathf.Max(low.x, high.x) + span);

                var ramp = SwitchbackCells(xLo, xHi, low.y, high.y, low.x);
                cells = new List<Vector2Int>();
                cells.AddRange(SlopePath(low, ramp[0]));
                cells.AddRange(ramp);
                cells.AddRange(SlopePath(ramp[ramp.Count - 1], high));
            }

            // Endpoints belong to the same pass; a floor forced in afterwards could seal
            // the air the ramp needs one column over.
            if (cells[0] != src) cells.Insert(0, src);
            if (cells[cells.Count - 1] != goal) cells.Add(goal);
            CarveCorridor(tiles, cells, lockAir);
        }

        public static void CarveEllipse(TileMap tiles, int cx, int cy, int rx, int ry, RectInt rect)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    if (x < rect.xMin || x > rect.xMax || y < rect.yMin || y > rect.yMax) continue;
                    float dx = (x - cx) / Mathf.Max(1f, rx);
                    float dy = (y - cy) / Mathf.Max(1f, ry);
                    if (dx * dx + dy * dy <= 1f) tiles.Carve(x, y);
                }
            }
        }

        // ------------------------------------------------------------- cave shape

        /// <summary>
        /// Classic 4-5 cellular automata cave. Returns true for solid tiles.
        /// Out-of-bounds counts as solid so caves close themselves off at the edges.
        /// </summary>
        public static bool[,] CellularCave(System.Random rng, int w, int h, float fillProbability = 0.50f,
            int iterations = 4)
        {
            var grid = new bool[w, h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    grid[x, y] = rng.NextDouble() < fillProbability;
                }
            }

            for (int step = 0; step < iterations; step++)
            {
                var next = new bool[w, h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int n = 0;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= w || ny >= h) n++;
                                else if (grid[nx, ny]) n++;
                            }
                        }
                        if (n >= 5) next[x, y] = true;
                        else if (n <= 3) next[x, y] = false;
                        else next[x, y] = grid[x, y];
                    }
                }
                grid = next;
            }
            return grid;
        }

        /// <summary>
        /// Breaks tall voids up with rocky outcrops spaced within step-up range, so an
        /// organic cave stays climbable without turning into regular shelving.
        /// </summary>
        public static void ScaffoldLedges(TileMap tiles, System.Random rng, RectInt rect, int maxGap)
        {
            int x = rect.xMin;
            while (x <= rect.xMax)
            {
                int band = rng.Next(2, 5);
                int lastCol = Mathf.Min(x + band - 1, rect.xMax);
                int run = 0;
                for (int y = rect.yMin; y <= rect.yMax; y++)
                {
                    bool anySolid = false;
                    for (int cx = x; cx <= lastCol; cx++)
                    {
                        if (tiles.IsSolid(cx, y)) { anySolid = true; break; }
                    }
                    if (anySolid) { run = 0; continue; }

                    run++;
                    if (run < maxGap) continue;

                    if (rng.NextDouble() < 0.75)
                    {
                        for (int cx = x; cx <= lastCol; cx++) tiles.Fill(cx, y, TileKind.Rock);
                        run = 0;
                    }
                    else
                    {
                        run = 1;
                    }
                }
                x += band + rng.Next(0, 3);
            }
        }

        /// <summary>Connected components of air inside a rectangle (4-connectivity).</summary>
        public static List<List<Vector2Int>> AirRegions(TileMap tiles, RectInt rect)
        {
            var seen = new HashSet<Vector2Int>();
            var regions = new List<List<Vector2Int>>();
            var queue = new Queue<Vector2Int>();

            for (int y = rect.yMin; y <= rect.yMax; y++)
            {
                for (int x = rect.xMin; x <= rect.xMax; x++)
                {
                    var origin = new Vector2Int(x, y);
                    if (seen.Contains(origin) || tiles.IsSolid(x, y)) continue;

                    var component = new List<Vector2Int>();
                    queue.Clear();
                    queue.Enqueue(origin);
                    seen.Add(origin);
                    while (queue.Count > 0)
                    {
                        var c = queue.Dequeue();
                        component.Add(c);
                        EnqueueAirNeighbor(tiles, rect, seen, queue, c.x + 1, c.y);
                        EnqueueAirNeighbor(tiles, rect, seen, queue, c.x - 1, c.y);
                        EnqueueAirNeighbor(tiles, rect, seen, queue, c.x, c.y + 1);
                        EnqueueAirNeighbor(tiles, rect, seen, queue, c.x, c.y - 1);
                    }
                    regions.Add(component);
                }
            }
            return regions;
        }

        private static void EnqueueAirNeighbor(TileMap tiles, RectInt rect, HashSet<Vector2Int> seen,
            Queue<Vector2Int> queue, int nx, int ny)
        {
            if (nx < rect.xMin || nx > rect.xMax || ny < rect.yMin || ny > rect.yMax) return;
            var n = new Vector2Int(nx, ny);
            if (seen.Contains(n) || tiles.IsSolid(nx, ny)) return;
            seen.Add(n);
            queue.Enqueue(n);
        }
    }
}
