using System.Collections.Generic;
using UnityEngine;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>
    /// Answers "can the player actually get there?" for a tile map, using the player's
    /// real movement limits rather than simple air connectivity.
    ///
    /// Two models are used:
    ///   * jump only  - what the guaranteed route (doors, exit, boss) is validated against
    ///   * jetpack    - what open cave interiors and optional loot are validated against
    /// </summary>
    public class ReachabilityAnalyzer
    {
        private readonly TileMap tiles;
        private readonly PlayerMovementProfile profile;
        private const int MaxFallTiles = 60;

        public ReachabilityAnalyzer(TileMap tiles, PlayerMovementProfile profile)
        {
            this.tiles = tiles;
            this.profile = profile;
        }

        /// <summary>Is there room for the player's body with its feet at this tile?</summary>
        public bool IsClear(int x, int y)
        {
            for (int k = 0; k < profile.ClearanceTiles; k++)
            {
                if (tiles.IsSolid(x, y + k)) return false;
            }
            return true;
        }

        /// <summary>Can the player stand here - body fits and there is ground underfoot?</summary>
        public bool IsStandable(int x, int y)
        {
            return IsClear(x, y) && tiles.IsSolid(x, y - 1);
        }

        public bool IsStandable(Vector2Int c) => IsStandable(c.x, c.y);

        /// <summary>Every standing cell the player can reach from a start cell.</summary>
        public HashSet<Vector2Int> ComputeReachable(Vector2Int start, bool withJetpack)
        {
            var seen = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            var buffer = new List<Vector2Int>(48);

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                buffer.Clear();
                AppendNeighbors(cell, withJetpack, buffer);
                foreach (var n in buffer)
                {
                    if (seen.Add(n)) queue.Enqueue(n);
                }
            }
            return seen;
        }

        private void AppendNeighbors(Vector2Int from, bool withJetpack, List<Vector2Int> results)
        {
            int x = from.x, y = from.y;
            int stepUp = profile.MaxStepUpTiles;
            int clearance = profile.ClearanceTiles;

            // Walk, step up onto a ledge, or step down onto lower ground.
            for (int dx = -1; dx <= 1; dx += 2)
            {
                for (int dy = -stepUp; dy <= stepUp; dy++)
                {
                    int tx = x + dx, ty = y + dy;
                    if (!IsStandable(tx, ty)) continue;
                    if (dy > 0)
                    {
                        bool columnClear = true;
                        for (int k = 0; k < dy + clearance; k++)
                        {
                            if (tiles.IsSolid(x, y + k)) { columnClear = false; break; }
                        }
                        if (!columnClear) continue;
                    }
                    if (!IsClear(tx, ty)) continue;
                    results.Add(new Vector2Int(tx, ty));
                }
            }

            // Jump across a gap.
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int d = 2; d <= profile.MaxJumpAcrossTiles; d++)
                {
                    for (int dy = -3; dy <= stepUp; dy++)
                    {
                        int tx = x + sx * d, ty = y + dy;
                        if (!IsStandable(tx, ty)) continue;
                        int top = Mathf.Max(y, ty);
                        bool arcClear = true;
                        for (int k = 0; k <= d; k++)
                        {
                            if (!IsClear(x + sx * k, top)) { arcClear = false; break; }
                        }
                        if (arcClear) results.Add(new Vector2Int(tx, ty));
                    }
                }
            }

            // Walk off an edge and fall.
            for (int sx = -1; sx <= 1; sx++)
            {
                int cx = x + sx;
                if (sx != 0 && !IsClear(cx, y)) continue;
                int cy = y;
                for (int step = 0; step < MaxFallTiles; step++)
                {
                    cy--;
                    if (cy <= 0 || !IsClear(cx, cy)) break;
                    if (IsStandable(cx, cy))
                    {
                        results.Add(new Vector2Int(cx, cy));
                        break;
                    }
                }
            }

            if (!withJetpack) return;

            // Rise on jetpack thrust and land on a higher ledge.
            for (int rise = 1; rise <= profile.MaxFlyUpTiles; rise++)
            {
                if (!IsClear(x, y + rise)) break;
                for (int drift = -profile.MaxFlyDriftTiles; drift <= profile.MaxFlyDriftTiles; drift++)
                {
                    int tx = x + drift, ty = y + rise;
                    if (!IsStandable(tx, ty)) continue;
                    int step = drift >= 0 ? 1 : -1;
                    bool laneClear = true;
                    for (int k = 0; k <= Mathf.Abs(drift); k++)
                    {
                        if (!IsClear(x + step * k, y + rise)) { laneClear = false; break; }
                    }
                    if (laneClear) results.Add(new Vector2Int(tx, ty));
                }
            }
        }

        /// <summary>Nearest standing cell at or below a point, then above it as a fallback.</summary>
        public Vector2Int? SnapToStanding(Vector2Int point, int limit = 30)
        {
            for (int k = 0; k < limit; k++)
            {
                if (IsStandable(point.x, point.y - k)) return new Vector2Int(point.x, point.y - k);
            }
            for (int k = 1; k < limit; k++)
            {
                if (IsStandable(point.x, point.y + k)) return new Vector2Int(point.x, point.y + k);
            }
            return null;
        }

        /// <summary>All standing cells inside a rectangle, in a stable order.</summary>
        public List<Vector2Int> StandingCellsIn(RectInt rect)
        {
            var result = new List<Vector2Int>();
            for (int y = rect.yMin; y <= rect.yMax; y++)
            {
                for (int x = rect.xMin; x <= rect.xMax; x++)
                {
                    if (IsStandable(x, y)) result.Add(new Vector2Int(x, y));
                }
            }
            return result;
        }

        /// <summary>
        /// A required point counts as reachable if any standing cell close to it is
        /// reachable - the exact tile may shift while neighbouring terrain is carved.
        /// </summary>
        public bool AreaReachable(Vector2Int anchor, HashSet<Vector2Int> reachable, int radius = 3)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    var c = new Vector2Int(anchor.x + dx, anchor.y + dy);
                    if (reachable.Contains(c) && IsStandable(c)) return true;
                }
            }
            return false;
        }
    }
}
