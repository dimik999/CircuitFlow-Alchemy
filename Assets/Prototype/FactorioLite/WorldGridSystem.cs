using System;
using System.Collections.Generic;
using System.IO;
using CircuitFlowAlchemy.Core.Models;
using UnityEngine;

namespace CircuitFlowAlchemy.Prototype.FactorioLite
{
    public enum BuildingType
    {
        None,
        Pipe,
        Extractor,
        Storage,
        Mixer,
        Generator,
        PowerPole,
        MarketTerminal
    }

    public class WorldGridSystem : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, GameObject> _tiles = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<Vector2Int, BuildingData> _buildings = new Dictionary<Vector2Int, BuildingData>();
        private readonly Dictionary<Vector2Int, EssenceType> _resourceNodes = new Dictionary<Vector2Int, EssenceType>();
        private readonly Dictionary<string, float> _inventory = new Dictionary<string, float>();

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

        public bool TryPlace(BuildingType type, Vector2Int cell, Vector2Int direction)
        {
            if (!IsInside(cell) || _buildings.ContainsKey(cell))
            {
                return false;
            }

            if (type == BuildingType.Extractor && !IsResourceNode(cell))
            {
                return false;
            }

            var go = new GameObject($"{type}_{cell.x}_{cell.y}");
            go.transform.position = new Vector3(cell.x, cell.y, -0.2f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BuildingSpriteFactory.GetWorldSprite(type);
            sr.color = Color.white;
            go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            _buildings[cell] = new BuildingData
            {
                Type = type,
                View = go,
                Direction = NormalizeDir(direction),
                BufferAmount = 0f
            };

            TryAddDirectionArrow(_buildings[cell]);
            UpdateRotation(_buildings[cell]);

            return true;
        }

        public bool TryRemove(Vector2Int cell)
        {
            if (!_buildings.TryGetValue(cell, out var data))
            {
                return false;
            }

            if (data.View != null)
            {
                Destroy(data.View);
            }

            _buildings.Remove(cell);
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

        private void GenerateGround()
        {
            int minX = -(Width / 2);
            int maxX = Width / 2;
            int minY = -(Height / 2);
            int maxY = Height / 2;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var tile = new GameObject($"Tile_{x}_{y}");
                    tile.transform.position = new Vector3(x, y, 0f);
                    var sr = tile.AddComponent<SpriteRenderer>();
                    bool isDark = ((x + y) & 1) == 0;
                    sr.sprite = SpriteUtil.MakeSolidSprite(isDark
                        ? new Color(0.13f, 0.17f, 0.21f)
                        : new Color(0.16f, 0.2f, 0.25f));
                    sr.sortingOrder = -10;
                    tile.transform.localScale = new Vector3(0.98f, 0.98f, 1f);
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
                        sr.color = GetNodeColor(type);
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

        private void SimulateExtractionAndFlow()
        {
            RebuildPowerNetwork();

            foreach (var pair in _buildings)
            {
                if (pair.Value.Type != BuildingType.Extractor)
                {
                    continue;
                }

                if (!IsBuildingPowered(pair.Key, pair.Value))
                {
                    continue;
                }

                if (_resourceNodes.TryGetValue(pair.Key, out var nodeType))
                {
                    pair.Value.BufferType = nodeType;
                    pair.Value.BufferAmount += _extractRateMultiplier;
                }
            }

            foreach (var pair in _buildings)
            {
                if (pair.Value.Type == BuildingType.Mixer)
                {
                    if (!IsBuildingPowered(pair.Key, pair.Value))
                    {
                        continue;
                    }

                    TryMix(pair.Value);
                }
            }

            var cells = new List<Vector2Int>(_buildings.Keys);
            foreach (var cell in cells)
            {
                if (!_buildings.TryGetValue(cell, out var source))
                {
                    continue;
                }

                if ((source.Type == BuildingType.Extractor || source.Type == BuildingType.Pipe || source.Type == BuildingType.Mixer)
                    && source.BufferAmount > 0f)
                {
                    TryTransferForward(cell, source);
                }
            }
        }

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
            if (building.Type == BuildingType.Pipe || building.Type == BuildingType.Storage)
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

        private void TryMix(BuildingData mixer)
        {
            if (mixer.MixInputs.Count == 0)
            {
                return;
            }

            float aqua = 0f;
            float ignis = 0f;
            for (int i = mixer.MixInputs.Count - 1; i >= 0; i--)
            {
                var chunk = mixer.MixInputs[i];
                if (chunk.Type == EssenceType.Aqua) aqua += chunk.Amount;
                if (chunk.Type == EssenceType.Ignis) ignis += chunk.Amount;
            }

            if (aqua < 1f || ignis < 1f || mixer.BufferAmount > 4f)
            {
                return;
            }

            ConsumeMixInput(mixer, EssenceType.Aqua, 1f);
            ConsumeMixInput(mixer, EssenceType.Ignis, 1f);
            mixer.BufferType = EssenceType.Aeris;
            mixer.BufferAmount += _mixerOutputMultiplier;
        }

        private static void ConsumeMixInput(BuildingData mixer, EssenceType type, float amount)
        {
            float remain = amount;
            for (int i = 0; i < mixer.MixInputs.Count && remain > 0f; i++)
            {
                if (mixer.MixInputs[i].Type != type)
                {
                    continue;
                }

                float used = Mathf.Min(remain, mixer.MixInputs[i].Amount);
                mixer.MixInputs[i] = new EssenceChunk(type, mixer.MixInputs[i].Amount - used);
                remain -= used;
            }

            mixer.MixInputs.RemoveAll(x => x.Amount <= 0.001f);
        }

        private void TryTransferForward(Vector2Int sourcePos, BuildingData source)
        {
            var targetPos = sourcePos + source.Direction;
            if (!_buildings.TryGetValue(targetPos, out var target))
            {
                return;
            }

            if (!CanAccept(target, source.Direction, source.BufferType))
            {
                return;
            }

            float moved = Mathf.Min(1f, source.BufferAmount);
            source.BufferAmount -= moved;

            if (target.Type == BuildingType.Storage)
            {
                AddToInventory(source.BufferType.ToString(), moved);
                return;
            }

            if (target.Type == BuildingType.Mixer)
            {
                target.MixInputs.Add(new EssenceChunk(source.BufferType, moved));
                return;
            }

            target.BufferType = source.BufferType;
            target.BufferAmount += moved;
        }

        private static bool CanAccept(BuildingData target, Vector2Int incomingDir, EssenceType incomingType)
        {
            if (target.Type == BuildingType.Storage)
            {
                return true;
            }

            if (target.Type == BuildingType.Pipe)
            {
                // Pipe accepts only from its back side and sends forward.
                return target.Direction == incomingDir;
            }

            if (target.Type == BuildingType.Mixer)
            {
                // Mixer receives from side/back, not from output side.
                if (target.Direction == -incomingDir)
                {
                    return false;
                }

                return incomingType == EssenceType.Aqua || incomingType == EssenceType.Ignis;
            }

            return false;
        }

        private static IEnumerable<Vector2Int> Neighbors(Vector2Int c)
        {
            yield return c + Vector2Int.up;
            yield return c + Vector2Int.down;
            yield return c + Vector2Int.left;
            yield return c + Vector2Int.right;
        }

        private void AddToInventory(string key, float amount)
        {
            if (_inventory.ContainsKey(key))
            {
                _inventory[key] += amount;
            }
            else
            {
                _inventory[key] = amount;
            }
        }

        private static Color GetColor(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Pipe:
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

        private static void UpdateRotation(BuildingData data)
        {
            if (data?.View == null)
            {
                return;
            }

            float angle = 0f;
            if (data.Direction == Vector2Int.up) angle = 90f;
            if (data.Direction == Vector2Int.left) angle = 180f;
            if (data.Direction == Vector2Int.down) angle = 270f;
            data.View.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static void TryAddDirectionArrow(BuildingData data)
        {
            if (data == null || data.View == null)
            {
                return;
            }

            if (data.Type == BuildingType.Storage)
            {
                return;
            }

            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(data.View.transform, false);
            arrow.transform.localPosition = new Vector3(0.2f, 0f, -0.05f);
            arrow.transform.localScale = new Vector3(0.35f, 0.15f, 1f);

            var sr = arrow.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteUtil.MakeSolidSprite(new Color(0.1f, 0.1f, 0.1f, 0.9f));
            sr.sortingOrder = 5;
        }

        public string SaveToJson()
        {
            var save = new SaveState();
            foreach (var inv in _inventory)
            {
                save.Inventory.Add(new InventoryEntry { Key = inv.Key, Value = inv.Value });
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

            _buildings.Clear();
            _inventory.Clear();

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

            _extractRateMultiplier = Mathf.Clamp(save.ExtractRateMultiplier <= 0f ? 1f : save.ExtractRateMultiplier, 1f, 4f);
            _mixerOutputMultiplier = Mathf.Clamp(save.MixerOutputMultiplier <= 0f ? 1f : save.MixerOutputMultiplier, 1f, 3f);
            _powerReach = Mathf.Clamp(save.PowerReach <= 0 ? 4 : save.PowerReach, 4, 8);
        }

        private sealed class BuildingData
        {
            public BuildingType Type;
            public GameObject View;
            public Vector2Int Direction;
            public EssenceType BufferType;
            public float BufferAmount;
            public List<EssenceChunk> MixInputs = new List<EssenceChunk>();
        }

        private readonly struct EssenceChunk
        {
            public readonly EssenceType Type;
            public readonly float Amount;

            public EssenceChunk(EssenceType type, float amount)
            {
                Type = type;
                Amount = amount;
            }
        }

        [Serializable]
        private sealed class SaveState
        {
            public List<BuildingEntry> Buildings = new List<BuildingEntry>();
            public List<InventoryEntry> Inventory = new List<InventoryEntry>();
            public float ExtractRateMultiplier = 1f;
            public float MixerOutputMultiplier = 1f;
            public int PowerReach = 4;
        }

        [Serializable]
        private sealed class BuildingEntry
        {
            public int X;
            public int Y;
            public string Type;
            public int DirX;
            public int DirY;
            public string BufferType;
            public float BufferAmount;
        }

        [Serializable]
        private sealed class InventoryEntry
        {
            public string Key;
            public float Value;
        }
    }

    public static class BuildingSpriteFactory
    {
        private static readonly Dictionary<BuildingType, Sprite> WorldSprites = new Dictionary<BuildingType, Sprite>();
        private static readonly Dictionary<BuildingType, Texture2D> IconTextures = new Dictionary<BuildingType, Texture2D>();
        private static Sprite _arrowSprite;
        private static bool _initialized;

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            ExportPngSetToProjectFolder();
        }

        public static Sprite GetWorldSprite(BuildingType type)
        {
            EnsureInitialized();
            if (WorldSprites.TryGetValue(type, out var sprite))
            {
                return sprite;
            }

            var tex = DrawIconTexture(type, 32);
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
            WorldSprites[type] = sprite;
            return sprite;
        }

        public static Texture2D GetIconTexture(BuildingType type)
        {
            EnsureInitialized();
            if (IconTextures.TryGetValue(type, out var tex))
            {
                return tex;
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
                default:
                    DrawRect(tex, 5, 5, size - 10, size - 10, Color.gray, false);
                    break;
            }

            tex.Apply();
            return tex;
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
