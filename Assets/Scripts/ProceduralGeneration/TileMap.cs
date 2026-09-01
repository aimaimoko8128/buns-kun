using UnityEngine;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>What a tile is made of. Purely cosmetic - both values are solid terrain.</summary>
    public enum TileKind
    {
        Rock,     // natural cave wall
        Built     // floor of a carved path, ledge, or ramp
    }

    /// <summary>
    /// The tile grid a whole layer is carved out of. One tile is one world unit.
    /// Tiles can be locked, which means later generation stages are not allowed to
    /// change them - that is how doorways and the world boundary are kept intact.
    /// </summary>
    public class TileMap
    {
        public readonly int Width;
        public readonly int Height;

        /// <summary>World position of the bottom-left corner of tile (0,0).</summary>
        public readonly Vector2 WorldOrigin;

        private readonly bool[] solid;
        private readonly TileKind[] kinds;
        private readonly bool[] locked;

        public TileMap(int width, int height, Vector2 worldOrigin)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            WorldOrigin = worldOrigin;

            solid = new bool[Width * Height];
            kinds = new TileKind[Width * Height];
            locked = new bool[Width * Height];

            for (int i = 0; i < solid.Length; i++) solid[i] = true;
        }

        private int Index(int x, int y) => y * Width + x;

        public bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        /// <summary>Anything outside the grid counts as solid, so callers never fall out of the world.</summary>
        public bool IsSolid(int x, int y) => !Inside(x, y) || solid[Index(x, y)];

        public bool IsAir(int x, int y) => !IsSolid(x, y);

        public bool IsLocked(int x, int y) => Inside(x, y) && locked[Index(x, y)];

        public TileKind KindAt(int x, int y) => Inside(x, y) ? kinds[Index(x, y)] : TileKind.Rock;

        public void Carve(int x, int y, bool lockTile = false)
        {
            if (!Inside(x, y) || locked[Index(x, y)]) return;
            solid[Index(x, y)] = false;
            if (lockTile) locked[Index(x, y)] = true;
        }

        public void Fill(int x, int y, TileKind kind = TileKind.Rock, bool lockTile = false)
        {
            if (!Inside(x, y) || locked[Index(x, y)]) return;
            solid[Index(x, y)] = true;
            kinds[Index(x, y)] = kind;
            if (lockTile) locked[Index(x, y)] = true;
        }

        public Vector2 TileCenterToWorld(int x, int y)
        {
            return WorldOrigin + new Vector2(x + 0.5f, y + 0.5f);
        }

        public Vector2 TileCenterToWorld(Vector2Int tile) => TileCenterToWorld(tile.x, tile.y);

        /// <summary>World position a character should be spawned at to stand on tile (x, y).</summary>
        public Vector3 StandPositionToWorld(Vector2Int tile, float bodyHalfHeight)
        {
            Vector2 center = TileCenterToWorld(tile);
            return new Vector3(center.x, center.y - 0.5f + bodyHalfHeight, 0f);
        }
    }
}
