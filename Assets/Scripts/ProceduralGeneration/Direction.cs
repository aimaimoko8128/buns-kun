using UnityEngine;

namespace BunsKun.ProceduralGeneration
{
    public enum Direction
    {
        Right,
        Left,
        Up,
        Down
    }

    public static class DirectionExtensions
    {
        public static Direction Opposite(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Right: return Direction.Left;
                case Direction.Left: return Direction.Right;
                case Direction.Up: return Direction.Down;
                default: return Direction.Up;
            }
        }

        public static Vector2Int Delta(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Right: return new Vector2Int(1, 0);
                case Direction.Left: return new Vector2Int(-1, 0);
                case Direction.Up: return new Vector2Int(0, 1);
                default: return new Vector2Int(0, -1);
            }
        }
    }
}
