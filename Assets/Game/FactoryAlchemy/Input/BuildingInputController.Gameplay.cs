using UnityEngine;
using System.Collections.Generic;
using System;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public partial class BuildingInputController : MonoBehaviour
    {
        private void CreatePlacementPreview()
        {
            _preview = new GameObject("PlacementPreview");
            _previewRenderer = _preview.AddComponent<SpriteRenderer>();
            _previewRenderer.sprite = BuildingSpriteFactory.GetWorldSprite(BuildingType.Pipe);
            _previewRenderer.sortingOrder = 20;
            _preview.transform.localScale = Vector3.one;

            var arrow = new GameObject("PlacementArrow");
            arrow.transform.SetParent(_preview.transform, false);
            arrow.transform.localPosition = new Vector3(0.24f * WorldGridSystem.CellWorldSize, 0f, -0.04f);
            _previewArrowRenderer = arrow.AddComponent<SpriteRenderer>();
            _previewArrowRenderer.sprite = BuildingSpriteFactory.GetArrowSprite();
            _previewArrowRenderer.sortingOrder = 21;
            _previewArrowRenderer.color = new Color(1f, 1f, 1f, 0.85f);
        }

        private void UpdatePreview(Vector2Int cell, bool canPlace)
        {
            if (_preview == null)
            {
                return;
            }

            if (_selected == BuildingType.None)
            {
                _preview.SetActive(false);
                return;
            }
            if (!_preview.activeSelf)
            {
                _preview.SetActive(true);
            }

            _preview.transform.position = WorldGridSystem.CellToWorld(cell, -0.3f);
            Vector2Int previewDir = _direction;
            bool hasPipeConnection = false;
            if (_selected == BuildingType.Pipe && _world != null)
            {
                _world.TryGetPreferredPipeFlowDirection(cell, _direction, out previewDir, out hasPipeConnection);
            }

            _preview.transform.rotation = Quaternion.Euler(0f, 0f, DirToAngle(previewDir));
            _previewRenderer.sprite = BuildingSpriteFactory.GetWorldSprite(_selected);
            _previewRenderer.color = canPlace
                ? new Color(0.3f, 1f, 0.4f, 0.35f)
                : new Color(1f, 0.25f, 0.25f, 0.35f);
            if (_previewArrowRenderer != null)
            {
                _previewArrowRenderer.enabled = _selected != BuildingType.None;
                if (_selected == BuildingType.Pipe)
                {
                    // Pipe preview highlight: cyan when direction is working with neighbors.
                    _previewArrowRenderer.color = canPlace
                        ? (hasPipeConnection ? new Color(0.3f, 0.9f, 1f, 0.95f) : new Color(0.85f, 0.85f, 0.85f, 0.8f))
                        : new Color(1f, 0.4f, 0.4f, 0.9f);
                }
                else
                {
                    _previewArrowRenderer.color = canPlace
                        ? new Color(1f, 1f, 1f, 0.9f)
                        : new Color(1f, 0.4f, 0.4f, 0.9f);
                }
            }
        }

        private bool CanPlaceSelected(Vector2Int cell)
        {
            if (_selected == BuildingType.None)
            {
                return false;
            }

            if (!_world.IsInside(cell) || _world.HasBuilding(cell))
            {
                return false;
            }

            if (_selected == BuildingType.Extractor)
            {
                return _world.IsResourceNode(cell) && GetCraftedCount(_selected) > 0;
            }

            return GetCraftedCount(_selected) > 0;
        }

        private static float DirToAngle(Vector2Int dir)
        {
            if (dir == Vector2Int.up) return 90f;
            if (dir == Vector2Int.left) return 180f;
            if (dir == Vector2Int.down) return 270f;
            return 0f;
        }

        private bool IsMixerUnlocked()
        {
            return _currentGuildOrderIndex >= 1;
        }

        private bool IsPowerUnlocked()
        {
            return _currentGuildOrderIndex >= 2;
        }

        private bool IsBuildingUnlocked(BuildingType type)
        {
            if (type == BuildingType.None || type == BuildingType.Pipe || type == BuildingType.PipeCorner || type == BuildingType.PipeConnector || type == BuildingType.PipeSplitter || type == BuildingType.Extractor || type == BuildingType.Storage)
            {
                return true;
            }
            if (type == BuildingType.Mixer)
            {
                return IsMixerUnlocked();
            }
            if (type == BuildingType.Generator || type == BuildingType.PowerPole || type == BuildingType.MarketTerminal)
            {
                return IsPowerUnlocked();
            }
            return true;
        }

        private bool CanAfford(BuildingType type)
        {
            var costs = GetBuildCost(type);
            for (int i = 0; i < costs.Count; i++)
            {
                if (_world.GetInventoryAmount(costs[i].resource) < costs[i].amount)
                {
                    return false;
                }
            }
            return true;
        }

        private int GetCraftedCount(BuildingType type)
        {
            return _craftedItems.TryGetValue(type, out var count) ? count : 0;
        }

        private void AddCraftedItem(BuildingType type, int amount)
        {
            if (type == BuildingType.None || amount <= 0)
            {
                return;
            }

            _craftedItems[type] = GetCraftedCount(type) + amount;
        }

        private bool TryConsumeCraftedItem(BuildingType type)
        {
            if (type == BuildingType.None)
            {
                return false;
            }

            int count = GetCraftedCount(type);
            if (count <= 0)
            {
                return false;
            }

            _craftedItems[type] = count - 1;
            return true;
        }

        private int TryQueueCraft(BuildingType type, int amount)
        {
            if (type == BuildingType.None || amount <= 0)
            {
                return 0;
            }

            int queued = 0;
            for (int n = 0; n < amount; n++)
            {
                var costs = GetBuildCost(type);
                bool ok = true;
                int consumed = 0;
                for (int i = 0; i < costs.Count; i++)
                {
                    if (_world.ConsumeInventoryResource(costs[i].resource, costs[i].amount))
                    {
                        consumed++;
                    }
                    else
                    {
                        ok = false;
                        for (int j = 0; j < consumed; j++)
                        {
                            _world.AddInventory(costs[j].resource, costs[j].amount);
                        }
                        break;
                    }
                }

                if (!ok)
                {
                    break;
                }

                _craftQueue.Add(type);
                queued++;
            }

            return queued;
        }

        private void UpdateCraftingQueue(float deltaTime)
        {
            if (_screen != UiScreen.InGame || _isPauseMenuOpen || _showSettings)
            {
                return;
            }

            if (_isCraftQueuePaused)
            {
                return;
            }

            if (!_activeCraftType.HasValue)
            {
                if (_craftQueue.Count == 0)
                {
                    return;
                }
                _activeCraftType = _craftQueue[0];
                _craftQueue.RemoveAt(0);
                _activeCraftProgress = 0f;
            }

            _activeCraftProgress += Mathf.Max(0f, deltaTime);
            float duration = GetCraftDuration(_activeCraftType.Value);
            if (_activeCraftProgress >= duration)
            {
                AddCraftedItem(_activeCraftType.Value, 1);
                _activeCraftType = null;
                _activeCraftProgress = 0f;
            }
        }

        private void CancelLastQueuedCraft()
        {
            if (_craftQueue.Count == 0)
            {
                return;
            }

            int last = _craftQueue.Count - 1;
            var type = _craftQueue[last];
            _craftQueue.RemoveAt(last);
            RefundRecipeCost(type);
            ClampSelectedCraftQueueIndex();
            _hint = $"Отменено: {type} (ресурсы возвращены)";
        }

        private void CancelAllQueuedCraft()
        {
            int refunded = 0;
            for (int i = 0; i < _craftQueue.Count; i++)
            {
                RefundRecipeCost(_craftQueue[i]);
                refunded++;
            }
            _craftQueue.Clear();
            _selectedCraftQueueIndex = -1;

            if (_activeCraftType.HasValue)
            {
                RefundRecipeCost(_activeCraftType.Value);
                _activeCraftType = null;
                _activeCraftProgress = 0f;
                refunded++;
            }

            _hint = refunded > 0
                ? $"Отменено задач: {refunded} (ресурсы возвращены)"
                : "Очередь крафта пуста";
        }

        private void ToggleCraftQueuePause()
        {
            _isCraftQueuePaused = !_isCraftQueuePaused;
            _hint = _isCraftQueuePaused ? "Очередь крафта: пауза" : "Очередь крафта: продолжено";
        }

        private void MoveSelectedCraftQueueItem(int direction)
        {
            if (_selectedCraftQueueIndex < 0 || _selectedCraftQueueIndex >= _craftQueue.Count || _craftQueue.Count < 2)
            {
                return;
            }

            int target = Mathf.Clamp(_selectedCraftQueueIndex + direction, 0, _craftQueue.Count - 1);
            if (target == _selectedCraftQueueIndex)
            {
                return;
            }

            var tmp = _craftQueue[_selectedCraftQueueIndex];
            _craftQueue[_selectedCraftQueueIndex] = _craftQueue[target];
            _craftQueue[target] = tmp;
            _selectedCraftQueueIndex = target;
            _hint = $"Приоритет изменен: {_craftQueue[_selectedCraftQueueIndex]} -> #{_selectedCraftQueueIndex + 1}";
        }

        private void MoveSelectedCraftQueueToFront()
        {
            if (_selectedCraftQueueIndex <= 0 || _selectedCraftQueueIndex >= _craftQueue.Count)
            {
                return;
            }

            var type = _craftQueue[_selectedCraftQueueIndex];
            _craftQueue.RemoveAt(_selectedCraftQueueIndex);
            _craftQueue.Insert(0, type);
            _selectedCraftQueueIndex = 0;
            _hint = $"Перемещено в начало: {type}";
        }

        private void ClampSelectedCraftQueueIndex()
        {
            if (_craftQueue.Count == 0)
            {
                _selectedCraftQueueIndex = -1;
                return;
            }

            if (_selectedCraftQueueIndex < 0)
            {
                return;
            }

            if (_selectedCraftQueueIndex >= _craftQueue.Count)
            {
                _selectedCraftQueueIndex = _craftQueue.Count - 1;
            }
        }

        private void RefundRecipeCost(BuildingType type)
        {
            var costs = GetBuildCost(type);
            for (int i = 0; i < costs.Count; i++)
            {
                _world.AddInventory(costs[i].resource, costs[i].amount);
            }
        }

        private float GetCraftDuration(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Pipe: return 0.28f;
                case BuildingType.PipeCorner: return 0.32f;
                case BuildingType.PipeConnector: return 0.35f;
                case BuildingType.PipeSplitter: return 0.42f;
                case BuildingType.PowerPole: return 0.38f;
                case BuildingType.Storage: return 0.55f;
                case BuildingType.Extractor: return 0.68f;
                case BuildingType.Mixer: return 0.9f;
                case BuildingType.Generator: return 1.05f;
                case BuildingType.MarketTerminal: return 1.2f;
                default: return 0.5f;
            }
        }

        private string GetBuildCostText(BuildingType type)
        {
            var costs = GetBuildCost(type);
            if (costs.Count == 0) return "без затрат";
            var parts = new List<string>();
            for (int i = 0; i < costs.Count; i++)
            {
                parts.Add($"{costs[i].resource}:{costs[i].amount:0}");
            }
            return string.Join(", ", parts);
        }

        private List<(string resource, float amount)> GetBuildCost(BuildingType type)
        {
            var list = new List<(string resource, float amount)>();
            switch (type)
            {
                case BuildingType.Pipe:
                    list.Add(("Terra", 1f));
                    break;
                case BuildingType.PipeCorner:
                    list.Add(("Terra", 1f));
                    list.Add(("Aqua", 1f));
                    break;
                case BuildingType.PipeConnector:
                    list.Add(("Terra", 2f));
                    list.Add(("Ignis", 1f));
                    break;
                case BuildingType.PipeSplitter:
                    list.Add(("Terra", 2f));
                    list.Add(("Aqua", 1f));
                    break;
                case BuildingType.Extractor:
                    list.Add(("Terra", 3f));
                    list.Add(("Ignis", 1f));
                    break;
                case BuildingType.Storage:
                    list.Add(("Terra", 2f));
                    list.Add(("Aqua", 1f));
                    break;
                case BuildingType.Mixer:
                    list.Add(("Terra", 3f));
                    list.Add(("Aqua", 2f));
                    list.Add(("Ignis", 1f));
                    break;
                case BuildingType.Generator:
                    list.Add(("Terra", 4f));
                    list.Add(("Ignis", 2f));
                    break;
                case BuildingType.PowerPole:
                    list.Add(("Terra", 2f));
                    break;
                case BuildingType.MarketTerminal:
                    list.Add(("Terra", 4f));
                    list.Add(("Aqua", 2f));
                    list.Add(("Ignis", 2f));
                    break;
            }
            return list;
        }

        private float GetInventory(string key)
        {
            return _world.GetInventoryAmount(key);
        }

        private static string DirToText(Vector2Int dir)
        {
            if (dir == Vector2Int.up) return "Вверх";
            if (dir == Vector2Int.right) return "Вправо";
            if (dir == Vector2Int.down) return "Вниз";
            return "Влево";
        }

        private void RotateDirectionClockwise()
        {
            if (_direction == Vector2Int.up) _direction = Vector2Int.right;
            else if (_direction == Vector2Int.right) _direction = Vector2Int.down;
            else if (_direction == Vector2Int.down) _direction = Vector2Int.left;
            else _direction = Vector2Int.up;
        }

        private void TryAdvanceGoals()
        {
            if (_goals == null || _currentGoalIndex >= _goals.Length)
            {
                return;
            }

            var goal = _goals[_currentGoalIndex];
            if (_world.GetInventoryAmount(goal.ResourceKey) >= goal.RequiredAmount)
            {
                if (goal.CoinReward > 0)
                {
                    _coins += goal.CoinReward;
                }

                _currentGoalIndex++;
                if (_currentGoalIndex < _goals.Length)
                {
                    var next = _goals[_currentGoalIndex];
                    _hint = goal.CoinReward > 0
                        ? $"Цель выполнена: {goal.Title}. +{goal.CoinReward} зол. Следующая: {next.Title}"
                        : $"Цель выполнена: {goal.Title}. Следующая: {next.Title}";
                }
                else
                {
                    GrantActOneFinaleOnce();
                }
            }

            TryAdvanceGuildOrders();
        }

        private void GrantActOneFinaleOnce()
        {
            if (_actOneFinaleComplete)
            {
                return;
            }

            _actOneFinaleComplete = true;
            const int finaleBonus = 250;
            _coins += finaleBonus;
            _hint =
                $"Акт 1 завершён! Проект-ключ «Кольцо первой цепи» собран. Награда: +{finaleBonus} золота и карт-бланш Гильдии на Восточный карьер (новая зона — в Акте 2). Заказы гильдии можно продолжать.";
        }

        private void SaveGame()
        {
            SaveGame(_activeSlot, GetSaveDisplayName(_activeSlot));
        }

        private void LoadGame()
        {
            LoadGame(_activeSlot);
        }

        private void SaveGame(int slot)
        {
            SaveGame(slot, GetSaveDisplayName(slot));
        }

        private void SaveGame(int slot, string saveName)
        {
            if (_world == null)
            {
                return;
            }

            PlayerPrefs.SetString(KeyForSlot(SaveKeyWorld, slot), _world.SaveToJson());
            PlayerPrefs.SetInt(KeyForSlot(SaveKeyGoal, slot), _currentGoalIndex);
            PlayerPrefs.SetInt(KeyForSlot(SaveKeyGuildOrder, slot), _currentGuildOrderIndex);
            PlayerPrefs.SetInt(KeyForSlot(SaveKeyActOneFinale, slot), _actOneFinaleComplete ? 1 : 0);
            PlayerPrefs.SetInt(KeyForSlot(SaveKeyCoins, slot), _coins);
            PlayerPrefs.SetInt(KeyForSlot(SaveKeyUpgExtractor, slot), _upgExtractor);
            PlayerPrefs.SetInt(KeyForSlot(SaveKeyUpgMixer, slot), _upgMixer);
            PlayerPrefs.SetInt(KeyForSlot(SaveKeyUpgPower, slot), _upgPower);
            PlayerPrefs.SetString(KeyForSlot(SaveKeyCrafted, slot), SerializeCraftedItems());
            PlayerPrefs.SetString(KeyForSlot(SaveKeyCraftQueue, slot), SerializeCraftQueueState());
            PlayerPrefs.SetString(KeyForSlot(SaveKeyHotbar, slot), SerializeHotbarState());
            var player = FindFirstObjectByType<PlayerController2D>();
            if (player != null)
            {
                PlayerPrefs.SetFloat(KeyForSlot(SaveKeyPlayerX, slot), player.transform.position.x);
                PlayerPrefs.SetFloat(KeyForSlot(SaveKeyPlayerY, slot), player.transform.position.y);
            }
            string normalizedName = string.IsNullOrWhiteSpace(saveName) ? $"Слот {slot}" : saveName.Trim();
            PlayerPrefs.SetString(KeyForSlot(SaveKeyName, slot), normalizedName);
            PlayerPrefs.Save();
            _activeSlot = slot;
            _showPauseSaveSlots = false;
            _hint = $"Сохранено в слот {slot}";
        }

        private void LoadGame(int slot)
        {
            if (_world == null)
            {
                return;
            }

            var json = PlayerPrefs.GetString(KeyForSlot(SaveKeyWorld, slot), string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                _world.LoadFromJson(json);
            }

            _currentGoalIndex = Mathf.Clamp(PlayerPrefs.GetInt(KeyForSlot(SaveKeyGoal, slot), 0), 0, _goals.Length);
            _currentGuildOrderIndex = Mathf.Clamp(PlayerPrefs.GetInt(KeyForSlot(SaveKeyGuildOrder, slot), 0), 0, _guildOrders.Length);
            _coins = Mathf.Max(0, PlayerPrefs.GetInt(KeyForSlot(SaveKeyCoins, slot), 0));
            _actOneFinaleComplete = PlayerPrefs.GetInt(KeyForSlot(SaveKeyActOneFinale, slot), 0) == 1;
            bool grantedAct1Migr = false;
            if (_goals != null && _currentGoalIndex >= _goals.Length && !_actOneFinaleComplete)
            {
                GrantActOneFinaleOnce();
                grantedAct1Migr = true;
            }

            _upgExtractor = Mathf.Max(0, PlayerPrefs.GetInt(KeyForSlot(SaveKeyUpgExtractor, slot), 0));
            _upgMixer = Mathf.Max(0, PlayerPrefs.GetInt(KeyForSlot(SaveKeyUpgMixer, slot), 0));
            _upgPower = Mathf.Max(0, PlayerPrefs.GetInt(KeyForSlot(SaveKeyUpgPower, slot), 0));
            DeserializeCraftedItems(PlayerPrefs.GetString(KeyForSlot(SaveKeyCrafted, slot), string.Empty));
            DeserializeCraftQueueState(PlayerPrefs.GetString(KeyForSlot(SaveKeyCraftQueue, slot), string.Empty));
            DeserializeHotbarState(PlayerPrefs.GetString(KeyForSlot(SaveKeyHotbar, slot), string.Empty));
            var player = FindFirstObjectByType<PlayerController2D>();
            if (player != null)
            {
                float px = PlayerPrefs.GetFloat(KeyForSlot(SaveKeyPlayerX, slot), player.transform.position.x);
                float py = PlayerPrefs.GetFloat(KeyForSlot(SaveKeyPlayerY, slot), player.transform.position.y);
                player.transform.position = new Vector3(px, py, player.transform.position.z);
            }
            _showInventoryWindow = false;
            _showBuildingWindow = false;
            _draggingHotbarType = null;
            _inventoryScroll = 0f;
            _storageWindowScroll = Vector2.zero;
            _showPauseSaveSlots = false;
            _selected = BuildingType.None;
            _tutorialStep = 0;
            _tutorialElapsed = 0f;
            _tutorialVisible = false;
            SyncUpgradeLevelsFromWorld();
            _hint = grantedAct1Migr ? $"{_hint} (загрузка слота {slot})" : $"Загрузка из слота {slot}";
        }

        private bool HasSave(int slot)
        {
            return PlayerPrefs.HasKey(KeyForSlot(SaveKeyWorld, slot));
        }

        private string GetSaveDisplayName(int slot)
        {
            if (!HasSave(slot))
            {
                return $"Слот {slot} (пусто)";
            }

            string saved = PlayerPrefs.GetString(KeyForSlot(SaveKeyName, slot), $"Слот {slot}");
            return string.IsNullOrWhiteSpace(saved) ? $"Слот {slot}" : saved;
        }

        private string GetSlotShortLabel(int slot)
        {
            string name = GetSaveDisplayName(slot);
            if (name.Length > 12)
            {
                name = name.Substring(0, 12) + "...";
            }
            return name;
        }

        private static string KeyForSlot(string key, int slot)
        {
            return $"{key}_SLOT_{slot}";
        }

        private string SerializeCraftedItems()
        {
            var parts = new List<string>();
            foreach (var kv in _craftedItems)
            {
                if (kv.Value > 0)
                {
                    parts.Add($"{(int)kv.Key}:{kv.Value}");
                }
            }
            return string.Join(";", parts);
        }

        private void DeserializeCraftedItems(string data)
        {
            _craftedItems.Clear();
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            var entries = data.Split(';');
            for (int i = 0; i < entries.Length; i++)
            {
                var pair = entries[i].Split(':');
                if (pair.Length != 2) continue;
                if (!int.TryParse(pair[0], out int typeInt)) continue;
                if (!int.TryParse(pair[1], out int count)) continue;
                var type = (BuildingType)typeInt;
                if (count > 0) _craftedItems[type] = count;
            }
        }

        private string SerializeHotbarState()
        {
            var parts = new List<string>();
            for (int i = 0; i < HotbarSize; i++)
            {
                parts.Add(_hotbarSlots[i].HasValue ? ((int)_hotbarSlots[i].Value).ToString() : "-1");
            }
            return string.Join(",", parts);
        }

        private void DeserializeHotbarState(string data)
        {
            InitializeHotbarDefaults();
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            var parts = data.Split(',');
            int n = Mathf.Min(parts.Length, HotbarSize);
            for (int i = 0; i < n; i++)
            {
                if (!int.TryParse(parts[i], out int typeInt)) continue;
                if (typeInt < 0) continue;
                if (!Enum.IsDefined(typeof(BuildingType), typeInt)) continue;
                var type = (BuildingType)typeInt;
                if (type == BuildingType.None) continue;
                _hotbarSlots[i] = type;
            }
        }

        private string SerializeCraftQueueState()
        {
            string activeType = _activeCraftType.HasValue ? ((int)_activeCraftType.Value).ToString() : "-1";
            string progress = _activeCraftProgress.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            string paused = _isCraftQueuePaused ? "1" : "0";
            var queued = new List<string>();
            for (int i = 0; i < _craftQueue.Count; i++)
            {
                queued.Add(((int)_craftQueue[i]).ToString());
            }

            return $"{paused}|{activeType}|{progress}|{string.Join(",", queued)}";
        }

        private void DeserializeCraftQueueState(string data)
        {
            _craftQueue.Clear();
            _activeCraftType = null;
            _activeCraftProgress = 0f;
            _isCraftQueuePaused = false;
            _selectedCraftQueueIndex = -1;

            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            var parts = data.Split('|');
            if (parts.Length < 4)
            {
                return;
            }

            _isCraftQueuePaused = parts[0] == "1";

            if (int.TryParse(parts[1], out int activeTypeInt) && Enum.IsDefined(typeof(BuildingType), activeTypeInt))
            {
                if (activeTypeInt >= 0)
                {
                    _activeCraftType = (BuildingType)activeTypeInt;
                }
            }

            if (float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float progress))
            {
                _activeCraftProgress = Mathf.Max(0f, progress);
            }

            if (!string.IsNullOrEmpty(parts[3]))
            {
                var items = parts[3].Split(',');
                for (int i = 0; i < items.Length; i++)
                {
                    if (!int.TryParse(items[i], out int t)) continue;
                    if (!Enum.IsDefined(typeof(BuildingType), t)) continue;
                    if ((BuildingType)t == BuildingType.None) continue;
                    _craftQueue.Add((BuildingType)t);
                }
            }
        }

        private void StartNewGame()
        {
            _world.ResetRuntimeState();
            var player = FindFirstObjectByType<PlayerController2D>();
            if (player != null)
            {
                var safeSpawn = _world.FindNearestFreeCell(Vector2Int.zero, 42);
                player.transform.position = WorldGridSystem.CellToWorld(safeSpawn, 0f);
            }
            _currentGoalIndex = 0;
            _currentGuildOrderIndex = 0;
            _actOneFinaleComplete = false;
            _coins = 0;
            _upgExtractor = 0;
            _upgMixer = 0;
            _upgPower = 0;
            _craftedItems.Clear();
            _craftQueue.Clear();
            _activeCraftType = null;
            _activeCraftProgress = 0f;
            _isCraftQueuePaused = false;
            _selectedCraftQueueIndex = -1;
            _showInventoryWindow = false;
            _showCraftWindow = false;
            _showBuildingWindow = false;
            _draggingHotbarType = null;
            _inventoryScroll = 0f;
            _storageWindowScroll = Vector2.zero;
            InitializeHotbarDefaults();
            _selected = BuildingType.None;
            _tutorialStep = 0;
            _tutorialElapsed = 0f;
            _tutorialVisible = true;
            _direction = Vector2Int.right;
            _screen = UiScreen.InGame;
            _hint = "Новая игра начата";
        }

        private void SyncUpgradeLevelsFromWorld()
        {
            _upgExtractor = Mathf.Max(_upgExtractor, Mathf.RoundToInt((_world.ExtractRateMultiplier - 1f) / 0.5f));
            _upgMixer = Mathf.Max(_upgMixer, Mathf.RoundToInt((_world.MixerOutputMultiplier - 1f) / 0.25f));
            _upgPower = Mathf.Max(_upgPower, _world.PowerReach - 4);
        }

        private bool CanUseMarket()
        {
            return _world.GetBuildingCount(BuildingType.MarketTerminal) > 0;
        }

        private void TryBuyUpgradeExtractor()
        {
            if (!CanUseMarket())
            {
                _hint = "Построй рынок (7), чтобы покупать апгрейды";
                return;
            }

            if (_upgExtractor >= 6)
            {
                _hint = "Апгрейд добычи уже на максимуме";
                return;
            }

            var cost = GetExtractorUpgradeCost();
            if (!TryConsumeUpgradeResources(cost))
            {
                _hint = $"Недостаточно ресурсов: {FormatUpgradeCost(cost)}";
                return;
            }

            _upgExtractor++;
            _world.ApplyExtractorUpgrade(0.5f);
            _hint = "Куплен апгрейд добычи (+50%)";
        }

        private void TryBuyUpgradeMixer()
        {
            if (!CanUseMarket())
            {
                _hint = "Построй рынок (7), чтобы покупать апгрейды";
                return;
            }

            if (_upgMixer >= 8)
            {
                _hint = "Апгрейд смесителя уже на максимуме";
                return;
            }

            var cost = GetMixerUpgradeCost();
            if (!TryConsumeUpgradeResources(cost))
            {
                _hint = $"Недостаточно ресурсов: {FormatUpgradeCost(cost)}";
                return;
            }

            _upgMixer++;
            _world.ApplyMixerUpgrade(0.25f);
            _hint = "Куплен апгрейд смесителя (+25%)";
        }

        private void TryBuyUpgradePower()
        {
            if (!CanUseMarket())
            {
                _hint = "Построй рынок (7), чтобы покупать апгрейды";
                return;
            }

            if (_upgPower >= 4)
            {
                _hint = "Апгрейд энергосети уже на максимуме";
                return;
            }

            var cost = GetPowerUpgradeCost();
            if (!TryConsumeUpgradeResources(cost))
            {
                _hint = $"Недостаточно ресурсов: {FormatUpgradeCost(cost)}";
                return;
            }

            _upgPower++;
            _world.ApplyPowerReachUpgrade(1);
            _hint = "Куплен апгрейд энергосети (+1 дальность)";
        }

        private ResourceCost[] GetExtractorUpgradeCost()
        {
            return new[]
            {
                new ResourceCost("Aqua", 8 + _upgExtractor * 4),
                new ResourceCost("Terra", 4 + _upgExtractor * 2)
            };
        }

        private ResourceCost[] GetMixerUpgradeCost()
        {
            return new[]
            {
                new ResourceCost("Ignis", 8 + _upgMixer * 4),
                new ResourceCost("Aeris", 4 + _upgMixer * 2)
            };
        }

        private ResourceCost[] GetPowerUpgradeCost()
        {
            return new[]
            {
                new ResourceCost("Terra", 10 + _upgPower * 5),
                new ResourceCost("Aeris", 3 + _upgPower)
            };
        }

        private bool HasUpgradeResources(ResourceCost[] costs)
        {
            if (costs == null)
            {
                return false;
            }

            for (int i = 0; i < costs.Length; i++)
            {
                var c = costs[i];
                if (_world.GetInventoryAmount(c.Key) + 0.0001f < c.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryConsumeUpgradeResources(ResourceCost[] costs)
        {
            if (!HasUpgradeResources(costs))
            {
                return false;
            }

            for (int i = 0; i < costs.Length; i++)
            {
                var c = costs[i];
                _world.TryConsumeInventory(c.Key, c.Amount);
            }

            return true;
        }

        private static string FormatUpgradeCost(ResourceCost[] costs)
        {
            if (costs == null || costs.Length == 0)
            {
                return "-";
            }

            string text = string.Empty;
            for (int i = 0; i < costs.Length; i++)
            {
                string part = $"{costs[i].Key} {costs[i].Amount:0}";
                text = string.IsNullOrEmpty(text) ? part : $"{text}, {part}";
            }

            return text;
        }

        private string UpgradeText()
        {
            return $"Апгрейды: Добыча Lv{_upgExtractor} (x{_world.ExtractRateMultiplier:0.##}) | " +
                   $"Смеситель Lv{_upgMixer} (x{_world.MixerOutputMultiplier:0.##}) | " +
                   $"Энергосеть Lv{_upgPower} (радиус {_world.PowerReach})";
        }

        private void TryAdvanceGuildOrders()
        {
            if (_guildOrders == null || _currentGuildOrderIndex >= _guildOrders.Length)
            {
                return;
            }

            var order = _guildOrders[_currentGuildOrderIndex];
            if (!IsGuildOrderCompleted(order))
            {
                return;
            }

            _coins += order.RewardCoins;
            _currentGuildOrderIndex++;
            _hint = _currentGuildOrderIndex < _guildOrders.Length
                ? $"Гильдия: выполнено '{order.Title}', +{order.RewardCoins} монет"
                : $"Все заказы гильдии выполнены, +{order.RewardCoins} монет";
        }

        private bool IsGuildOrderCompleted(GuildOrder order)
        {
            if (order.ResourceKey == "PowerSetup")
            {
                return _world.GetBuildingCount(BuildingType.Generator) >= 1 && _world.GetBuildingCount(BuildingType.PowerPole) >= 1;
            }

            return _world.GetInventoryAmount(order.ResourceKey) >= order.RequiredAmount;
        }

        private string GuildOrderText()
        {
            if (_guildOrders == null || _currentGuildOrderIndex >= _guildOrders.Length)
            {
                return "все заказы выполнены";
            }

            var order = _guildOrders[_currentGuildOrderIndex];
            if (order.ResourceKey == "PowerSetup")
            {
                int g = _world.GetBuildingCount(BuildingType.Generator);
                int p = _world.GetBuildingCount(BuildingType.PowerPole);
                return $"{order.Title} (Generator {g}/1, Pole {p}/1, награда {order.RewardCoins})";
            }

            float current = _world.GetInventoryAmount(order.ResourceKey);
            return $"{order.Title} ({order.ResourceKey} {current:0.##}/{order.RequiredAmount:0.##}, награда {order.RewardCoins})";
        }

        private string GoalText()
        {
            if (_goals == null || _currentGoalIndex >= _goals.Length)
            {
                if (_actOneFinaleComplete)
                {
                    return "Акт 1 «Ученик» завершён. Артефакт: «Кольцо первой цепи». Открыт маршрут на Восточный карьер (Акт 2).";
                }

                return "Текущая цель: кампания завершена";
            }

            var goal = _goals[_currentGoalIndex];
            var current = _world.GetInventoryAmount(goal.ResourceKey);
            return $"Текущая цель: {goal.Title} ({goal.ResourceKey} {current:0.##}/{goal.RequiredAmount:0.##}, за цель +{goal.CoinReward} зол.)";
        }

        private static Vector2Int MouseToCell()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return Vector2Int.zero;
            }

            Vector2 mouse;
#if ENABLE_INPUT_SYSTEM
            mouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            mouse = Input.mousePosition;
#else
            mouse = Vector2.zero;
#endif

            var world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 10f));
            return WorldGridSystem.WorldToCell(world);
        }

        private static Vector3 MouseToWorld()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return Vector3.zero;
            }

            Vector2 mouse;
