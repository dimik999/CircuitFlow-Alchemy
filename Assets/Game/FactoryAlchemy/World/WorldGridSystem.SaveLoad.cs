using System;
using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Models;
using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public partial class WorldGridSystem : MonoBehaviour
    {
        public string SaveToJson()
        {
            var save = new SaveState();
            save.Width = _width;
            save.Height = _height;
            save.MapSeed = _mapSeed;
            foreach (var inv in _inventory)
            {
                save.Inventory.Add(new InventoryEntry { Key = inv.Key, Value = inv.Value });
            }

            foreach (var node in _resourceNodes)
            {
                save.ResourceNodes.Add(new ResourceNodeEntry
                {
                    X = node.Key.x,
                    Y = node.Key.y,
                    Type = node.Value.ToString()
                });
            }

            foreach (var pair in _buildings)
            {
                save.Buildings.Add(new BuildingEntry
                {
                    X = pair.Key.x,
                    Y = pair.Key.y,
                    Type = pair.Value.Type.ToString(),
                    DirX = pair.Value.Direction.x,
                    DirY = pair.Value.Direction.y,
                    BufferType = pair.Value.BufferType.ToString(),
                    BufferAmount = pair.Value.BufferAmount
                });
            }

            foreach (var storagePair in _storageInventories)
            {
                foreach (var item in storagePair.Value)
                {
                    if (item.Value <= 0.001f) continue;
                    save.StorageEntries.Add(new StorageEntry
                    {
                        X = storagePair.Key.x,
                        Y = storagePair.Key.y,
                        Key = item.Key,
                        Value = item.Value
                    });
                }
            }

            save.ExtractRateMultiplier = _extractRateMultiplier;
            save.MixerOutputMultiplier = _mixerOutputMultiplier;
            save.PowerReach = _powerReach;

            return JsonUtility.ToJson(save);
        }

        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var save = JsonUtility.FromJson<SaveState>(json);
            if (save == null)
            {
                return;
            }

            foreach (var b in _buildings.Values)
            {
                if (b.View != null)
                {
                    Destroy(b.View);
                }
            }

            foreach (var tile in _tiles.Values)
            {
                if (tile != null)
                {
                    Destroy(tile);
                }
            }

            _buildings.Clear();
            _inventory.Clear();
            _resourceNodes.Clear();
            _tiles.Clear();

            if (save.Width > 0) _width = save.Width;
            if (save.Height > 0) _height = save.Height;
            if (save.MapSeed > 0) _mapSeed = save.MapSeed;

            GenerateGround();

            if (save.ResourceNodes != null && save.ResourceNodes.Count > 0)
            {
                foreach (var node in save.ResourceNodes)
                {
                    var cell = new Vector2Int(node.X, node.Y);
                    if (!IsInside(cell))
                    {
                        continue;
                    }

                    if (!Enum.TryParse(node.Type, out EssenceType type))
                    {
                        continue;
                    }

                    _resourceNodes[cell] = type;
                    ApplyResourceNodeTint(cell, type);
                }
            }
            else
            {
                if (_mapSeed <= 0)
                {
                    _mapSeed = ComputeDeterministicSeedFromText(json);
                }
                GenerateResourceNodes(new System.Random(_mapSeed));
            }

            foreach (var inv in save.Inventory)
            {
                _inventory[inv.Key] = Mathf.Max(0f, inv.Value);
            }

            foreach (var entry in save.Buildings)
            {
                if (!Enum.TryParse(entry.Type, out BuildingType type))
                {
                    continue;
                }

                var cell = new Vector2Int(entry.X, entry.Y);
                var dir = new Vector2Int(entry.DirX, entry.DirY);
                if (!TryPlace(type, cell, dir))
                {
                    continue;
                }

                if (_buildings.TryGetValue(cell, out var b))
                {
                    if (Enum.TryParse(entry.BufferType, out EssenceType bufferType))
                    {
                        b.BufferType = bufferType;
                    }

                    b.BufferAmount = Mathf.Max(0f, entry.BufferAmount);
                }
            }

            if (save.StorageEntries != null)
            {
                foreach (var entry in save.StorageEntries)
                {
                    var c = new Vector2Int(entry.X, entry.Y);
                    if (!_buildings.TryGetValue(c, out var b) || b.Type != BuildingType.Storage)
                    {
                        continue;
                    }

                    AddToStorage(c, entry.Key, Mathf.Max(0f, entry.Value));
                }
            }

            RemoveLegacyDirectionArrowMarkers();
            RebuildAllBuildingVisuals();
            AutoOrientPipesAfterLoad();
            RefreshAllPipeVisuals();
            UpdateTransportFlowVisuals();

            _extractRateMultiplier = Mathf.Clamp(save.ExtractRateMultiplier <= 0f ? 1f : save.ExtractRateMultiplier, 1f, 4f);
            _mixerOutputMultiplier = Mathf.Clamp(save.MixerOutputMultiplier <= 0f ? 1f : save.MixerOutputMultiplier, 1f, 3f);
            _powerReach = Mathf.Clamp(save.PowerReach <= 0 ? 4 : save.PowerReach, 4, 8);
            RebuildPowerNetwork();
        }

        public void RebuildAllBuildingVisuals()
        {
            BuildingSpriteFactory.InvalidateCaches();
            var cells = new List<Vector2Int>(_buildings.Keys);
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                if (!_buildings.TryGetValue(cell, out var data) || data.View == null)
                {
                    continue;
                }

                var sr = data.View.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = BuildingSpriteFactory.GetWorldSprite(data.Type);
                }

                data.View.transform.localScale = Vector3.one;
                data.View.transform.position = CellToWorld(cell, -0.2f);
                UpdateRotation(data);
                if (data.Type == BuildingType.Pipe)
                {
                    ApplyTransportVisualPosition(cell, data);
                }
            }
        }
    }
}
