using System;
using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Models;
using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public partial class WorldGridSystem : MonoBehaviour
    {
        public const float CellWorldSize = 2f;

        private readonly Dictionary<Vector2Int, GameObject> _tiles = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<Vector2Int, BuildingData> _buildings = new Dictionary<Vector2Int, BuildingData>();
        private readonly Dictionary<Vector2Int, EssenceType> _resourceNodes = new Dictionary<Vector2Int, EssenceType>();
        private readonly Dictionary<string, float> _inventory = new Dictionary<string, float>();
        private readonly Dictionary<Vector2Int, Dictionary<string, float>> _storageInventories = new Dictionary<Vector2Int, Dictionary<string, float>>();

        private float _extractTickTimer;
        private readonly HashSet<Vector2Int> _poweredCells = new HashSet<Vector2Int>();
        private float _extractRateMultiplier = 1f;
        private float _mixerOutputMultiplier = 1f;
        private int _powerReach = 4;
        private int _width = 36;
        private int _height = 24;
        private int _mapSeed;

        public int Width => _width;
        public int Height => _height;

        public IReadOnlyDictionary<string, float> Inventory => _inventory;
        public float GetInventoryAmount(string key) => _inventory.TryGetValue(key, out var amount) ? amount : 0f;
        public bool IsPoweredCell(Vector2Int cell) => _poweredCells.Contains(cell);
        public float ExtractRateMultiplier => _extractRateMultiplier;
        public float MixerOutputMultiplier => _mixerOutputMultiplier;
        public int PowerReach => _powerReach;

        public static Vector3 CellToWorld(Vector2Int cell, float z = 0f)
        {
            return new Vector3(cell.x * CellWorldSize, cell.y * CellWorldSize, z);
        }

        public static Vector2Int WorldToCell(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / CellWorldSize),
                Mathf.RoundToInt(worldPos.y / CellWorldSize)
            );
        }
        public int GetBuildingCount(BuildingType type)
        {
            int count = 0;
            foreach (var pair in _buildings)
            {
                if (pair.Value.Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        public void ApplyExtractorUpgrade(float delta)
        {
            _extractRateMultiplier = Mathf.Clamp(_extractRateMultiplier + delta, 1f, 4f);
        }

        public void ApplyMixerUpgrade(float delta)
        {
            _mixerOutputMultiplier = Mathf.Clamp(_mixerOutputMultiplier + delta, 1f, 3f);
        }

        public void ApplyPowerReachUpgrade(int delta)
        {
            _powerReach = Mathf.Clamp(_powerReach + delta, 4, 8);
        }

        public void ResetRuntimeState()
        {
            foreach (var data in _buildings.Values)
            {
                if (data.View != null)
                {
                    Destroy(data.View);
                }
            }

            _buildings.Clear();
            _inventory.Clear();
            _storageInventories.Clear();
            _poweredCells.Clear();
            _extractRateMultiplier = 1f;
            _mixerOutputMultiplier = 1f;
            _powerReach = 4;

            foreach (var tile in _tiles.Values)
            {
                if (tile != null)
                {
                    Destroy(tile);
                }
            }

            _tiles.Clear();
            _resourceNodes.Clear();
            GenerateNewWorld();
        }

        public void GenerateNewWorld()
        {
            _mapSeed = UnityEngine.Random.Range(1, int.MaxValue);
            var rng = new System.Random(_mapSeed);
            _width = rng.Next(96, 161);   // Large world width
            _height = rng.Next(64, 121);  // Large world height

            GenerateGround();
            GenerateResourceNodes(rng);
        }

        private void Start()
        {
            BuildingSpriteFactory.InvalidateCaches();
            GenerateNewWorld();
        }

        private void Update()
        {
            if (!GameUiState.IsGameplayActive)
            {
                return;
            }

            _extractTickTimer += Time.deltaTime;
            if (_extractTickTimer >= 1f)
            {
                _extractTickTimer = 0f;
                SimulateExtractionAndFlow();
            }
        }

        public bool IsResourceNode(Vector2Int cell) => _resourceNodes.ContainsKey(cell);
        public bool IsCellBlocked(Vector2Int cell) => HasBuilding(cell) || IsResourceNode(cell);

        public bool IsBlockedAtWorldPosition(Vector3 worldPos, float radius = 0f)
        {
            var cell = WorldToCell(worldPos);
            if (!IsInside(cell))
            {
                return true;
            }

            if (_resourceNodes.ContainsKey(cell))
            {
                return true;
            }

            var probe = new Vector3(worldPos.x, worldPos.y, -0.2f);
            foreach (var pair in _buildings)
            {
                var view = pair.Value?.View;
                if (view == null)
                {
                    continue;
                }

                var sr = view.GetComponent<SpriteRenderer>();
                Bounds b = sr != null
                    ? sr.bounds
                    : new Bounds(CellToWorld(pair.Key, -0.2f), new Vector3(CellWorldSize, CellWorldSize, 1f));
                b.Expand(radius * 2f);
                if (b.Contains(probe))
                {
                    return true;
                }
            }

            return false;
        }

        public Vector2Int FindNearestFreeCell(Vector2Int start, int maxRadius = 24)
        {
            if (IsInside(start) && !IsCellBlocked(start))
            {
                return start;
            }

            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int dy = radius - Mathf.Abs(dx);
                    var c1 = new Vector2Int(start.x + dx, start.y + dy);
                    var c2 = new Vector2Int(start.x + dx, start.y - dy);

                    if (IsInside(c1) && !IsCellBlocked(c1)) return c1;
                    if (IsInside(c2) && !IsCellBlocked(c2)) return c2;
                }
            }

            return Vector2Int.zero;
        }
        public bool TryGatherByHand(Vector2Int cell, float amount)
        {
            if (!_resourceNodes.TryGetValue(cell, out var type))
            {
                return false;
            }

            AddToInventory(type.ToString(), Mathf.Max(0.1f, amount));
            return true;
        }

        public bool TryConsumeInventory(string key, float amount)
        {
            if (!_inventory.TryGetValue(key, out var current) || current < amount)
            {
                return false;
            }

            float left = current - amount;
            if (left <= 0.001f)
            {
                _inventory.Remove(key);
            }
            else
            {
                _inventory[key] = left;
            }

            return true;
        }

        public bool ConsumeInventoryResource(string key, float amount)
        {
            return TryConsumeInventory(key, amount);
        }

        public void AddInventory(string key, float amount)
        {
            AddToInventory(key, amount);
        }

        public bool TryGetBuildingType(Vector2Int cell, out BuildingType type)
        {
            if (_buildings.TryGetValue(cell, out var b))
            {
                type = b.Type;
                return true;
            }

            type = BuildingType.None;
            return false;
        }

        public bool TryGetBuildingAtWorldPosition(Vector3 worldPos, out Vector2Int cell, out BuildingType type)
        {
            foreach (var pair in _buildings)
            {
                var view = pair.Value?.View;
                if (view == null)
                {
                    continue;
                }

                var renderers = view.GetComponentsInChildren<SpriteRenderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null && renderers[i].bounds.Contains(worldPos))
                    {
                        cell = pair.Key;
                        type = pair.Value.Type;
                        return true;
                    }
                }
            }

            cell = Vector2Int.zero;
            type = BuildingType.None;
            return false;
        }

        public Dictionary<string, float> GetStorageSnapshot(Vector2Int cell)
        {
            if (!_storageInventories.TryGetValue(cell, out var src))
            {
                return new Dictionary<string, float>();
            }

            return new Dictionary<string, float>(src);
        }

        public bool WithdrawFromStorage(Vector2Int cell, string key, float amount)
        {
            if (amount <= 0f || string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (!_storageInventories.TryGetValue(cell, out var storage))
            {
                return false;
            }

            if (!storage.TryGetValue(key, out var current) || current < amount)
            {
                return false;
            }

            float left = current - amount;
            if (left <= 0.001f)
            {
                storage.Remove(key);
            }
            else
            {
                storage[key] = left;
            }

            AddToInventory(key, amount);
            return true;
        }

        public Dictionary<string, float> GetMixerInputSnapshot(Vector2Int cell)
        {
            var result = new Dictionary<string, float>();
            if (!_buildings.TryGetValue(cell, out var b) || b.Type != BuildingType.Mixer)
            {
                return result;
            }

            for (int i = 0; i < b.MixInputs.Count; i++)
            {
                string key = b.MixInputs[i].Type.ToString();
                if (result.ContainsKey(key)) result[key] += b.MixInputs[i].Amount;
                else result[key] = b.MixInputs[i].Amount;
            }

            return result;
        }

        public bool TryGetBuildingOutput(Vector2Int cell, out string resourceKey, out float amount)
        {
            resourceKey = string.Empty;
            amount = 0f;
            if (!_buildings.TryGetValue(cell, out var b))
            {
                return false;
            }

            if (b.BufferAmount <= 0.001f)
            {
                return false;
            }

            resourceKey = b.BufferType.ToString();
            amount = b.BufferAmount;
            return true;
        }

        public bool TryGetBuildingDirection(Vector2Int cell, out Vector2Int direction)
        {
            direction = Vector2Int.zero;
            if (!_buildings.TryGetValue(cell, out var b))
            {
                return false;
            }

            direction = b.Direction;
            return true;
        }

        public bool TryGetBuildingLastIncomingDirection(Vector2Int cell, out Vector2Int direction)
        {
            direction = Vector2Int.zero;
            if (!_buildings.TryGetValue(cell, out var b))
            {
                return false;
            }

            direction = b.LastIncomingDir;
            return true;
        }

        public bool IsBuildingPoweredAt(Vector2Int cell)
        {
            if (!_buildings.TryGetValue(cell, out var b))
            {
                return false;
            }

            return IsBuildingPowered(cell, b);
        }

        public bool TryGetPipeConnectionMask(Vector2Int cell, out int mask)
        {
            mask = 0;
            if (!_buildings.TryGetValue(cell, out var b) || b.Type != BuildingType.Pipe)
            {
                return false;
            }

            mask = BuildPipeConnectionMask(cell, b);
            return true;
        }

        public bool TryPlace(BuildingType type, Vector2Int cell, Vector2Int direction)
        {
            if (!IsInside(cell) || _buildings.ContainsKey(cell))
            {
                return false;
            }

            var player = FindFirstObjectByType<PlayerController2D>();
            if (player != null && WorldToCell(player.transform.position) == cell)
            {
                return false;
            }

            if (type == BuildingType.Extractor && !IsResourceNode(cell))
            {
                return false;
            }

            var go = new GameObject($"{type}_{cell.x}_{cell.y}");
            go.transform.position = CellToWorld(cell, -0.2f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BuildingSpriteFactory.GetWorldSprite(type);
            sr.color = Color.white;
            go.transform.localScale = Vector3.one;

            _buildings[cell] = new BuildingData
            {
                Type = type,
                View = go,
                Direction = NormalizeDir(direction),
                BufferAmount = 0f
            };

            if (type == BuildingType.Storage && !_storageInventories.ContainsKey(cell))
            {
                _storageInventories[cell] = new Dictionary<string, float>();
            }

            TryAddDirectionArrow(_buildings[cell]);
            TryAddTransportFlowOverlay(_buildings[cell]);
            UpdateTransportFlowVisual(_buildings[cell]);
            UpdateRotation(_buildings[cell]);
            RefreshPipeVisualsAround(cell);

            return true;
        }

        public bool TryRemove(Vector2Int cell)
        {
            return TryRemove(cell, out _);
        }

        public bool TryRemove(Vector2Int cell, out BuildingType removedType)
        {
            removedType = BuildingType.None;
            if (!_buildings.TryGetValue(cell, out var data))
            {
                return false;
            }

            removedType = data.Type;
            if (data.View != null)
            {
                Destroy(data.View);
            }

            _buildings.Remove(cell);
            _storageInventories.Remove(cell);
            RefreshPipeVisualsAround(cell);
            return true;
        }

        public bool HasBuilding(Vector2Int cell) => _buildings.ContainsKey(cell);

        public EssenceType? GetNodeType(Vector2Int cell)
        {
            if (_resourceNodes.TryGetValue(cell, out var t))
            {
                return t;
            }

            return null;
        }

        public bool IsInside(Vector2Int cell)
        {
            int minX = -(Width / 2);
            int maxX = Width / 2;
            int minY = -(Height / 2);
            int maxY = Height / 2;
            return cell.x >= minX && cell.x <= maxX && cell.y >= minY && cell.y <= maxY;
        }
    }
}
