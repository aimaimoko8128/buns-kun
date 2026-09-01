using System.Collections.Generic;
using UnityEngine;
using BunsKun.Game;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>
    /// Turns a tile map into actual scene geometry. Neighbouring solid tiles of the same
    /// kind are merged into the largest rectangles possible, so a cave made of thousands
    /// of tiles becomes a few hundred sprites and colliders instead of one per tile.
    /// </summary>
    public static class TerrainRenderer
    {
        private static readonly Color RockColor = new Color(0.34f, 0.27f, 0.24f);
        private static readonly Color BuiltColor = new Color(0.52f, 0.40f, 0.28f);
        private static readonly Color BackgroundColor = new Color(0.12f, 0.10f, 0.13f);

        public static void Build(TileMap tiles, Transform parent)
        {
            BuildBackground(tiles, parent);

            bool[] used = new bool[tiles.Width * tiles.Height];
            var terrainRoot = new GameObject("Terrain");
            terrainRoot.transform.SetParent(parent, false);

            for (int y = 0; y < tiles.Height; y++)
            {
                for (int x = 0; x < tiles.Width; x++)
                {
                    int index = y * tiles.Width + x;
                    if (used[index] || !tiles.IsSolid(x, y)) continue;

                    TileKind kind = tiles.KindAt(x, y);

                    // Grow right as far as this run of identical tiles goes.
                    int width = 1;
                    while (x + width < tiles.Width
                           && !used[y * tiles.Width + x + width]
                           && tiles.IsSolid(x + width, y)
                           && tiles.KindAt(x + width, y) == kind)
                    {
                        width++;
                    }

                    // Then grow down while the whole row below matches.
                    int height = 1;
                    bool canGrow = true;
                    while (canGrow && y + height < tiles.Height)
                    {
                        for (int i = 0; i < width; i++)
                        {
                            int below = (y + height) * tiles.Width + x + i;
                            if (used[below] || !tiles.IsSolid(x + i, y + height)
                                || tiles.KindAt(x + i, y + height) != kind)
                            {
                                canGrow = false;
                                break;
                            }
                        }
                        if (canGrow) height++;
                    }

                    for (int dy = 0; dy < height; dy++)
                    {
                        for (int dx = 0; dx < width; dx++)
                        {
                            used[(y + dy) * tiles.Width + x + dx] = true;
                        }
                    }

                    CreateBlock(tiles, terrainRoot.transform, x, y, width, height, kind);
                }
            }
        }

        private static void CreateBlock(TileMap tiles, Transform parent, int x, int y,
            int width, int height, TileKind kind)
        {
            Vector2 min = tiles.TileCenterToWorld(x, y) - new Vector2(0.5f, 0.5f);
            var center = new Vector3(min.x + width * 0.5f, min.y + height * 0.5f, 0f);

            var go = new GameObject($"Rock_{x}_{y}");
            go.transform.SetParent(parent, true);
            go.transform.position = center;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square(kind == TileKind.Rock ? RockColor : BuiltColor);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(width, height);
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);
        }

        private static void BuildBackground(TileMap tiles, Transform parent)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(parent, false);
            Vector2 min = tiles.WorldOrigin;
            go.transform.position = new Vector3(min.x + tiles.Width * 0.5f, min.y + tiles.Height * 0.5f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square(BackgroundColor);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(tiles.Width, tiles.Height);
            sr.sortingOrder = -3;
        }
    }
}