#if ENABLE_INPUT_SYSTEM
            mouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            mouse = Input.mousePosition;
#else
            mouse = Vector2.zero;
#endif

            return cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 10f));
        }

        private static bool PressedLeftMouse()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private static bool PressedRightMouse()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(1);
#else
            return false;
#endif
        }

        private static Vector2 MouseGuiPosition()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            var mouse = (Vector2)Input.mousePosition;
#else
            var mouse = Vector2.zero;
#endif
            float invScale = 1f / Mathf.Max(0.01f, GameSettings.UiScale);
            return new Vector2(mouse.x * invScale, (Screen.height - mouse.y) * invScale);
        }

        private bool IsMouseOverGameplayUi()
        {
            Vector2 mp = MouseGuiPosition();
            float invScale = 1f / Mathf.Max(0.01f, GameSettings.UiScale);
            float sw = Screen.width * invScale;
            float sh = Screen.height * invScale;

            if (_showCraftWindow)
            {
                var craftPanel = new Rect((sw - 560f) * 0.5f, (sh - 390f) * 0.5f, 560f, 390f);
                return craftPanel.Contains(mp);
            }

            if (_showInventoryWindow)
            {
                var inventoryPanel = new Rect((sw - 520f) * 0.5f, (sh - 360f) * 0.5f, 520f, 360f);
                return inventoryPanel.Contains(mp);
            }

            if (_isPauseMenuOpen)
            {
                var pausePanel = new Rect((sw - 420f) * 0.5f, (sh - 340f) * 0.5f, 420f, 340f);
                return pausePanel.Contains(mp);
            }

            if (_showSettings)
            {
                var settingsPanel = new Rect((sw - 420f) * 0.5f, (sh - 272f) * 0.5f, 420f, 272f);
                return settingsPanel.Contains(mp);
            }

            if (_tutorialVisible && !_isPauseMenuOpen && !_showSettings)
            {
                var tutorialPanel = new Rect(16f, 16f, Mathf.Min(560f, sw - 32f), 56f);
                if (tutorialPanel.Contains(mp))
                {
                    return true;
                }
            }

            var hud = new Rect(16f, sh - 176f, sw - 32f, 160f);
            return hud.Contains(mp);
        }

        private static bool PressedDigit1()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha1);
