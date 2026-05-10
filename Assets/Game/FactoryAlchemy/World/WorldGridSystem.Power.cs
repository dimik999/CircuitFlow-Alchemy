using System;
using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Models;
using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public partial class WorldGridSystem : MonoBehaviour
    {
        private void RebuildPowerNetwork()
        {
            _poweredCells.Clear();
            var queue = new Queue<Vector2Int>();

            foreach (var pair in _buildings)
            {
                if (pair.Value.Type == BuildingType.Generator)
                {
                    _poweredCells.Add(pair.Key);
                    queue.Enqueue(pair.Key);
                }
            }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var n in Neighbors(cell))
                {
                    if (!_buildings.TryGetValue(n, out var b))
                    {
                        continue;
                    }

                    if (b.Type != BuildingType.PowerPole || _poweredCells.Contains(n))
                    {
                        continue;
                    }

                    if (Mathf.Abs(n.x - cell.x) + Mathf.Abs(n.y - cell.y) <= _powerReach)
                    {
                        _poweredCells.Add(n);
                        queue.Enqueue(n);
                    }
                }
            }

            // Buildings are powered if adjacent to powered generator/pole.
            foreach (var pair in _buildings)
            {
                if (pair.Value.Type == BuildingType.Generator || pair.Value.Type == BuildingType.PowerPole)
                {
                    continue;
                }

                foreach (var n in Neighbors(pair.Key))
                {
                    if (_poweredCells.Contains(n))
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
