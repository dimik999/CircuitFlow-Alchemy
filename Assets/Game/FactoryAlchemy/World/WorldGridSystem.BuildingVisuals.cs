using System;
using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Models;
using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public partial class WorldGridSystem : MonoBehaviour
    {
        private static Sprite _transportOverlaySprite;

        private static Color GetColor(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Pipe:
                case BuildingType.PipeCorner:
                    return new Color(0.7f, 0.7f, 0.75f);
                case BuildingType.Extractor:
                    return new Color(0.93f, 0.56f, 0.18f);
                case BuildingType.Storage:
                    return new Color(0.3f, 0.9f, 0.4f);
                case BuildingType.Mixer:
                    return new Color(0.7f, 0.4f, 0.95f);
                case BuildingType.Generator:
                    return new Color(0.96f, 0.88f, 0.2f);
                case BuildingType.PowerPole:
                    return new Color(0.9f, 0.75f, 0.5f);
                case BuildingType.MarketTerminal:
                    return new Color(0.2f, 0.95f, 0.95f);
                case BuildingType.PipeConnector:
                    return new Color(0.95f, 0.72f, 0.35f);
                case BuildingType.PipeSplitter:
                    return new Color(0.98f, 0.56f, 0.35f);
                default:
                    return Color.white;
            }
        }

        private static Color GetNodeColor(EssenceType type)
        {
            switch (type)
            {
                case EssenceType.Aqua:
                    return new Color(0.2f, 0.45f, 0.9f);
                case EssenceType.Ignis:
                    return new Color(0.95f, 0.35f, 0.2f);
                case EssenceType.Terra:
                    return new Color(0.55f, 0.38f, 0.2f);
                default:
                    return Color.gray;
            }
        }

        private void ApplyResourceNodeTint(Vector2Int cell, EssenceType type)
        {
            if (_tiles.TryGetValue(cell, out var tile))
            {
                var sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.Lerp(GetNodeColor(type), Color.white, 0.18f);
                }
            }
        }

        private static Vector2Int NormalizeDir(Vector2Int dir)
        {
            if (dir == Vector2Int.zero)
            {
                return Vector2Int.right;
            }

            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            {
                return new Vector2Int(dir.x >= 0 ? 1 : -1, 0);
            }

            return new Vector2Int(0, dir.y >= 0 ? 1 : -1);
        }

        private static int ComputeDeterministicSeedFromText(string text)
        {
            unchecked
            {
                int hash = 23;
                if (!string.IsNullOrEmpty(text))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        hash = (hash * 31) + text[i];
                    }
                }

                if (hash == int.MinValue) hash = int.MaxValue;
                hash = Mathf.Abs(hash);
                return hash == 0 ? 1 : hash;
            }
        }

        private static void UpdateRotation(BuildingData data)
        {
            if (data?.View == null)
            {
                return;
            }

            if (data.Type == BuildingType.Pipe || data.Type == BuildingType.PowerPole)
            {
                data.View.transform.rotation = Quaternion.identity;
                return;
            }
            float angle = 0f;
            if (data.Direction == Vector2Int.up) angle = 90f;
            if (data.Direction == Vector2Int.left) angle = 180f;
            if (data.Direction == Vector2Int.down) angle = 270f;
            data.View.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static void ApplyPlacementVisualOffset(BuildingData data, Vector2Int cell)
        {
            if (data?.View == null)
            {
                return;
            }

            data.View.transform.position = CellToWorld(cell, -0.2f);
        }

        /// <summary>Removes legacy dark rectangle markers from older saves / builds.</summary>
        public void RemoveLegacyDirectionArrowMarkers()
        {
            foreach (var pair in _buildings)
            {
                if (pair.Value?.View == null)
                {
                    continue;
                }

                Transform root = pair.Value.View.transform;
                for (int i = root.childCount - 1; i >= 0; i--)
                {
                    Transform child = root.GetChild(i);
                    if (child.name == "Arrow")
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }

        private static bool IsTransportCarrier(BuildingType type)
        {
            return type == BuildingType.Pipe
                || type == BuildingType.PipeCorner
                || type == BuildingType.PipeConnector
                || type == BuildingType.PipeSplitter;
        }

        private static void TryAddTransportFlowOverlay(BuildingData data)
        {
            if (data == null || data.View == null || !IsTransportCarrier(data.Type))
            {
                return;
            }

            var overlay = new GameObject("FlowOverlay");
            overlay.transform.SetParent(data.View.transform, false);
            overlay.transform.localPosition = new Vector3(0f, 0f, -0.035f);
            overlay.transform.localScale = new Vector3(0.42f * CellWorldSize, 0.42f * CellWorldSize, 1f);

            var sr = overlay.AddComponent<SpriteRenderer>();
            if (_transportOverlaySprite == null)
            {
                _transportOverlaySprite = SpriteUtil.MakeSolidSprite(Color.white);
            }

            sr.sprite = _transportOverlaySprite;
            sr.sortingOrder = -1;
            sr.enabled = false;
            data.FlowOverlay = sr;
        }

        private static Color GetFlowColor(EssenceType type)
        {
            switch (type)
            {
                case EssenceType.Aqua:
                    return new Color(0.35f, 0.75f, 1f, 1f);
                case EssenceType.Ignis:
                    return new Color(1f, 0.52f, 0.25f, 1f);
                case EssenceType.Terra:
                    return new Color(0.48f, 0.78f, 0.36f, 1f);
                case EssenceType.Aeris:
                    return new Color(0.78f, 0.95f, 1f, 1f);
                default:
                    return new Color(0.9f, 0.9f, 0.9f, 1f);
            }
        }

        private void UpdateTransportFlowVisual(BuildingData data)
        {
            if (data == null || data.FlowOverlay == null || !IsTransportCarrier(data.Type))
            {
                return;
            }

            if (!GameSettings.ShowFlowIndicators)
            {
                data.FlowOverlay.enabled = false;
                return;
            }

            float fill = Mathf.Clamp01(data.BufferAmount / 1f);
            float pulse = data.FlowPulseTicks > 0 ? (data.FlowPulseTicks >= 2 ? 0.5f : 0.28f) : 0f;
            float intensity = Mathf.Max(fill, pulse);
            if (intensity <= 0.001f)
            {
                data.FlowOverlay.enabled = false;
                return;
            }

            float alpha = Mathf.Lerp(0.14f, 0.58f, intensity);
            EssenceType visualType = fill > 0.001f ? data.BufferType : data.FlowPulseType;
            var c = GetFlowColor(visualType);
            c.a = alpha;
            data.FlowOverlay.color = c;
            data.FlowOverlay.enabled = true;
        }

        private void UpdateTransportFlowVisuals()
        {
            if (!GameSettings.ShowFlowIndicators)
            {
                SetFlowIndicatorsVisible(false);
                return;
            }

            foreach (var pair in _buildings)
            {
                UpdateTransportFlowVisual(pair.Value);
            }
        }

        public void SetFlowIndicatorsVisible(bool isVisible)
        {
            var allRenderers = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
            for (int i = 0; i < allRenderers.Length; i++)
            {
                var r = allRenderers[i];
                if (r == null || r.gameObject == null || r.gameObject.name != "FlowOverlay")
                {
                    continue;
                }

                if (!r.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!isVisible)
                {
                    r.enabled = false;
                    var c = r.color;
                    c.a = 0f;
                    r.color = c;
                }
            }

            foreach (var pair in _buildings)
            {
                var data = pair.Value;
                if (data?.View == null || !IsTransportCarrier(data.Type))
                {
                    continue;
                }

                var overlays = data.View.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < overlays.Length; i++)
                {
                    var r = overlays[i];
                    if (r == null || r.gameObject.name != "FlowOverlay")
                    {
                        continue;
                    }

                    r.sortingOrder = -1;
                    if (!isVisible)
                    {
                        r.enabled = false;
                    }
                    data.FlowOverlay = r;
                }
            }
        }

        public void RefreshFlowIndicators()
        {
            SetFlowIndicatorsVisible(GameSettings.ShowFlowIndicators);
            UpdateTransportFlowVisuals();
        }
    }
}
