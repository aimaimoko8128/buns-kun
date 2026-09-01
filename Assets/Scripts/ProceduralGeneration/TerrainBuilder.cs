using UnityEngine;
using BunsKun.Game;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>
    /// Helper for building simple placeholder terrain pieces (floors, walls, platforms)
    /// out of generated sprites and box colliders - no art assets or prefabs required.
    /// </summary>
    public static class TerrainBuilder
    {
        public static GameObject CreateSolidBlock(Vector3 center, Vector2 size, Transform parent, Color color, string blockName)
        {
            var go = new GameObject(blockName);
            go.transform.SetParent(parent, true);
            go.transform.position = center;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square(color);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            return go;
        }

        public static GameObject CreatePlatform(Vector3 center, float width, Transform parent, Color color)
        {
            return CreateSolidBlock(center, new Vector2(width, 0.6f), parent, color, "Platform");
        }
    }
}
