using System;
using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Models;
using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public partial class WorldGridSystem : MonoBehaviour
    {
        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private void RebuildPowerNetwork()
        {
            _poweredCells.Clear();

            foreach (var pair in _buildings)
            {
                if (pair.Value.Type == BuildingType.Generator)
                {
                    _poweredCells.Add(pair.Key);
                }
            }

            // Spread through poles within PowerReach (Manhattan), not only to 4 neighbors.
            bool expanded;
            do
            {
                expanded = false;
                var poweredSnapshot = new List<Vector2Int>(_poweredCells);
                foreach (var source in poweredSnapshot)
                {
                    if (!_buildings.TryGetValue(source, out var sourceData))
                    {
                        continue;
                    }

                    if (sourceData.Type != BuildingType.Generator && sourceData.Type != BuildingType.PowerPole)
                    {
                        continue;
                    }

                    foreach (var pair in _buildings)
                    {
                        if (pair.Value.Type != BuildingType.PowerPole || _poweredCells.Contains(pair.Key))
                        {
                            continue;
                        }

                        int dist = ManhattanDistance(source, pair.Key);
                        if (dist > 0 && dist <= _powerReach)
                        {
                            _poweredCells.Add(pair.Key);
                            expanded = true;
                        }
                    }
                }
            }
            while (expanded);

            // Production buildings are powered when within PowerReach of any powered generator/pole.
            foreach (var pair in _buildings)
            {
                if (pair.Value.Type == BuildingType.Generator || pair.Value.Type == BuildingType.PowerPole)
                {
                    continue;
                }

                if (pair.Value.Type == BuildingType.Pipe || pair.Value.Type == BuildingType.PipeCorner
                    || pair.Value.Type == BuildingType.Storage || pair.Value.Type == BuildingType.PipeConnector
                    || pair.Value.Type == BuildingType.PipeSplitter)
                {
                    continue;
                }

                foreach (var source in _poweredCells)
                {
                    if (!_buildings.TryGetValue(source, out var sourceData))
                    {
                        continue;
                    }

                    if (sourceData.Type != BuildingType.Generator && sourceData.Type != BuildingType.PowerPole)
                    {
                        continue;
                    }

                    int dist = ManhattanDistance(pair.Key, source);
                    if (dist > 0 && dist <= _powerReach)
                    {
                        _poweredCells.Add(pair.Key);
                        break;
                    }
                }
            }

            UpdatePowerVisuals();
        }

        private bool IsBuildingPowered(Vector2Int pos, BuildingData building)
        {
            if (building.Type == BuildingType.Pipe || building.Type == BuildingType.PipeCorner || building.Type == BuildingType.Storage
                || building.Type == BuildingType.PipeConnector || building.Type == BuildingType.PipeSplitter)
            {
                return true;
            }

            return _poweredCells.Contains(pos);
        }

        public void SetWorldVisible(bool isVisible)
        {
            foreach (var tile in _tiles.Values)
            {
                if (tile != null)
                {
                    var sr = tile.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.enabled = isVisible;
                }
            }

            foreach (var b in _buildings.Values)
            {
                if (b?.View == null) continue;
                var sr = b.View.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = isVisible;
                var childRenderers = b.View.GetComponentsInChildren<SpriteRenderer>();
                foreach (var cr in childRenderers)
                {
                    cr.enabled = isVisible;
                }
            }
        }

        private void UpdatePowerVisuals()
        {
            foreach (var pair in _buildings)
            {
                var b = pair.Value;
                if (b.View == null)
                {
                    continue;
                }

                var sr = b.View.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    continue;
                }

                bool requiresPower = b.Type == BuildingType.Extractor || b.Type == BuildingType.Mixer;
                bool powered = IsBuildingPowered(pair.Key, b);
                sr.color = requiresPower && !powered
                    ? new Color(0.45f, 0.45f, 0.45f, 1f)
                    : Color.white;
            }
        }
    }
}
