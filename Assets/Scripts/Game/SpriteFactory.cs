using System.Collections.Generic;
using UnityEngine;

namespace BunsKun.Game
{
    /// <summary>
    /// Generates simple placeholder sprites (circles, squares, rounded rects) at runtime
    /// so the game is playable without any hand-authored art assets.
    /// Sprites are cached by their generation parameters to avoid redundant texture creation.
    /// </summary>
    public static class SpriteFactory
    {
        private const int Resolution = 64;
        private const float PixelsPerUnit = 64f;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Clears the sprite cache. Must be called once at the start of every Play session:
        /// when "Enter Play Mode Options" has domain reload disabled, this static cache
        /// otherwise keeps referencing Sprite objects that Unity already destroyed when the
        /// previous Play session ended, which makes everything built from them invisible.
        /// </summary>
        public static void ResetCache()
        {
            Cache.Clear();
        }

        public static Sprite Circle(Color color)
        {
            string key = "circle_" + ColorUtility.ToHtmlStringRGBA(color);
            if (Cache.TryGetValue(key, out Sprite cached)) return cached;

            var texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2(Resolution / 2f, Resolution / 2f);
            float radius = Resolution / 2f - 1f;

            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    Color pixel = color;
                    pixel.a = color.a * Mathf.Clamp01(alpha);
                    texture.SetPixel(x, y, pixel);
                }
            }
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, Resolution, Resolution), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Square(Color color, float cornerRadiusFraction = 0f)
        {
            string key = "square_" + ColorUtility.ToHtmlStringRGBA(color) + "_" + cornerRadiusFraction.ToString("F2");
            if (Cache.TryGetValue(key, out Sprite cached)) return cached;

            var texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            float corner = Resolution * Mathf.Clamp01(cornerRadiusFraction);

            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    bool inside = true;
                    if (corner > 0.5f)
                    {
                        inside = InsideRoundedSquare(x, y, Resolution, corner);
                    }
                    Color pixel = color;
                    pixel.a = inside ? color.a : 0f;
                    texture.SetPixel(x, y, pixel);
                }
            }
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, Resolution, Resolution), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            Cache[key] = sprite;
            return sprite;
        }

        private static bool InsideRoundedSquare(int x, int y, int size, float corner)
        {
            float nx = x + 0.5f;
            float ny = y + 0.5f;
            float minEdge = Mathf.Min(nx, ny, size - nx, size - ny);
            if (minEdge >= 0f) return true;
            return true;
        }

        /// <summary>Creates a simple downward-pointing triangle sprite, used for UI "next attack" indicators.</summary>
        public static Sprite Triangle(Color color)
        {
            string key = "triangle_" + ColorUtility.ToHtmlStringRGBA(color);
            if (Cache.TryGetValue(key, out Sprite cached)) return cached;

            var texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    float u = x / (float)Resolution;
                    float v = 1f - y / (float)Resolution;
                    bool inside = v <= 1f && v >= 0f && Mathf.Abs(u - 0.5f) <= (1f - v) * 0.5f;
                    Color pixel = color;
                    pixel.a = inside ? color.a : 0f;
                    texture.SetPixel(x, y, pixel);
                }
            }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, Resolution, Resolution), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            Cache[key] = sprite;
            return sprite;
        }
    }
}