#else
            return false;
#endif
        }

        private static bool PressedDigit2()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha2);
#else
            return false;
#endif
        }

        private static bool PressedDigit3()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha3);
#else
            return false;
#endif
        }

        private static bool PressedDigit4()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha4);
#else
            return false;
#endif
        }

        private static bool PressedDigit5()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit5Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha5);
#else
            return false;
#endif
        }

        private static bool PressedDigit6()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit6Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha6);
#else
            return false;
#endif
        }

        private static bool PressedDigit7()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit7Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha7);
#else
            return false;
#endif
        }

        private static bool PressedDigit8()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit8Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha8);
#else
            return false;
#endif
        }

        private static bool PressedDigit0()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit0Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha0);
#else
            return false;
#endif
        }

        private static bool PressedRotate()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }

        private static bool PressedSave()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F5);
#else
            return false;
#endif
        }

        private static bool PressedLoad()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F9);
#else
            return false;
#endif
        }

        private static bool PressedToggleInventory()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Tab);
#else
            return false;
#endif
        }

        private static bool PressedToggleCraftWindow()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.K);
#else
            return false;
#endif
        }

        private static bool PressedHandGather()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }

        private static bool PressedEscape()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        private static bool PressedUpgradeExtractor()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.U);
#else
            return false;
#endif
        }

        private static bool PressedUpgradeMixer()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.I);
#else
            return false;
#endif
        }

        private static bool PressedUpgradePower()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.O);
#else
            return false;
#endif
        }

    }
}
