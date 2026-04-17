using UnityEngine;

namespace CircuitFlowAlchemy.Prototype.FactoryAlchemy
{
    public static class SpriteUtil
    {
        public const int TilePixels = 128;

        public static Sprite MakeSolidSprite(Color color)
        {
            var texture = new Texture2D(TilePixels, TilePixels, TextureFormat.RGBA32, false);
            for (int y = 0; y < TilePixels; y++)
            {
                for (int x = 0; x < TilePixels; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, TilePixels, TilePixels), new Vector2(0.5f, 0.5f), TilePixels);
        }

        public static Sprite MakeGroundTileSprite(Color baseColor, Color accentColor, int xSeed, int ySeed)
        {
            var texture = new Texture2D(TilePixels, TilePixels, TextureFormat.RGBA32, false);
            int hash = (xSeed * 73856093) ^ (ySeed * 19349663);

            for (int y = 0; y < TilePixels; y++)
            {
                for (int x = 0; x < TilePixels; x++)
                {
                    bool border = x < 2 || y < 2 || x >= TilePixels - 2 || y >= TilePixels - 2;
                    float checker = (((x / 16) + (y / 16)) & 1) == 0 ? 0.06f : -0.02f;
                    var c = baseColor * (1f + checker);
                    bool accentDot = ((x + hash) % 29 == 0) && ((y + hash) % 23 == 0);
                    if (accentDot) c = Color.Lerp(c, accentColor, 0.25f);
                    if (border) c = Color.Lerp(c, Color.black, 0.25f);
                    c.a = 1f;
                    texture.SetPixel(x, y, c);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, TilePixels, TilePixels), new Vector2(0.5f, 0.5f), TilePixels);
        }
    }
}
