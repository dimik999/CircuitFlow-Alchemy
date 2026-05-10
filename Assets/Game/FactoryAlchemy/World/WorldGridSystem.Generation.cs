using System;
using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Models;
using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public partial class WorldGridSystem : MonoBehaviour
    {
        private void GenerateGround()
        {
            int minX = -(Width / 2);
            int maxX = Width / 2;
            int minY = -(Height / 2);
            int maxY = Height / 2;

            // Use a tiny shared tileset to avoid creating thousands of unique textures.
            var darkBase = new Color(0.16f, 0.22f, 0.30f);
            var darkAccent = new Color(0.08f, 0.52f, 0.70f);
            var lightBase = new Color(0.20f, 0.27f, 0.36f);
            var lightAccent = new Color(0.14f, 0.62f, 0.82f);

            var darkVariants = new[]
            {
                SpriteUtil.MakeGroundTileSprite(darkBase, darkAccent, 11, 17),
                SpriteUtil.MakeGroundTileSprite(darkBase, darkAccent, 23, 31)
            };
            var lightVariants = new[]
            {
                SpriteUtil.MakeGroundTileSprite(lightBase, lightAccent, 7, 13),
                SpriteUtil.MakeGroundTileSprite(lightBase, lightAccent, 19, 29)
            };

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var tile = new GameObject($"Tile_{x}_{y}");
                    tile.transform.position = CellToWorld(new Vector2Int(x, y), 0f);
                    var sr = tile.AddComponent<SpriteRenderer>();
                    bool isDark = ((x + y) & 1) == 0;
                    int variant = ((x * 17 + y * 31) & 1);
                    sr.sprite = isDark ? darkVariants[variant] : lightVariants[variant];
                    sr.sortingOrder = -10;
                    tile.transform.localScale = new Vector3(CellWorldSize, CellWorldSize, 1f);
                    _tiles[new Vector2Int(x, y)] = tile;
                }
            }
        }

        private void GenerateResourceNodes(System.Random rng)
        {
            int nodeCount = Mathf.Max(28, (_width * _height) / 320);
            for (int i = 0; i < nodeCount; i++)
            {
                int x = rng.Next(-(_width / 2) + 2, (_width / 2) - 1);
                int y = rng.Next(-(_height / 2) + 2, (_height / 2) - 1);
                int roll = rng.Next(0, 100);
                EssenceType type;
                if (roll < 40) type = EssenceType.Aqua;
                else if (roll < 75) type = EssenceType.Ignis;
                else type = EssenceType.Terra;

                int sizeRoll = rng.Next(0, 100);
                int radius = sizeRoll < 45 ? 0 : (sizeRoll < 85 ? 1 : 2); // small/medium/large deposit
                var center = new Vector2Int(x, y);
                foreach (var pos in EnumerateDepositCells(center, radius))
                {
                    if (!IsInside(pos) || _resourceNodes.ContainsKey(pos))
                    {
                        continue;
                    }

                    _resourceNodes[pos] = type;
                    if (_tiles.TryGetValue(pos, out var tile))
                    {
                        var sr = tile.GetComponent<SpriteRenderer>();
                        sr.color = Color.Lerp(GetNodeColor(type), Color.white, 0.18f);
                    }
                }
            }
        }

        private static IEnumerable<Vector2Int> EnumerateDepositCells(Vector2Int center, int radius)
        {
            if (radius <= 0)
            {
                yield return center;
                yield break;
            }

            // diamond-like cluster with mild irregularity for natural look
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int manhattan = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (manhattan <= radius + 1 && (dx + dy + center.x + center.y) % 3 != 0)
                    {
                        yield return new Vector2Int(center.x + dx, center.y + dy);
                    }
                }
            }
            yield return center;
        }
    }
}
