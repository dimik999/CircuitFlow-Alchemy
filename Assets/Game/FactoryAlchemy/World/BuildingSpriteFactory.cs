using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public static class BuildingSpriteFactory
    {
        // If your source art has a different default orientation, tweak these two values.
        private const float StraightBaseAngle = 0f;
        private const float CornerBaseAngle = 0f;

        private static readonly Dictionary<BuildingType, Sprite> WorldSprites = new Dictionary<BuildingType, Sprite>();
        private static readonly Dictionary<int, Sprite> PipeWorldSprites = new Dictionary<int, Sprite>();
        private static readonly Dictionary<BuildingType, Texture2D> IconTextures = new Dictionary<BuildingType, Texture2D>();
        private static Sprite _arrowSprite;
        private static bool _initialized;

        public static void InvalidateCaches()
        {
            WorldSprites.Clear();
            PipeWorldSprites.Clear();
            IconTextures.Clear();
            _arrowSprite = null;
            _initialized = false;
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            ExportPngSetToProjectFolder();
        }

        /// <summary>Extra transform scale applied on placed/preview objects (sprite PPU already normalized).</summary>
        public static float GetWorldVisualScale(BuildingType type)
        {
            return 1f;
        }

        public static Sprite GetWorldSprite(BuildingType type)
        {
            EnsureInitialized();
            if (type == BuildingType.Pipe)
            {
                return GetPipeWorldSprite(0);
            }
            if (type == BuildingType.PipeCorner)
            {
                return LoadWorldSpriteFromResources(type, "Sprites/Buildings/pipe_corner")
                    ?? LoadWorldSpriteFromResources(type, "Sprites/Buildings/pipe_angle")
                    ?? LoadWorldSpriteFromResources(type, "Sprites/Buildings/pipe_l")
                    ?? GetPipeWorldSprite(6);
            }

            string resourcePath = $"Sprites/Buildings/{GetBuildingSpriteName(type)}";
            Sprite loaded = type == BuildingType.PipeConnector || type == BuildingType.PipeSplitter
                ? CreateCenteredJunctionWorldSprite(type, resourcePath)
                : LoadWorldSpriteFromResources(type, resourcePath);
            if (loaded != null)
            {
                WorldSprites[type] = loaded;
                return loaded;
            }

            if (WorldSprites.TryGetValue(type, out var sprite))
            {
                return sprite;
            }

            int size = SpriteUtil.TilePixels;
            var tex = DrawIconTexture(type, size);
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), size);
            WorldSprites[type] = sprite;
            return sprite;
        }

        public static Sprite GetPipeWorldSprite(int mask)
        {
            EnsureInitialized();
            mask &= 0x0F;
            if (PipeWorldSprites.TryGetValue(mask, out var s))
            {
                return s;
            }

            s = LoadPipeSpriteFromResources(mask);
            if (s != null)
            {
                PipeWorldSprites[mask] = s;
                return s;
            }

            int size = SpriteUtil.TilePixels;
            var tex = DrawPipeTexture(size, mask);
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), size);
            PipeWorldSprites[mask] = s;
            return s;
        }

        public static bool TryResolvePipeSpriteAndAngle(int mask, out Sprite sprite, out float angle)
        {
            EnsureInitialized();
            mask &= 0x0F;
            angle = 0f;

            // Full manual override by mask has highest priority.
            sprite = LoadWorldSpriteFromResources(BuildingType.Pipe, $"Sprites/Buildings/pipe_{mask}");
            if (sprite != null)
            {
                return true;
            }

            // Straight/corner workflow without runtime pixel rotation (works even if texture isn't Read/Write).
            var straight = LoadWorldSpriteFromResources(BuildingType.Pipe, "Sprites/Buildings/pipe_straight")
                           ?? LoadWorldSpriteFromResources(BuildingType.Pipe, "Sprites/Buildings/pipe");
            var corner = LoadWorldSpriteFromResources(BuildingType.Pipe, "Sprites/Buildings/pipe_corner")
                         ?? LoadWorldSpriteFromResources(BuildingType.Pipe, "Sprites/Buildings/pipe_angle")
                         ?? LoadWorldSpriteFromResources(BuildingType.Pipe, "Sprites/Buildings/pipe_l");

            if (straight != null)
            {
                if (mask == 0)  { sprite = straight; angle = StraightBaseAngle + 0f; return true; }
                if (mask == 2)  { sprite = straight; angle = StraightBaseAngle + 0f; return true; }   // E
                if (mask == 8)  { sprite = straight; angle = StraightBaseAngle + 0f; return true; }   // W
                if (mask == 1)  { sprite = straight; angle = StraightBaseAngle + 90f; return true; }  // N
                if (mask == 4)  { sprite = straight; angle = StraightBaseAngle + 90f; return true; }  // S
                if (mask == 10) { sprite = straight; angle = StraightBaseAngle + 0f; return true; }   // E-W
                if (mask == 5)  { sprite = straight; angle = StraightBaseAngle + 90f; return true; }  // N-S
            }

            if (corner != null)
            {
                // Measured orientation from source art: base is S+W.
                if (mask == 12) { sprite = corner; angle = CornerBaseAngle + 0f; return true; }     // S+W
                if (mask == 6)  { sprite = corner; angle = CornerBaseAngle + 90f; return true; }    // E+S
                if (mask == 3)  { sprite = corner; angle = CornerBaseAngle + 180f; return true; }   // N+E
                if (mask == 9)  { sprite = corner; angle = CornerBaseAngle + 270f; return true; }   // W+N
            }

            sprite = null;
            angle = 0f;
            return false;
        }

        public static Texture2D GetIconTexture(BuildingType type)
        {
            EnsureInitialized();
            if (IconTextures.TryGetValue(type, out var tex))
            {
                return tex;
            }

            var iconSprite = LoadRawSpriteFromResources($"Sprites/BuildIcons/{GetBuildingSpriteName(type)}");
            if (iconSprite == null)
            {
                // Fallback: reuse world sprite for icon when dedicated icon is absent.
                iconSprite = LoadRawSpriteFromResources($"Sprites/Buildings/{GetBuildingSpriteName(type)}");
            }
            if (iconSprite != null && iconSprite.texture != null)
            {
                IconTextures[type] = iconSprite.texture;
                return iconSprite.texture;
            }

            tex = DrawIconTexture(type, 16);
            IconTextures[type] = tex;
            return tex;
        }

        public static Sprite GetArrowSprite()
        {
            EnsureInitialized();
            if (_arrowSprite != null)
            {
                return _arrowSprite;
            }

            const int s = 16;
            var tex = NewTexture(s, new Color(0f, 0f, 0f, 0f));
            Color c = new Color(0.95f, 0.96f, 1f, 0.92f);
            for (int y = 4; y < 12; y++)
            {
                for (int x = 2; x < 8; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }
            for (int y = 2; y < 14; y++)
            {
                int dx = y < 8 ? y - 2 : 13 - y;
                int tipX = 8 + dx;
                for (int x = 8; x <= tipX; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            _arrowSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _arrowSprite;
        }

        private static Texture2D DrawIconTexture(BuildingType type, int size)
        {
            var bg = new Color(0.10f, 0.13f, 0.18f, 1f);
            var tex = NewTexture(size, bg);

            DrawRect(tex, 1, 1, size - 2, size - 2, new Color(0.04f, 0.05f, 0.08f, 1f), false);
            DrawRect(tex, 1, 1, size - 2, size - 2, new Color(0.28f, 0.32f, 0.42f, 1f), true);

            switch (type)
            {
                case BuildingType.Pipe:
                    DrawRect(tex, size / 2 - 2, 4, 4, size - 8, new Color(0.73f, 0.78f, 0.84f, 1f), false);
                    DrawRect(tex, 4, size / 2 - 2, size - 8, 4, new Color(0.73f, 0.78f, 0.84f, 1f), false);
                    break;
                case BuildingType.PipeCorner:
                    DrawRect(tex, size / 2 - 2, size / 2 - 2, 4, size / 2 + 1, new Color(0.73f, 0.78f, 0.84f, 1f), false);
                    DrawRect(tex, size / 2 - 2, size / 2 - 2, size / 2 + 1, 4, new Color(0.73f, 0.78f, 0.84f, 1f), false);
                    break;
                case BuildingType.Extractor:
                    DrawRect(tex, 5, 5, size - 10, size - 10, new Color(0.96f, 0.60f, 0.18f, 1f), false);
                    DrawRect(tex, size / 2 - 2, 2, 4, 6, new Color(0.83f, 0.90f, 0.98f, 1f), false);
                    break;
                case BuildingType.Storage:
                    DrawRect(tex, 4, 6, size - 8, size - 10, new Color(0.28f, 0.86f, 0.45f, 1f), false);
                    DrawRect(tex, 6, 8, size - 12, 3, new Color(0.17f, 0.40f, 0.23f, 1f), false);
                    break;
                case BuildingType.Mixer:
                    DrawRect(tex, 4, 4, size - 8, size - 8, new Color(0.62f, 0.38f, 0.93f, 1f), false);
                    DrawRect(tex, size / 2 - 1, 4, 2, size - 8, new Color(0.90f, 0.86f, 1f, 1f), false);
                    DrawRect(tex, 4, size / 2 - 1, size - 8, 2, new Color(0.90f, 0.86f, 1f, 1f), false);
                    break;
                case BuildingType.Generator:
                    DrawRect(tex, 4, 4, size - 8, size - 8, new Color(0.96f, 0.89f, 0.20f, 1f), false);
                    for (int i = 0; i < 6; i++)
                    {
                        tex.SetPixel(6 + i, size - 8 - i, new Color(0.25f, 0.20f, 0.05f, 1f));
                    }
                    break;
                case BuildingType.PowerPole:
                    DrawRect(tex, size / 2 - 1, 4, 2, size - 8, new Color(0.92f, 0.76f, 0.50f, 1f), false);
                    DrawRect(tex, 4, 6, size - 8, 2, new Color(0.78f, 0.64f, 0.42f, 1f), false);
                    break;
                case BuildingType.MarketTerminal:
                    DrawRect(tex, 4, 6, size - 8, size - 10, new Color(0.20f, 0.93f, 0.93f, 1f), false);
                    DrawRect(tex, 4, 3, size - 8, 3, new Color(0.10f, 0.62f, 0.62f, 1f), false);
                    break;
                case BuildingType.PipeConnector:
                {
                    // Connector: 3 inputs (blue) + 1 output (orange).
                    var core = new Color(0.24f, 0.30f, 0.40f, 1f);
                    var inPort = new Color(0.22f, 0.74f, 0.98f, 1f);
                    var outPort = new Color(0.97f, 0.56f, 0.20f, 1f);
                    int c = size / 2;
                    int w = Mathf.Max(3, size / 4);
                    int half = w / 2;
                    DrawRect(tex, c - half, c - half, w, w, core, false); // center
                    DrawRect(tex, 2, c - half, c - half - 1, w, inPort, false); // input left
                    DrawRect(tex, c - half, 2, w, c - half - 1, inPort, false); // input up
                    DrawRect(tex, c - half, c + half + 1, w, size - c - half - 3, inPort, false); // input down
                    DrawRect(tex, c + half + 1, c - half, size - c - half - 3, w, outPort, false); // output right
                    break;
                }
                case BuildingType.PipeSplitter:
                {
                    // Splitter: 1 input (blue) + 3 outputs (orange).
                    var core = new Color(0.24f, 0.30f, 0.40f, 1f);
                    var inPort = new Color(0.22f, 0.74f, 0.98f, 1f);
                    var outPort = new Color(0.97f, 0.56f, 0.20f, 1f);
                    int c = size / 2;
                    int w = Mathf.Max(3, size / 4);
                    int half = w / 2;
                    DrawRect(tex, c - half, c - half, w, w, core, false); // center
                    DrawRect(tex, 2, c - half, c - half - 1, w, inPort, false); // input left
                    DrawRect(tex, c - half, 2, w, c - half - 1, outPort, false); // output up
                    DrawRect(tex, c - half, c + half + 1, w, size - c - half - 3, outPort, false); // output down
                    DrawRect(tex, c + half + 1, c - half, size - c - half - 3, w, outPort, false); // output right
                    break;
                }
                default:
                    DrawRect(tex, 5, 5, size - 10, size - 10, Color.gray, false);
                    break;
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D DrawPipeTexture(int size, int mask)
        {
            // Transparent background for world readability; only pipe itself is drawn.
            var pipe = new Color(0.90f, 0.95f, 1.00f, 1f);
            var accent = new Color(0.45f, 0.55f, 0.72f, 1f);
            var tex = NewTexture(size, new Color(0f, 0f, 0f, 0f));
            int c = size / 2;
            int w = Mathf.Max(12, size / 7);
            int half = w / 2;

            DrawRect(tex, c - half, c - half, w, w, pipe, false);
            DrawRect(tex, c - half + 1, c - half + 1, w - 2, w - 2, accent, true);

            if ((mask & 1) != 0) DrawRect(tex, c - half, c + half + 1, w, size - c - half - 3, pipe, false); // N
            if ((mask & 2) != 0) DrawRect(tex, c + half + 1, c - half, size - c - half - 3, w, pipe, false); // E
            if ((mask & 4) != 0) DrawRect(tex, c - half, 2, w, c - half - 1, pipe, false);       // S
            if ((mask & 8) != 0) DrawRect(tex, 2, c - half, c - half - 1, w, pipe, false);       // W

            tex.Apply();
            return tex;
        }

        private static Sprite LoadPipeSpriteFromResources(int mask)
        {
            if (TryResolvePipeSpriteAndAngle(mask, out var resolved, out _))
            {
                return resolved;
            }

            // Important: without per-mask sprites we fallback to procedural pipes so
            // connectivity remains visually correct for turns/T-junctions/crossings.
            return null;
        }

        private static string GetBuildingSpriteName(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Pipe: return "pipe";
                case BuildingType.PipeCorner: return "pipe_corner";
                case BuildingType.Extractor: return "extractor";
                case BuildingType.Storage: return "storage";
                case BuildingType.Mixer: return "mixer";
                case BuildingType.Generator: return "generator";
                case BuildingType.PowerPole: return "powerpole";
                case BuildingType.MarketTerminal: return "market";
                case BuildingType.PipeConnector: return "connector";
                case BuildingType.PipeSplitter: return "splitter";
                default: return "unknown";
            }
        }

        private static Sprite LoadWorldSpriteFromResources(BuildingType type, string pathWithoutExtension)
        {
            Sprite raw;
            if (type == BuildingType.PowerPole)
            {
                // Force explicit file-based pole sprite first, then fallback to standard Resources path.
                raw = LoadRawPowerPoleSpriteFallback();
                if (raw == null)
                {
                    raw = LoadRawSpriteFromResources(pathWithoutExtension);
                }
            }
            else
            {
                raw = LoadRawSpriteFromResources(pathWithoutExtension);
            }
            if (raw == null)
            {
                return null;
            }

            // Safety net for selected machine types:
            // if an imported texture is too elongated (contains pre-baked pipe strip),
            // skip it and fall back to generated 1x1 sprite.
            bool applyElongationGuard = type == BuildingType.Extractor
                                        || type == BuildingType.Mixer
                                        || type == BuildingType.Generator
                                        || type == BuildingType.MarketTerminal;
            if (applyElongationGuard && TryGetTightRect(raw, out var tight))
            {
                float w = Mathf.Max(1f, tight.width);
                float h = Mathf.Max(1f, tight.height);
                float ratio = Mathf.Max(w / h, h / w);
                if (ratio > 1.35f)
                {
                    return null;
                }
            }

            if (type == BuildingType.PipeConnector || type == BuildingType.PipeSplitter)
            {
                return CreateCenteredJunctionWorldSprite(type, pathWithoutExtension);
            }

            return NormalizeWorldSprite(type, raw);
        }

        private static Sprite CreateCenteredJunctionWorldSprite(BuildingType type, string pathWithoutExtension)
        {
            Sprite raw = LoadRawJunctionSprite(pathWithoutExtension);
            if (raw == null || raw.texture == null)
            {
                return null;
            }

            Rect rect = raw.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return null;
            }

            float basisPixels = Mathf.Max(rect.width, rect.height);
            float targetUnits = WorldGridSystem.CellWorldSize;
            float ppu = Mathf.Max(1f, basisPixels / targetUnits);

            return Sprite.Create(
                raw.texture,
                rect,
                new Vector2(0.5f, 0.5f),
                ppu,
                0,
                SpriteMeshType.FullRect);
        }

        private static Sprite LoadRawJunctionSprite(string pathWithoutExtension)
        {
            string baseName = Path.GetFileName(pathWithoutExtension);
            var sprites = Resources.LoadAll<Sprite>(pathWithoutExtension);
            if (sprites != null && sprites.Length > 0)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null && sprites[i].name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                    {
                        return sprites[i];
                    }
                }

                return sprites[0];
            }

            try
            {
                string filePath = Path.Combine(Application.dataPath, "Resources", pathWithoutExtension + ".png");
                if (!File.Exists(filePath))
                {
                    return null;
                }

                byte[] bytes = File.ReadAllBytes(filePath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                if (!tex.LoadImage(bytes, false))
                {
                    return null;
                }

                var fromFile = Resources.Load<Sprite>(pathWithoutExtension);
                if (fromFile != null && fromFile.rect.width > 0f && fromFile.rect.height > 0f)
                {
                    return Sprite.Create(
                        tex,
                        fromFile.rect,
                        new Vector2(0.5f, 0.5f),
                        fromFile.pixelsPerUnit);
                }

                return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), SpriteUtil.TilePixels);
            }
            catch
            {
                return null;
            }
        }

        private static Sprite LoadRawPowerPoleSpriteFallback()
        {
            // 1) Prefer direct file read from exact expected path.
            try
            {
                string filePath = Path.Combine(Application.dataPath, "Resources/Sprites/Buildings/powerpole.png");
                if (File.Exists(filePath))
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    if (tex.LoadImage(bytes, false))
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), SpriteUtil.TilePixels);
                    }
                }
            }
            catch
            {
                // Keep trying below.
            }

            // Try common alternative resource names first.
            var names = new[]
            {
                "Sprites/Buildings/power_pole",
                "Sprites/Buildings/powerPole",
                "Sprites/Buildings/pole",
                "Sprites/Buildings/powerpole"
            };
            for (int i = 0; i < names.Length; i++)
            {
                var s = LoadRawSpriteFromResources(names[i]);
                if (s != null)
                {
                    return s;
                }
            }

            return null;
        }

        private static Sprite LoadRawSpriteFromResources(string pathWithoutExtension)
        {
            string baseName = Path.GetFileName(pathWithoutExtension);
            var sprites = Resources.LoadAll<Sprite>(pathWithoutExtension);
            if (sprites != null && sprites.Length > 0)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null
                        && sprites[i].name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                    {
                        return sprites[i];
                    }
                }

                return sprites[0];
            }

            var sprite = Resources.Load<Sprite>(pathWithoutExtension);
            if (sprite != null)
            {
                return sprite;
            }

            // If imported as Texture2D rather than Sprite, convert at runtime.
            var tex = Resources.Load<Texture2D>(pathWithoutExtension);
            if (tex == null)
            {
                return null;
            }

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), SpriteUtil.TilePixels);
        }

        private static Sprite NormalizeWorldSprite(BuildingType type, Sprite source)
        {
            var rect = source.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return source;
            }

            bool isTransportFamily = type == BuildingType.Pipe || type == BuildingType.PipeCorner || type == BuildingType.PipeConnector || type == BuildingType.PipeSplitter;
            bool forceFullTexture = type == BuildingType.PowerPole
                                    || type == BuildingType.PipeCorner
                                    || type == BuildingType.Storage;
            Rect sampledRect;
            if (forceFullTexture && source.texture != null && source.texture.width > 0 && source.texture.height > 0)
            {
                sampledRect = new Rect(0f, 0f, source.texture.width, source.texture.height);
            }
            else if (type == BuildingType.PipeConnector || type == BuildingType.PipeSplitter)
            {
                // Full sprite slice — no tight crop, so cross arms are not clipped.
                sampledRect = rect;
            }
            else if (type == BuildingType.Mixer)
            {
                // Full slice keeps baked pipe stubs; tight crop was clipping connection ports.
                sampledRect = rect;
            }
            else
            {
                sampledRect = isTransportFamily
                    ? rect
                    : (TryGetTightRect(source, out var calculatedTight) ? calculatedTight : rect);
            }

            float basisPixels = GetBasisPixels(type, sampledRect);
            float targetUnits = GetTargetWorldUnits(type);
            float ppu = Mathf.Max(1f, basisPixels / Mathf.Max(0.01f, targetUnits));
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            if (type == BuildingType.Extractor
                && TryGetTightRect(source, out var tightForPivot))
            {
                // Recenter by visible content center to keep objects visually cell-centered.
                float px = ((tightForPivot.x + tightForPivot.width * 0.5f) - rect.x) / rect.width;
                float py = ((tightForPivot.y + tightForPivot.height * 0.5f) - rect.y) / rect.height;
                pivot = new Vector2(Mathf.Clamp01(px), Mathf.Clamp01(py));
            }

            return Sprite.Create(
                source.texture,
                sampledRect,
                pivot,
                ppu,
                0,
                SpriteMeshType.FullRect,
                source.border
            );
        }

        private static float GetTargetWorldUnits(BuildingType type)
        {
            float baseUnits;
            switch (type)
            {
                // Keep transport family visually consistent and close to tile scale.
                case BuildingType.Pipe:
                    baseUnits = 1.0f;
                    break;
                case BuildingType.PipeCorner:
                    baseUnits = 1.0f;
                    break;
                case BuildingType.PipeConnector:
                case BuildingType.PipeSplitter:
                    // Match straight pipe cell footprint so ports align with neighbors.
                    baseUnits = 1.0f;
                    break;
                // Core machines can be slightly larger for readability.
                case BuildingType.Extractor:
                case BuildingType.Generator:
                case BuildingType.Storage:
                case BuildingType.MarketTerminal:
                    baseUnits = 1.2f;
                    break;
                // Mixer uses full sprite slice (with stubs); modest boost over 1.2 for the drum only.
                case BuildingType.Mixer:
                    baseUnits = 1.32f;
                    break;
                case BuildingType.PowerPole:
                    baseUnits = 1.0f;
                    break;
                default:
                    baseUnits = 1.15f;
                    break;
            }

            return baseUnits * WorldGridSystem.CellWorldSize;
        }

        private static Sprite RotateSprite90(Sprite source, bool clockwise)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                int xMin = Mathf.RoundToInt(source.rect.xMin);
                int yMin = Mathf.RoundToInt(source.rect.yMin);
                int w = Mathf.RoundToInt(source.rect.width);
                int h = Mathf.RoundToInt(source.rect.height);
                var srcPixels = source.texture.GetPixels(xMin, yMin, w, h);
                var outTex = new Texture2D(h, w, TextureFormat.RGBA32, false);
                outTex.filterMode = FilterMode.Point;
                outTex.wrapMode = TextureWrapMode.Clamp;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int srcIndex = y * w + x;
                        int rx = clockwise ? (h - 1 - y) : y;
                        int ry = clockwise ? x : (w - 1 - x);
                        outTex.SetPixel(rx, ry, srcPixels[srcIndex]);
                    }
                }

                outTex.Apply();
                return Sprite.Create(outTex, new Rect(0, 0, outTex.width, outTex.height), new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
            }
            catch
            {
                // Source texture may be non-readable depending on import settings.
                return null;
            }
        }

        private static float GetBasisPixels(BuildingType type, Rect rect)
        {
            // Use max dimension so one sprite fits one tile even for non-square art.
            if (type == BuildingType.Pipe || type == BuildingType.PipeCorner || type == BuildingType.PipeConnector || type == BuildingType.PipeSplitter)
            {
                return Mathf.Max(1f, Mathf.Max(rect.width, rect.height));
            }

            return Mathf.Max(1f, Mathf.Max(rect.width, rect.height));
        }

        private static bool TryGetTightRect(Sprite source, out Rect tightRect)
        {
            tightRect = source.rect;
            try
            {
                int xMin = Mathf.RoundToInt(source.rect.xMin);
                int yMin = Mathf.RoundToInt(source.rect.yMin);
                int width = Mathf.RoundToInt(source.rect.width);
                int height = Mathf.RoundToInt(source.rect.height);
                var pixels = source.texture.GetPixels(xMin, yMin, width, height);

                int minX = width;
                int minY = height;
                int maxX = -1;
                int maxY = -1;

                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[row + x].a <= 0.01f) continue;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }

                if (maxX < minX || maxY < minY)
                {
                    return false;
                }

                tightRect = new Rect(xMin + minX, yMin + minY, maxX - minX + 1, maxY - minY + 1);
                return true;
            }
            catch
            {
                // Texture may be non-readable; fall back to full sprite rect.
                return false;
            }
        }

        private static bool TryGetPipeCornerPivot(Sprite source, out Vector2 pivot)
        {
            pivot = new Vector2(0.5f, 0.5f);
            if (source == null || source.texture == null)
            {
                return false;
            }

            try
            {
                int xMin = Mathf.RoundToInt(source.rect.xMin);
                int yMin = Mathf.RoundToInt(source.rect.yMin);
                int width = Mathf.RoundToInt(source.rect.width);
                int height = Mathf.RoundToInt(source.rect.height);
                if (width <= 1 || height <= 1)
                {
                    return false;
                }

                var pixels = source.texture.GetPixels(xMin, yMin, width, height);
                int band = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(width, height) * 0.12f), 4, Mathf.Min(width, height) / 2);
                if (band <= 0)
                {
                    return false;
                }

                var west = SampleSide(pixels, width, height, band, 0);
                var east = SampleSide(pixels, width, height, band, 1);
                var south = SampleSide(pixels, width, height, band, 2);
                var north = SampleSide(pixels, width, height, band, 3);

                var sides = new[] { west, east, south, north };
                Array.Sort(sides, (a, b) => b.Score.CompareTo(a.Score));
                if (sides[0].Score <= 0 || sides[1].Score <= 0)
                {
                    return false;
                }

                int aSide = sides[0].Side;
                int bSide = sides[1].Side;
                bool opposite = (aSide == 0 && bSide == 1) || (aSide == 1 && bSide == 0) || (aSide == 2 && bSide == 3) || (aSide == 3 && bSide == 2);
                if (opposite)
                {
                    return false; // straight segment, not corner
                }

                bool aVertical = aSide == 0 || aSide == 1;
                bool bVertical = bSide == 0 || bSide == 1;
                if (aVertical == bVertical)
                {
                    return false;
                }

                var vertical = aVertical ? sides[0] : sides[1];
                var horizontal = aVertical ? sides[1] : sides[0];

                float junctionX = horizontal.AvgX;
                float junctionY = vertical.AvgY;
                pivot = new Vector2(
                    Mathf.Clamp01(junctionX / Mathf.Max(1f, width - 1f)),
                    Mathf.Clamp01(junctionY / Mathf.Max(1f, height - 1f))
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static SideSample SampleSide(Color[] pixels, int width, int height, int band, int side)
        {
            int score = 0;
            float sumX = 0f;
            float sumY = 0f;
            const float alphaThreshold = 0.03f;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    bool inside = side switch
                    {
                        0 => x < band,                 // W
                        1 => x >= width - band,        // E
                        2 => y < band,                 // S
                        _ => y >= height - band        // N
                    };
                    if (!inside)
                    {
                        continue;
                    }

                    if (pixels[row + x].a <= alphaThreshold)
                    {
                        continue;
                    }

                    score++;
                    sumX += x;
                    sumY += y;
                }
            }

            if (score == 0)
            {
                return new SideSample { Side = side, Score = 0, AvgX = width * 0.5f, AvgY = height * 0.5f };
            }

            return new SideSample
            {
                Side = side,
                Score = score,
                AvgX = sumX / score,
                AvgY = sumY / score
            };
        }

        private struct SideSample
        {
            public int Side; // 0=W,1=E,2=S,3=N
            public int Score;
            public float AvgX;
            public float AvgY;
        }

        private static void ExportPngSetToProjectFolder()
        {
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                string outDir = Path.Combine(projectRoot, "GeneratedIcons");
                Directory.CreateDirectory(outDir);

                WritePng(outDir, "icon_pipe.png", DrawIconTexture(BuildingType.Pipe, 64));
                WritePng(outDir, "icon_extractor.png", DrawIconTexture(BuildingType.Extractor, 64));
                WritePng(outDir, "icon_storage.png", DrawIconTexture(BuildingType.Storage, 64));
                WritePng(outDir, "icon_mixer.png", DrawIconTexture(BuildingType.Mixer, 64));
                WritePng(outDir, "icon_generator.png", DrawIconTexture(BuildingType.Generator, 64));
                WritePng(outDir, "icon_powerpole.png", DrawIconTexture(BuildingType.PowerPole, 64));
                WritePng(outDir, "icon_market.png", DrawIconTexture(BuildingType.MarketTerminal, 64));
                WritePng(outDir, "icon_connector.png", DrawIconTexture(BuildingType.PipeConnector, 64));
                WritePng(outDir, "icon_splitter.png", DrawIconTexture(BuildingType.PipeSplitter, 64));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"BuildingSpriteFactory: PNG export skipped. {e.Message}");
            }
        }

        private static void WritePng(string outDir, string fileName, Texture2D texture)
        {
            string path = Path.Combine(outDir, fileName);
            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(path, png);
        }

        private static Texture2D NewTexture(int size, Color fill)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var arr = new Color[size * size];
            for (int i = 0; i < arr.Length; i++) arr[i] = fill;
            tex.SetPixels(arr);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        private static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color c, bool onlyBorder)
        {
            for (int py = y; py < y + h; py++)
            {
                for (int px = x; px < x + w; px++)
                {
                    if (px < 0 || py < 0 || px >= tex.width || py >= tex.height) continue;
                    bool border = px == x || px == x + w - 1 || py == y || py == y + h - 1;
                    if (!onlyBorder || border)
                    {
                        tex.SetPixel(px, py, c);
                    }
                }
            }
        }
    }
}
