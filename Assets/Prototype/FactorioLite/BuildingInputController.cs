using UnityEngine;
using System.Collections.Generic;
using System;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CircuitFlowAlchemy.Prototype.FactorioLite
{
    public class BuildingInputController : MonoBehaviour
    {
        private const float HandGatherRadiusCells = 2.2f;
        private enum HudTab
        {
            Build,
            Orders,
            Upgrades
        }

        private enum InventorySortMode
        {
            NameAsc,
            CountDesc,
            Type
        }

        private enum UiScreen
        {
            MainMenu,
            SaveMenu,
            InGame
        }

        private const string SaveKeyWorld = "CFA_FACTORIO_LITE_WORLD_V1";
        private const string SaveKeyGoal = "CFA_FACTORIO_LITE_GOAL_V1";
        private const string SaveKeyGuildOrder = "CFA_FACTORIO_LITE_GUILD_ORDER_V1";
        private const string SaveKeyCoins = "CFA_FACTORIO_LITE_COINS_V1";
        private const string SaveKeyUpgExtractor = "CFA_FACTORIO_LITE_UPG_EXTRACTOR_V1";
        private const string SaveKeyUpgMixer = "CFA_FACTORIO_LITE_UPG_MIXER_V1";
        private const string SaveKeyUpgPower = "CFA_FACTORIO_LITE_UPG_POWER_V1";
        private const string SaveKeyCrafted = "CFA_FACTORIO_LITE_CRAFTED_V1";
        private const string SaveKeyCraftQueue = "CFA_FACTORIO_LITE_CRAFT_QUEUE_V1";
        private const string SaveKeyHotbar = "CFA_FACTORIO_LITE_HOTBAR_V1";
        private const string SaveKeyName = "CFA_FACTORIO_LITE_SAVE_NAME_V1";
        private const int HotbarSize = 8;

        private WorldGridSystem _world;
        private UiScreen _screen = UiScreen.InGame;
        private BuildingType _selected = BuildingType.None;
        private Vector2Int _direction = Vector2Int.right;
        private string _hint;
        private int _currentGoalIndex;
        private Goal[] _goals;
        private GuildOrder[] _guildOrders;
        private int _currentGuildOrderIndex;
        private int _coins;
        private int _upgExtractor;
        private int _upgMixer;
        private int _upgPower;
        private GameObject _preview;
        private SpriteRenderer _previewRenderer;
        private SpriteRenderer _previewArrowRenderer;
        private bool _showSettings;
        private bool _isPauseMenuOpen;
        private bool _showPauseLoadSlots;
        private bool _showPauseSaveSlots;
        private string _pendingSaveName = string.Empty;
        private int _activeSlot = 1;
        private HudTab _activeTab = HudTab.Build;
        private float _pauseAnim;
        private float _settingsAnim;
        private string _hoverTooltip;
        private readonly Dictionary<BuildingType, Texture2D> _buildIcons = new Dictionary<BuildingType, Texture2D>();
        private readonly Dictionary<string, Texture2D> _resourceIcons = new Dictionary<string, Texture2D>();
        private readonly Dictionary<BuildingType, int> _craftedItems = new Dictionary<BuildingType, int>();
        private readonly List<BuildingType> _craftQueue = new List<BuildingType>();
        private readonly BuildingType?[] _hotbarSlots = new BuildingType?[HotbarSize];
        private readonly Rect[] _hotbarSlotRects = new Rect[HotbarSize];
        private GUIStyle _slotLabelStyle;
        private AudioSource _uiAudioSource;
        private AudioClip _uiHoverClip;
        private AudioClip _uiClickClip;
        private string _lastHoveredControlId;
        private bool _showCraftWindow;
        private bool _showInventoryWindow;
        private BuildingType? _activeCraftType;
        private float _activeCraftProgress;
        private bool _isCraftQueuePaused;
        private int _selectedCraftQueueIndex = -1;
        private BuildingType? _draggingHotbarType;
        private InventorySortMode _inventorySort = InventorySortMode.Type;
        private float _inventoryScroll;
        private int _tutorialStep;
        private float _tutorialElapsed;
        private bool _tutorialVisible = true;

        private void Start()
        {
            _world = FindFirstObjectByType<WorldGridSystem>();
            _hint = "1-Труба | 2-Экстрактор | 3-Хранилище | 4-Смеситель | R-поворот";
            InitializeHotbarDefaults();
            _goals = new[]
            {
                new Goal("1-1 Собрать воду", "Aqua", 6f),
                new Goal("1-2 Собрать огонь", "Ignis", 6f),
                new Goal("1-3 Получить пар", "Aeris", 5f),
                new Goal("1-4 Запустить базовую фабрику", "Terra", 5f)
            };
            _guildOrders = new[]
            {
                new GuildOrder("Заказ Гильдии: Первые поставки", "Aqua", 8f, 45),
                new GuildOrder("Заказ Гильдии: Пламя для кузни", "Ignis", 8f, 60),
                new GuildOrder("Заказ Гильдии: Паровая линия", "Aeris", 7f, 90),
                new GuildOrder("Заказ Гильдии: Электрификация", "PowerSetup", 1f, 130)
            };

            CreatePlacementPreview();
            _screen = UiScreen.InGame;
            GameUiState.IsGameplayActive = true;

            if (SceneFlow.PendingNewGame)
            {
                SceneFlow.PendingNewGame = false;
                StartNewGame();
            }
            else if (SceneFlow.PendingLoadSlot > 0)
            {
                _activeSlot = SceneFlow.PendingLoadSlot;
                SceneFlow.PendingLoadSlot = -1;
                LoadGame(_activeSlot);
            }
            else
            {
                LoadGame();
            }

            _slotLabelStyle = new GUIStyle(UiTheme.Label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };

            _uiAudioSource = gameObject.AddComponent<AudioSource>();
            _uiAudioSource.playOnAwake = false;
            _uiAudioSource.spatialBlend = 0f;
            _uiAudioSource.volume = 0.4f;
            _uiHoverClip = CreateToneClip(890f, 0.04f, 0.12f);
            _uiClickClip = CreateToneClip(650f, 0.06f, 0.18f);
        }

        private void Update()
        {
            if (_world == null)
            {
                return;
            }

            ApplyUiModeState();
            _pauseAnim = Mathf.MoveTowards(_pauseAnim, _isPauseMenuOpen ? 1f : 0f, Time.unscaledDeltaTime * 7f);
            _settingsAnim = Mathf.MoveTowards(_settingsAnim, _showSettings ? 1f : 0f, Time.unscaledDeltaTime * 7f);
            UpdateCraftingQueue(Time.deltaTime);
            UpdateOnboarding(Time.deltaTime);

            if (_screen != UiScreen.InGame)
            {
                if (_preview != null) _preview.SetActive(false);
                return;
            }

            if (PressedEscape())
            {
                _isPauseMenuOpen = !_isPauseMenuOpen;
                _showPauseLoadSlots = false;
                _showPauseSaveSlots = false;
                if (_isPauseMenuOpen)
                {
                    _showSettings = false;
                }
            }

            if (PressedToggleInventory())
            {
                _showInventoryWindow = !_showInventoryWindow;
                if (_showInventoryWindow)
                {
                    _showCraftWindow = false;
                    _isPauseMenuOpen = false;
                    _showSettings = false;
                }
            }

            if (PressedToggleCraftWindow() && !_showInventoryWindow)
            {
                _showCraftWindow = !_showCraftWindow;
            }

            if (_isPauseMenuOpen || _showSettings || _showCraftWindow || _showInventoryWindow)
            {
                if (_preview != null) _preview.SetActive(false);
                return;
            }

            if (_preview != null && !_preview.activeSelf) _preview.SetActive(true);

            HandleHotbarDigitSelection();
            if (PressedRotate()) RotateDirectionClockwise();
            if (PressedSave()) SaveGame();
            if (PressedLoad()) LoadGame();
            if (PressedUpgradeExtractor()) TryBuyUpgradeExtractor();
            if (PressedUpgradeMixer()) TryBuyUpgradeMixer();
            if (PressedUpgradePower()) TryBuyUpgradePower();

            Vector2Int cell = MouseToCell();
            if (!_world.IsInside(cell))
            {
                UpdatePreview(cell, false);
                return;
            }

            bool canPlace = CanPlaceSelected(cell);
            UpdatePreview(cell, canPlace);

            if (IsMouseOverGameplayUi())
            {
                return;
            }

            if (PressedHandGather())
            {
                var player = FindFirstObjectByType<PlayerController2D>();
                Vector2Int playerCell = player != null
                    ? new Vector2Int(Mathf.RoundToInt(player.transform.position.x), Mathf.RoundToInt(player.transform.position.y))
                    : Vector2Int.zero;

                float dist = Vector2Int.Distance(playerCell, cell);
                if (dist > HandGatherRadiusCells)
                {
                    _hint = $"Слишком далеко для ручного сбора (радиус {HandGatherRadiusCells:0.0})";
                }
                else
                {
                    bool gathered = _world.TryGatherByHand(cell, 1f);
                    _hint = gathered ? "Собрано вручную +1" : "Здесь нельзя собрать ресурс рукой";
                }
            }

            if (PressedLeftMouse())
            {
                if (_selected == BuildingType.None)
                {
                    _hint = "Свободная рука: выбери объект для строительства";
                    return;
                }

                if (!TryConsumeCraftedItem(_selected))
                {
                    _hint = $"Нет готового предмета {_selected}. Открой крафт (K).";
                    return;
                }

                bool ok = _world.TryPlace(_selected, cell, _direction);
                if (!ok && _selected == BuildingType.Extractor)
                {
                    AddCraftedItem(_selected, 1);
                    _hint = "Экстрактор ставится только на узел ресурса";
                }
                else if (!ok)
                {
                    AddCraftedItem(_selected, 1);
                    _hint = "Нельзя поставить объект в эту клетку";
                }
            }

            if (PressedRightMouse())
            {
                if (_world.TryRemove(cell, out var removedType) && removedType != BuildingType.None)
                {
                    AddCraftedItem(removedType, 1);
                    _hint = $"Возвращено в карман: {removedType}";
                }
            }

            TryAdvanceGoals();
        }

        private void OnGUI()
        {
            if (_world == null)
            {
                return;
            }

            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * GameSettings.UiScale);
            float invScale = 1f / GameSettings.UiScale;
            var sw = Screen.width * invScale;
            var sh = Screen.height * invScale;
            _hoverTooltip = string.Empty;

            if (!_showCraftWindow)
            {
                DrawBottomHud(sw, sh);

                if (!string.IsNullOrEmpty(_hoverTooltip))
                {
                    DrawTooltip(sw, sh, _hoverTooltip);
                }
            }

            if (_pauseAnim > 0.001f)
            {
                DrawPauseMenuOverlay(sw, sh, !_showSettings);
            }

            if (_settingsAnim > 0.001f)
            {
                DrawSettingsPanel(sw, sh);
            }

            if (_showCraftWindow)
            {
                DrawCraftWindow(sw, sh);
            }

            if (_showInventoryWindow)
            {
                DrawInventoryModal(sw, sh);
            }

            if (_tutorialVisible && !_isPauseMenuOpen && !_showSettings)
            {
                DrawOnboardingOverlay(sw, sh);
            }
        }

        private void ApplyUiModeState()
        {
            bool gameplay = _screen == UiScreen.InGame && !_isPauseMenuOpen && !_showSettings && !_showInventoryWindow;
            GameUiState.IsGameplayActive = gameplay;
            _world.SetWorldVisible(true);

            var player = FindFirstObjectByType<PlayerController2D>();
            if (player != null)
            {
                var sr = player.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = true;
            }
        }

        private void DrawBottomHud(float sw, float sh)
        {
            var hud = new Rect(16, sh - 176, sw - 32, 160);
            GUI.Box(hud, GUIContent.none, UiTheme.Panel);

            var tabY = hud.y + 8;
            if (StyledButton(new Rect(hud.x + 10, tabY, 110, 28), "Build", "tab_build", _activeTab == HudTab.Build ? UiTheme.Button : UiTheme.TabButton)) _activeTab = HudTab.Build;
            if (StyledButton(new Rect(hud.x + 126, tabY, 110, 28), "Orders", "tab_orders", _activeTab == HudTab.Orders ? UiTheme.Button : UiTheme.TabButton)) _activeTab = HudTab.Orders;
            if (StyledButton(new Rect(hud.x + 242, tabY, 110, 28), "Upgrades", "tab_upgrades", _activeTab == HudTab.Upgrades ? UiTheme.Button : UiTheme.TabButton)) _activeTab = HudTab.Upgrades;
            switch (_activeTab)
            {
                case HudTab.Build:
                    DrawBuildTab(hud);
                    break;
                case HudTab.Orders:
                    DrawOrdersTab(hud);
                    break;
                case HudTab.Upgrades:
                    DrawUpgradesTab(hud);
                    break;
            }
        }

        private void DrawBuildTab(Rect hud)
        {
            GUI.Label(new Rect(hud.x + 12, hud.y + 42, hud.width - 24, 20), $"Выбрано: {_selected} | Направление: {DirToText(_direction)}", UiTheme.Label);
            float startX = hud.x + 12;
            float y = hud.y + 66;
            float slotW = 100f;
            float gap = 10f;
            for (int i = 0; i < HotbarSize; i++)
            {
                DrawHotbarSlot(i, startX + (slotW + gap) * i, y);
            }
            FinalizeHotbarDragIfReleased();

            if (StyledButton(new Rect(hud.x + hud.width - 132, y + 42, 120, 34), "Craft (K)", "open_craft_window", UiTheme.Button))
            {
                _showCraftWindow = !_showCraftWindow;
            }
            DrawHudCraftQueueControls(hud);
            if (GameSettings.ShowHints)
            {
                GUI.Label(new Rect(hud.x + 12, hud.y + 136, hud.width - 24, 20), $"Подсказка: {_hint}", UiTheme.Label);
            }
        }

        private void DrawHotbarSlot(int slotIndex, float x, float y)
        {
            var type = _hotbarSlots[slotIndex];
            string key = (slotIndex + 1).ToString();
            string name = type.HasValue ? type.Value.ToString() : "Пусто";
            bool selected = _selected == type;
            var rect = new Rect(x, y, 100, 60);
            _hotbarSlotRects[slotIndex] = rect;
            HandleHotbarDrop(slotIndex, rect);

            bool hovered = rect.Contains(Event.current.mousePosition);
            var prev = GUI.color;
            GUI.color = selected
                ? new Color(0.27f, 0.52f, 0.92f, 0.95f)
                : (hovered ? new Color(0.2f, 0.28f, 0.38f, 0.95f) : new Color(0.12f, 0.15f, 0.2f, 0.9f));
            GUI.Box(rect, GUIContent.none, UiTheme.Panel);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x + 8, y + 8, 20, 20), type.HasValue ? GetBuildIcon(type.Value) : GetResourceIcon("Empty"));
            GUI.color = prev;
            GUI.Label(new Rect(x + 32, y + 6, 62, 18), $"[{key}]", _slotLabelStyle);
            GUI.Label(new Rect(x + 30, y + 24, 66, 26), name, _slotLabelStyle);
            if (type.HasValue)
            {
                GUI.Label(new Rect(x + 4, y + 42, 92, 14), $"x{GetCraftedCount(type.Value)}", _slotLabelStyle);
            }
            if (hovered)
            {
                string hoverId = $"build_slot_{slotIndex}";
                if (_lastHoveredControlId != hoverId)
                {
                    _lastHoveredControlId = hoverId;
                    PlayUiHover();
                }
                _hoverTooltip = type.HasValue
                    ? $"{type.Value}: в кармане {GetCraftedCount(type.Value)} | крафт: {GetBuildCostText(type.Value)}"
                    : "Пустой слот. Перетащи сюда сооружение из инвентаря (Tab).";
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                if (!type.HasValue)
                {
                    _selected = BuildingType.None;
                    _hint = "Пустой слот";
                }
                else if (IsBuildingUnlocked(type.Value))
                {
                    _selected = type.Value;
                    PlayUiClick();
                }
                else
                {
                    _hint = $"Объект {type.Value} пока не разблокирован";
                }
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
            {
                _hotbarSlots[slotIndex] = null;
                if (_selected == type) _selected = BuildingType.None;
                _hint = $"Слот {slotIndex + 1} очищен";
                Event.current.Use();
            }
        }

        private void DrawOrdersTab(Rect hud)
        {
            GUI.Label(new Rect(hud.x + 12, hud.y + 48, hud.width - 24, 20), GoalText(), UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 74, hud.width - 24, 20), $"Гильдия: {GuildOrderText()}", UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 100, hud.width - 24, 20), $"Прогресс кампании: {_currentGoalIndex}/{_goals.Length}", UiTheme.Label);
        }

        private void DrawUpgradesTab(Rect hud)
        {
            GUI.Label(new Rect(hud.x + 12, hud.y + 48, hud.width - 24, 20), UpgradeText(), UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 74, hud.width - 24, 20), "U: добыча (+50%) | I: смеситель (+25%) | O: энергосеть (+1)", UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 100, hud.width - 24, 20), "Для покупки апгрейдов нужен Market (7).", UiTheme.Label);
        }

        private void DrawHudCraftQueueControls(Rect hud)
        {
            var panel = new Rect(hud.x + hud.width - 316, hud.y + 8, 304, 42);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(panel.x + 8, panel.y + 2, 176, 16), "Очередь крафта", UiTheme.Label);

            ClampSelectedCraftQueueIndex();
            int maxShown = Mathf.Min(6, _craftQueue.Count);
            for (int i = 0; i < maxShown; i++)
            {
                float x = panel.x + 8 + i * 22;
                var iconRect = new Rect(x, panel.y + 20, 18, 18);
                if (_selectedCraftQueueIndex == i)
                {
                    var prev = GUI.color;
                    GUI.color = new Color(0.25f, 0.75f, 1f, 0.85f);
                    GUI.DrawTexture(new Rect(x - 1, panel.y + 19, 20, 20), Texture2D.whiteTexture);
                    GUI.color = prev;
                }
                GUI.DrawTexture(iconRect, GetBuildIcon(_craftQueue[i]));
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && iconRect.Contains(Event.current.mousePosition))
                {
                    _selectedCraftQueueIndex = i;
                    Event.current.Use();
                }
            }

            if (_craftQueue.Count > maxShown)
            {
                GUI.Label(new Rect(panel.x + 8 + maxShown * 22, panel.y + 20, 30, 18), $"+{_craftQueue.Count - maxShown}", UiTheme.Label);
            }

            bool canReorder = _selectedCraftQueueIndex >= 0 && _selectedCraftQueueIndex < _craftQueue.Count && _craftQueue.Count > 1;
            GUI.enabled = canReorder;
            if (StyledButton(new Rect(panel.x + 186, panel.y + 17, 28, 22), "↑", "hud_q_up", UiTheme.Button)) MoveSelectedCraftQueueItem(-1);
            if (StyledButton(new Rect(panel.x + 218, panel.y + 17, 28, 22), "↓", "hud_q_down", UiTheme.Button)) MoveSelectedCraftQueueItem(1);
            if (StyledButton(new Rect(panel.x + 250, panel.y + 17, 48, 22), "Top", "hud_q_top", UiTheme.Button)) MoveSelectedCraftQueueToFront();
            GUI.enabled = true;
        }

        private void DrawInventoryModal(float sw, float sh)
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.58f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = prev;

            const float w = 520f;
            const float h = 360f;
            var panel = new Rect((sw - w) * 0.5f, (sh - h) * 0.5f, w, h);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(panel.x + 14, panel.y + 10, panel.width - 28, 24), "Инвентарь (Tab)", UiTheme.Title);
            DrawInventorySortControls(panel);
            DrawInventoryList(panel);
            DrawDraggedHotbarGhost();

            if (StyledButton(new Rect(panel.x + panel.width - 132, panel.y + panel.height - 36, 120, 26), "Закрыть", "inv_close", UiTheme.Button))
            {
                _showInventoryWindow = false;
            }
        }

        private void DrawInventorySortControls(Rect panel)
        {
            GUI.Label(new Rect(panel.x + 14, panel.y + 40, 90, 20), "Сортировка:", UiTheme.Label);
            if (StyledButton(new Rect(panel.x + 98, panel.y + 40, 90, 22), "Название", "inv_sort_name", _inventorySort == InventorySortMode.NameAsc ? UiTheme.Button : UiTheme.TabButton))
            {
                _inventorySort = InventorySortMode.NameAsc;
            }
            if (StyledButton(new Rect(panel.x + 192, panel.y + 40, 100, 22), "Кол-во", "inv_sort_count", _inventorySort == InventorySortMode.CountDesc ? UiTheme.Button : UiTheme.TabButton))
            {
                _inventorySort = InventorySortMode.CountDesc;
            }
            if (StyledButton(new Rect(panel.x + 296, panel.y + 40, 92, 22), "Тип", "inv_sort_type", _inventorySort == InventorySortMode.Type ? UiTheme.Button : UiTheme.TabButton))
            {
                _inventorySort = InventorySortMode.Type;
            }
        }

        private void DrawInventoryList(Rect panel)
        {
            var listRect = new Rect(panel.x + 14, panel.y + 68, panel.width - 28, panel.height - 110);
            GUI.Box(listRect, GUIContent.none, UiTheme.Panel);

            var entries = BuildInventoryEntries();
            float lineH = 24f;
            float contentH = entries.Count * lineH;
            float maxScroll = Mathf.Max(0f, contentH - listRect.height + 4f);
            _inventoryScroll = Mathf.Clamp(_inventoryScroll, 0f, maxScroll);

            if (listRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.ScrollWheel)
            {
                _inventoryScroll = Mathf.Clamp(_inventoryScroll + Event.current.delta.y * 14f, 0f, maxScroll);
                Event.current.Use();
            }

            int start = Mathf.FloorToInt(_inventoryScroll / lineH);
            int visible = Mathf.CeilToInt(listRect.height / lineH) + 1;
            int end = Mathf.Min(entries.Count, start + visible);
            float yOffset = listRect.y + 4f - (_inventoryScroll - start * lineH);

            for (int i = start; i < end; i++)
            {
                var e = entries[i];
                float y = yOffset + (i - start) * lineH;
                var row = new Rect(listRect.x + 4, y, listRect.width - 26, 22);
                GUI.DrawTexture(new Rect(row.x + 2, row.y + 3, 16, 16), e.Icon);
                GUI.Label(new Rect(row.x + 24, row.y + 1, 280, 20), e.DisplayName, UiTheme.Label);
                GUI.Label(new Rect(row.x + row.width - 90, row.y + 1, 80, 20), e.CountText, UiTheme.Label);

                if (e.IsBuilding && Event.current.type == EventType.MouseDown && Event.current.button == 0 && row.Contains(Event.current.mousePosition))
                {
                    _draggingHotbarType = e.BuildingType;
                    _hint = $"Перетащи {e.DisplayName} в слот нижней панели";
                    Event.current.Use();
                }
            }

            if (maxScroll > 0.001f)
            {
                float thumbH = Mathf.Max(24f, listRect.height * listRect.height / (contentH + 0.01f));
                float thumbY = listRect.y + (_inventoryScroll / maxScroll) * (listRect.height - thumbH);
                GUI.DrawTexture(new Rect(listRect.x + listRect.width - 8, thumbY, 6, thumbH), Texture2D.whiteTexture);
            }
        }

        private List<InventoryEntry> BuildInventoryEntries()
        {
            var list = new List<InventoryEntry>();
            foreach (var kv in _world.Inventory)
            {
                if (kv.Value <= 0.0001f) continue;
                list.Add(new InventoryEntry
                {
                    IsBuilding = false,
                    BuildingType = BuildingType.None,
                    DisplayName = kv.Key,
                    Count = kv.Value,
                    CountText = $"{kv.Value:0.##}",
                    Icon = GetResourceIcon(kv.Key),
                    Group = 0
                });
            }

            foreach (var kv in _craftedItems)
            {
                if (kv.Value <= 0) continue;
                list.Add(new InventoryEntry
                {
                    IsBuilding = true,
                    BuildingType = kv.Key,
                    DisplayName = kv.Key.ToString(),
                    Count = kv.Value,
                    CountText = kv.Value.ToString(),
                    Icon = GetBuildIcon(kv.Key),
                    Group = 1
                });
            }

            switch (_inventorySort)
            {
                case InventorySortMode.CountDesc:
                    list.Sort((a, b) =>
                    {
                        int c = b.Count.CompareTo(a.Count);
                        return c != 0 ? c : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
                    });
                    break;
                case InventorySortMode.Type:
                    list.Sort((a, b) =>
                    {
                        int g = a.Group.CompareTo(b.Group);
                        return g != 0 ? g : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
                    });
                    break;
                default:
                    list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
                    break;
            }

            return list;
        }

        private void DrawDraggedHotbarGhost()
        {
            if (!_draggingHotbarType.HasValue)
            {
                return;
            }

            Vector2 mp = Event.current.mousePosition;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(mp.x - 10, mp.y - 10, 20, 20), GetBuildIcon(_draggingHotbarType.Value));
            GUI.color = prev;
        }

        private void HandleHotbarDrop(int slotIndex, Rect slotRect)
        {
            if (!_draggingHotbarType.HasValue)
            {
                return;
            }

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                if (slotRect.Contains(Event.current.mousePosition))
                {
                    _hotbarSlots[slotIndex] = _draggingHotbarType.Value;
                    _hint = $"Закреплено в слоте {slotIndex + 1}: {_draggingHotbarType.Value}";
                    _draggingHotbarType = null;
                    Event.current.Use();
                }
            }
        }

        private void FinalizeHotbarDragIfReleased()
        {
            if (_draggingHotbarType.HasValue && Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                _draggingHotbarType = null;
            }
        }

        private void UpdateOnboarding(float deltaTime)
        {
            if (!_tutorialVisible)
            {
                return;
            }

            _tutorialElapsed += Mathf.Max(0f, deltaTime);
            if (_tutorialElapsed > 420f)
            {
                _tutorialVisible = false;
                return;
            }

            switch (_tutorialStep)
            {
                case 0:
                    if (_world.GetInventoryAmount("Aqua") >= 2f || _world.GetInventoryAmount("Ignis") >= 2f || _world.GetInventoryAmount("Terra") >= 2f)
                        _tutorialStep = 1;
                    break;
                case 1:
                    if (GetCraftedCount(BuildingType.Pipe) > 0 || _craftQueue.Count > 0 || _activeCraftType.HasValue)
                        _tutorialStep = 2;
                    break;
                case 2:
                    if (HasHotbarAssignment())
                        _tutorialStep = 3;
                    break;
                case 3:
                    if (_world.GetBuildingCount(BuildingType.Pipe) >= 1)
                        _tutorialStep = 4;
                    break;
                case 4:
                    if (_world.GetBuildingCount(BuildingType.Extractor) >= 1)
                        _tutorialStep = 5;
                    break;
                case 5:
                    if (_world.GetBuildingCount(BuildingType.Generator) >= 1 && _world.GetBuildingCount(BuildingType.PowerPole) >= 1)
                        _tutorialStep = 6;
                    break;
                case 6:
                    if (_currentGuildOrderIndex >= 1)
                        _tutorialVisible = false;
                    break;
            }
        }

        private bool HasHotbarAssignment()
        {
            for (int i = 0; i < HotbarSize; i++)
            {
                if (_hotbarSlots[i].HasValue)
                {
                    return true;
                }
            }
            return false;
        }

        private void DrawOnboardingOverlay(float sw, float sh)
        {
            var panel = new Rect(16, 16, Mathf.Min(560f, sw - 32f), 56f);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            string text = GetOnboardingText();
            GUI.Label(new Rect(panel.x + 10, panel.y + 8, panel.width - 120, 40), text, UiTheme.Label);
            if (StyledButton(new Rect(panel.x + panel.width - 96, panel.y + 14, 84, 28), "Скрыть", "tutorial_hide", UiTheme.Button))
            {
                _tutorialVisible = false;
            }
        }

        private string GetOnboardingText()
        {
            switch (_tutorialStep)
            {
                case 0: return "Шаг 1/7: Собери рукой (E) немного ресурсов с узлов.";
                case 1: return "Шаг 2/7: Открой крафт (K) и поставь в очередь хотя бы 1 трубу.";
                case 2: return "Шаг 3/7: Открой инвентарь (Tab) и перетащи сооружение в слот хотбара.";
                case 3: return "Шаг 4/7: Выбери слот (1..8) и поставь первую трубу.";
                case 4: return "Шаг 5/7: Скрафти и поставь экстрактор на ресурсный узел.";
                case 5: return "Шаг 6/7: Поставь генератор и столб, чтобы запитать сеть.";
                case 6: return "Шаг 7/7: Выполни первый заказ гильдии. Ты готов к демо!";
                default: return "Онбординг завершен.";
            }
        }

        private void InitializeHotbarDefaults()
        {
            for (int i = 0; i < HotbarSize; i++)
            {
                _hotbarSlots[i] = null;
                _hotbarSlotRects[i] = new Rect();
            }
        }

        private void HandleHotbarDigitSelection()
        {
            if (PressedDigit1()) SelectHotbarSlot(0);
            if (PressedDigit2()) SelectHotbarSlot(1);
            if (PressedDigit3()) SelectHotbarSlot(2);
            if (PressedDigit4()) SelectHotbarSlot(3);
            if (PressedDigit5()) SelectHotbarSlot(4);
            if (PressedDigit6()) SelectHotbarSlot(5);
            if (PressedDigit7()) SelectHotbarSlot(6);
            if (PressedDigit8()) SelectHotbarSlot(7);
            if (PressedDigit0()) _selected = BuildingType.None;
        }

        private void SelectHotbarSlot(int index)
        {
            if (index < 0 || index >= HotbarSize)
            {
                return;
            }

            var type = _hotbarSlots[index];
            if (!type.HasValue)
            {
                _selected = BuildingType.None;
                _hint = $"Слот {index + 1} пуст";
                return;
            }

            _selected = type.Value;
        }

        private void DrawPauseMenuOverlay(float sw, float sh, bool interactive)
        {
            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f * _pauseAnim);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = prevColor;

            const float w = 420f;
            const float h = 340f;
            float scale = Mathf.Lerp(0.96f, 1f, _pauseAnim);
            var panel = new Rect((sw - w * scale) * 0.5f, (sh - h * scale) * 0.5f, w * scale, h * scale);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(panel.x + 12, panel.y + 12, panel.width - 24, 28), "Пауза", UiTheme.Title);

            if (!interactive)
            {
                return;
            }

            if (StyledButton(new Rect(panel.x + 28, panel.y + 54, panel.width - 56, 38), "Продолжить", "pause_continue", UiTheme.Button))
            {
                _isPauseMenuOpen = false;
                _showPauseLoadSlots = false;
                _showPauseSaveSlots = false;
            }

            if (StyledButton(new Rect(panel.x + 28, panel.y + 98, panel.width - 56, 38), "Сохранить игру", "pause_save", UiTheme.Button))
            {
                _showPauseSaveSlots = !_showPauseSaveSlots;
                _showPauseLoadSlots = false;
                if (_showPauseSaveSlots)
                {
                    _pendingSaveName = GetSaveDisplayName(_activeSlot);
                }
            }

            if (StyledButton(new Rect(panel.x + 28, panel.y + 142, panel.width - 56, 38), "Загрузить игру", "pause_load", UiTheme.Button))
            {
                _showPauseLoadSlots = !_showPauseLoadSlots;
            }

            if (_showPauseLoadSlots)
            {
                if (StyledButton(new Rect(panel.x + 28, panel.y + 186, 116, 32), GetSlotShortLabel(1), "pause_slot1", UiTheme.Button))
                {
                    LoadGame(1);
                }
                if (StyledButton(new Rect(panel.x + 152, panel.y + 186, 116, 32), GetSlotShortLabel(2), "pause_slot2", UiTheme.Button))
                {
                    LoadGame(2);
                }
                if (StyledButton(new Rect(panel.x + 276, panel.y + 186, 116, 32), GetSlotShortLabel(3), "pause_slot3", UiTheme.Button))
                {
                    LoadGame(3);
                }
            }

            if (_showPauseSaveSlots)
            {
                GUI.Label(new Rect(panel.x + 28, panel.y + 182, panel.width - 56, 18), "Название сохранения:", UiTheme.Label);
                _pendingSaveName = GUI.TextField(new Rect(panel.x + 28, panel.y + 202, panel.width - 56, 24), _pendingSaveName ?? string.Empty, 32);
                if (StyledButton(new Rect(panel.x + 28, panel.y + 230, 116, 32), "Слот 1", "pause_save_slot1", UiTheme.Button))
                {
                    SaveGame(1, _pendingSaveName);
                }
                if (StyledButton(new Rect(panel.x + 152, panel.y + 230, 116, 32), "Слот 2", "pause_save_slot2", UiTheme.Button))
                {
                    SaveGame(2, _pendingSaveName);
                }
                if (StyledButton(new Rect(panel.x + 276, panel.y + 230, 116, 32), "Слот 3", "pause_save_slot3", UiTheme.Button))
                {
                    SaveGame(3, _pendingSaveName);
                }
            }

            if (!_showPauseSaveSlots)
            {
                if (StyledButton(new Rect(panel.x + 28, panel.y + 226, panel.width - 56, 38), "Настройки", "pause_settings", UiTheme.Button))
                {
                    _showSettings = !_showSettings;
                }

                if (StyledButton(new Rect(panel.x + 28, panel.y + 270, panel.width - 56, 38), "Выход в главное меню", "pause_mainmenu", UiTheme.Button))
                {
                    _isPauseMenuOpen = false;
                    _showPauseLoadSlots = false;
                    _showPauseSaveSlots = false;
                    SceneFlow.OpenMenuScene();
                }
            }
        }

        private void DrawSettingsPanel(float sw, float sh)
        {
            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.62f * _settingsAnim);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = prevColor;

            var w = 420f;
            var h = 246f;
            float scale = Mathf.Lerp(0.96f, 1f, _settingsAnim);
            var panel = new Rect((sw - w * scale) * 0.5f, (sh - h * scale) * 0.5f, w * scale, h * scale);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(panel.x + 12, panel.y + 10, panel.width - 24, 24), "Настройки", UiTheme.Title);

            GUI.Label(new Rect(panel.x + 16, panel.y + 52, 150, 20), $"Громкость: {(int)(GameSettings.MasterVolume * 100)}%", UiTheme.Label);
            float vol = GUI.HorizontalSlider(new Rect(panel.x + 170, panel.y + 58, panel.width - 186, 18), GameSettings.MasterVolume, 0f, 1f);
            if (Mathf.Abs(vol - GameSettings.MasterVolume) > 0.001f)
            {
                GameSettings.SetMasterVolume(vol);
                GameSettings.Save();
            }

            GUI.Label(new Rect(panel.x + 16, panel.y + 84, 150, 20), $"Масштаб UI: {GameSettings.UiScale:0.00}", UiTheme.Label);
            float uiScaleValue = GUI.HorizontalSlider(new Rect(panel.x + 170, panel.y + 90, panel.width - 186, 18), GameSettings.UiScale, 0.8f, 1.4f);
            if (Mathf.Abs(uiScaleValue - GameSettings.UiScale) > 0.001f)
            {
                GameSettings.SetUiScale(uiScaleValue);
                GameSettings.Save();
            }

            bool hints = GUI.Toggle(new Rect(panel.x + 16, panel.y + 116, 220, 24), GameSettings.ShowHints, "Показывать подсказки");
            if (hints != GameSettings.ShowHints)
            {
                GameSettings.SetShowHints(hints);
                GameSettings.Save();
            }

            GUI.Label(new Rect(panel.x + 16, panel.y + 142, 150, 20), $"Скорость зума: {GameSettings.CameraZoomSpeed:0.00}", UiTheme.Label);
            float zoomSpeed = GUI.HorizontalSlider(new Rect(panel.x + 170, panel.y + 148, panel.width - 186, 18), GameSettings.CameraZoomSpeed, 0.5f, 6f);
            if (Mathf.Abs(zoomSpeed - GameSettings.CameraZoomSpeed) > 0.001f)
            {
                GameSettings.SetCameraZoomSpeed(zoomSpeed);
                GameSettings.Save();
            }

            bool fullscreen = Screen.fullScreen;
            bool nextFullscreen = GUI.Toggle(new Rect(panel.x + 16, panel.y + 174, 220, 24), fullscreen, "Полноэкранный режим");
            if (nextFullscreen != fullscreen)
            {
                Screen.fullScreen = nextFullscreen;
            }

            if (StyledButton(new Rect(panel.x + panel.width - 132, panel.y + panel.height - 38, 120, 28), "Закрыть", "settings_close", UiTheme.Button))
            {
                _showSettings = false;
            }
        }

        private void DrawCraftWindow(float sw, float sh)
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = prev;

            float w = 560f;
            float h = 390f;
            var panel = new Rect((sw - w) * 0.5f, (sh - h) * 0.5f, w, h);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(panel.x + 14, panel.y + 10, panel.width - 28, 26), "Крафт построек", UiTheme.Title);

            if (_activeCraftType.HasValue)
            {
                float total = GetCraftDuration(_activeCraftType.Value);
                float p = total > 0.001f ? Mathf.Clamp01(_activeCraftProgress / total) : 0f;
                string pauseSuffix = _isCraftQueuePaused ? " [ПАУЗА]" : string.Empty;
                GUI.Label(new Rect(panel.x + 14, panel.y + 34, panel.width - 28, 18), $"В работе: {_activeCraftType.Value} ({(int)(p * 100f)}%){pauseSuffix}  |  В очереди: {_craftQueue.Count}", UiTheme.Label);
                GUI.Box(new Rect(panel.x + 14, panel.y + 54, panel.width - 28, 10), GUIContent.none, UiTheme.Panel);
                GUI.color = new Color(0.25f, 0.75f, 1f, 0.9f);
                GUI.DrawTexture(new Rect(panel.x + 16, panel.y + 56, (panel.width - 32) * p, 6), Texture2D.whiteTexture);
                GUI.color = prev;
            }
            else
            {
                string pauseSuffix = _isCraftQueuePaused ? " [ПАУЗА]" : string.Empty;
                GUI.Label(new Rect(panel.x + 14, panel.y + 36, panel.width - 28, 18), $"В очереди: {_craftQueue.Count}{pauseSuffix}", UiTheme.Label);
            }

            DrawCraftQueueStrip(panel);

            DrawCraftRow(panel, 0, BuildingType.Pipe, "Труба");
            DrawCraftRow(panel, 1, BuildingType.PipeConnector, "Соединитель");
            DrawCraftRow(panel, 2, BuildingType.PipeSplitter, "Разделитель");
            DrawCraftRow(panel, 3, BuildingType.Extractor, "Экстрактор");
            DrawCraftRow(panel, 4, BuildingType.Storage, "Хранилище");
            DrawCraftRow(panel, 5, BuildingType.Mixer, "Смеситель");
            DrawCraftRow(panel, 6, BuildingType.Generator, "Генератор");
            DrawCraftRow(panel, 7, BuildingType.PowerPole, "Столб");
            DrawCraftRow(panel, 8, BuildingType.MarketTerminal, "Рынок");

            bool hasQueuedOrActive = _craftQueue.Count > 0 || _activeCraftType.HasValue;
            GUI.enabled = _craftQueue.Count > 0;
            if (StyledButton(new Rect(panel.x + 14, panel.y + panel.height - 38, 100, 28), "Cancel Last", "craft_cancel_last", UiTheme.Button))
            {
                CancelLastQueuedCraft();
            }

            GUI.enabled = hasQueuedOrActive;
            if (StyledButton(new Rect(panel.x + 120, panel.y + panel.height - 38, 100, 28), "Cancel All", "craft_cancel_all", UiTheme.Button))
            {
                CancelAllQueuedCraft();
            }

            GUI.enabled = hasQueuedOrActive;
            if (StyledButton(new Rect(panel.x + 226, panel.y + panel.height - 38, 118, 28), _isCraftQueuePaused ? "Resume" : "Pause", "craft_pause_resume", UiTheme.Button))
            {
                ToggleCraftQueuePause();
            }

            bool canReorder = _selectedCraftQueueIndex >= 0 && _selectedCraftQueueIndex < _craftQueue.Count && _craftQueue.Count > 1;
            GUI.enabled = canReorder;
            if (StyledButton(new Rect(panel.x + 366, panel.y + 66, 36, 24), "↑", "craft_move_up", UiTheme.Button))
            {
                MoveSelectedCraftQueueItem(-1);
            }
            if (StyledButton(new Rect(panel.x + 406, panel.y + 66, 36, 24), "↓", "craft_move_down", UiTheme.Button))
            {
                MoveSelectedCraftQueueItem(1);
            }
            if (StyledButton(new Rect(panel.x + 446, panel.y + 66, 96, 24), "В начало", "craft_move_first", UiTheme.Button))
            {
                MoveSelectedCraftQueueToFront();
            }
            GUI.enabled = true;
            GUI.Label(new Rect(panel.x + 14, panel.y + 66, 346, 22),
                _selectedCraftQueueIndex >= 0 && _selectedCraftQueueIndex < _craftQueue.Count
                    ? $"Выбрано в очереди: {_craftQueue[_selectedCraftQueueIndex]} (#{_selectedCraftQueueIndex + 1})"
                    : "Выбери элемент в полосе очереди для изменения приоритета",
                UiTheme.Label);

            GUI.enabled = true;

            if (StyledButton(new Rect(panel.x + panel.width - 132, panel.y + panel.height - 38, 120, 28), "Закрыть", "craft_close", UiTheme.Button))
            {
                _showCraftWindow = false;
            }
        }

        private void DrawCraftQueueStrip(Rect panel)
        {
            var strip = new Rect(panel.x + 14, panel.y + 92, panel.width - 28, 26);
            GUI.Box(strip, GUIContent.none, UiTheme.Panel);
            ClampSelectedCraftQueueIndex();
            int maxShown = Mathf.Min(10, _craftQueue.Count);
            for (int i = 0; i < maxShown; i++)
            {
                float x = strip.x + 6 + i * 24;
                var iconRect = new Rect(x, strip.y + 4, 18, 18);
                if (_selectedCraftQueueIndex == i)
                {
                    var prev = GUI.color;
                    GUI.color = new Color(0.25f, 0.75f, 1f, 0.9f);
                    GUI.DrawTexture(new Rect(x - 2, strip.y + 2, 22, 22), Texture2D.whiteTexture);
                    GUI.color = prev;
                }
                GUI.DrawTexture(iconRect, GetBuildIcon(_craftQueue[i]));

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && iconRect.Contains(Event.current.mousePosition))
                {
                    _selectedCraftQueueIndex = i;
                    _hint = $"Выбран элемент очереди: {_craftQueue[i]}";
                    Event.current.Use();
                }
            }

            if (_craftQueue.Count > maxShown)
            {
                GUI.Label(new Rect(strip.x + 6 + maxShown * 24, strip.y + 4, 60, 18), $"+{_craftQueue.Count - maxShown}", UiTheme.Label);
            }
        }

        private void DrawCraftRow(Rect panel, int row, BuildingType type, string title)
        {
            float y = panel.y + 96 + row * 28;
            GUI.DrawTexture(new Rect(panel.x + 14, y + 6, 18, 18), GetBuildIcon(type));
            GUI.Label(new Rect(panel.x + 38, y + 4, 120, 22), title, UiTheme.Label);
            GUI.Label(new Rect(panel.x + 160, y + 4, 250, 22), GetBuildCostText(type), UiTheme.Label);
            GUI.Label(new Rect(panel.x + 416, y + 4, 60, 22), $"x{GetCraftedCount(type)}", UiTheme.Label);
            bool canCraft = IsBuildingUnlocked(type) && CanAfford(type);
            GUI.enabled = canCraft;
            if (StyledButton(new Rect(panel.x + 474, y + 2, 32, 26), "+1", $"craft_{type}_1", UiTheme.Button))
            {
                int queued = TryQueueCraft(type, 1);
                if (queued > 0)
                {
                    _hint = $"В очередь: {type} x{queued}";
                }
            }
            if (StyledButton(new Rect(panel.x + 510, y + 2, 34, 26), "+5", $"craft_{type}_5", UiTheme.Button))
            {
                int queued = TryQueueCraft(type, 5);
                if (queued > 0)
                {
                    _hint = $"В очередь: {type} x{queued}";
                }
            }
            GUI.enabled = true;
        }

        private void DrawTooltip(float sw, float sh, string text)
        {
            var mp = Event.current.mousePosition;
            var size = UiTheme.Label.CalcSize(new GUIContent(text));
            var rect = new Rect(Mathf.Min(mp.x + 14, sw - size.x - 24), Mathf.Min(mp.y + 14, sh - 34), size.x + 14, 24);
            GUI.Box(rect, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(rect.x + 7, rect.y + 3, rect.width - 10, 18), text, UiTheme.Label);
        }

        private bool StyledButton(Rect rect, string label, string controlId, GUIStyle style)
        {
            bool hovered = rect.Contains(Event.current.mousePosition);
            if (hovered && _lastHoveredControlId != controlId)
            {
                _lastHoveredControlId = controlId;
                PlayUiHover();
            }

            bool clicked = GUI.Button(rect, label, style ?? UiTheme.Button);
            if (clicked)
            {
                PlayUiClick();
            }

            return clicked;
        }

        private void PlayUiHover()
        {
            if (_uiAudioSource != null && _uiHoverClip != null)
            {
                _uiAudioSource.PlayOneShot(_uiHoverClip, 0.7f);
            }
        }

        private void PlayUiClick()
        {
            if (_uiAudioSource != null && _uiClickClip != null)
            {
                _uiAudioSource.PlayOneShot(_uiClickClip, 0.9f);
            }
        }

        private static AudioClip CreateToneClip(float frequency, float duration, float amplitude)
        {
            int sampleRate = 44100;
            int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = 1f - (i / (float)samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * env;
            }

            var clip = AudioClip.Create($"ui_{frequency}", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private Texture2D GetBuildIcon(BuildingType type)
        {
            if (_buildIcons.TryGetValue(type, out var cached))
            {
                return cached;
            }

            if (type == BuildingType.None)
            {
                var hand = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        bool on = (x > 3 && x < 12 && y > 3 && y < 12);
                        hand.SetPixel(x, y, on ? new Color(0.95f, 0.95f, 0.95f, 1f) : new Color(0, 0, 0, 0));
                    }
                }
                hand.Apply();
                _buildIcons[type] = hand;
                return hand;
            }

            var tex = BuildingSpriteFactory.GetIconTexture(type);
            _buildIcons[type] = tex;
            return tex;
        }

        private Texture2D GetResourceIcon(string resourceKey)
        {
            if (_resourceIcons.TryGetValue(resourceKey, out var cached))
            {
                return cached;
            }

            Color color = resourceKey switch
            {
                "Aqua" => new Color(0.25f, 0.65f, 1f, 1f),
                "Ignis" => new Color(1f, 0.45f, 0.2f, 1f),
                "Terra" => new Color(0.42f, 0.8f, 0.28f, 1f),
                "Aeris" => new Color(0.75f, 0.95f, 1f, 1f),
                _ => new Color(0.8f, 0.8f, 0.8f, 1f)
            };

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool border = x == 0 || y == 0 || x == 15 || y == 15;
                    tex.SetPixel(x, y, border ? new Color(0.08f, 0.1f, 0.14f, 1f) : color);
                }
            }
            tex.Apply(false, true);
            _resourceIcons[resourceKey] = tex;
            return tex;
        }

        private void CreatePlacementPreview()
        {
            _preview = new GameObject("PlacementPreview");
            _previewRenderer = _preview.AddComponent<SpriteRenderer>();
            _previewRenderer.sprite = BuildingSpriteFactory.GetWorldSprite(BuildingType.Pipe);
            _previewRenderer.sortingOrder = 20;
            _preview.transform.localScale = new Vector3(0.92f, 0.92f, 1f);

            var arrow = new GameObject("PlacementArrow");
            arrow.transform.SetParent(_preview.transform, false);
            arrow.transform.localPosition = new Vector3(0.24f, 0f, -0.04f);
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

            _preview.transform.position = new Vector3(cell.x, cell.y, -0.3f);
            _preview.transform.rotation = _selected == BuildingType.Pipe
                ? Quaternion.identity
                : Quaternion.Euler(0f, 0f, DirToAngle(_direction));
            _previewRenderer.sprite = BuildingSpriteFactory.GetWorldSprite(_selected);
            _previewRenderer.color = canPlace
                ? new Color(0.3f, 1f, 0.4f, 0.35f)
                : new Color(1f, 0.25f, 0.25f, 0.35f);
            if (_previewArrowRenderer != null)
            {
                _previewArrowRenderer.enabled = _selected != BuildingType.Storage && _selected != BuildingType.Pipe;
                _previewArrowRenderer.color = canPlace
                    ? new Color(1f, 1f, 1f, 0.9f)
                    : new Color(1f, 0.4f, 0.4f, 0.9f);
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
            if (type == BuildingType.None || type == BuildingType.Pipe || type == BuildingType.PipeConnector || type == BuildingType.PipeSplitter || type == BuildingType.Extractor || type == BuildingType.Storage)
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
                _currentGoalIndex++;
                _hint = _currentGoalIndex < _goals.Length
                    ? $"Цель выполнена: {goal.Title}. Следующая: {_goals[_currentGoalIndex].Title}"
                    : "Все цели Акта 1 demo выполнены. Отличная фабрика!";
            }

            TryAdvanceGuildOrders();
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
            PlayerPrefs.SetInt(KeyForSlot(SaveKeyCoins, slot), _coins);
            PlayerPrefs.SetInt(KeyForSlot(SaveKeyUpgExtractor, slot), _upgExtractor);
            PlayerPrefs.SetInt(KeyForSlot(SaveKeyUpgMixer, slot), _upgMixer);
            PlayerPrefs.SetInt(KeyForSlot(SaveKeyUpgPower, slot), _upgPower);
            PlayerPrefs.SetString(KeyForSlot(SaveKeyCrafted, slot), SerializeCraftedItems());
            PlayerPrefs.SetString(KeyForSlot(SaveKeyCraftQueue, slot), SerializeCraftQueueState());
            PlayerPrefs.SetString(KeyForSlot(SaveKeyHotbar, slot), SerializeHotbarState());
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
            _upgExtractor = Mathf.Max(0, PlayerPrefs.GetInt(KeyForSlot(SaveKeyUpgExtractor, slot), 0));
            _upgMixer = Mathf.Max(0, PlayerPrefs.GetInt(KeyForSlot(SaveKeyUpgMixer, slot), 0));
            _upgPower = Mathf.Max(0, PlayerPrefs.GetInt(KeyForSlot(SaveKeyUpgPower, slot), 0));
            DeserializeCraftedItems(PlayerPrefs.GetString(KeyForSlot(SaveKeyCrafted, slot), string.Empty));
            DeserializeCraftQueueState(PlayerPrefs.GetString(KeyForSlot(SaveKeyCraftQueue, slot), string.Empty));
            DeserializeHotbarState(PlayerPrefs.GetString(KeyForSlot(SaveKeyHotbar, slot), string.Empty));
            _showInventoryWindow = false;
            _draggingHotbarType = null;
            _inventoryScroll = 0f;
            _showPauseSaveSlots = false;
            _selected = BuildingType.None;
            _tutorialStep = 0;
            _tutorialElapsed = 0f;
            _tutorialVisible = false;
            SyncUpgradeLevelsFromWorld();
            _hint = $"Загрузка из слота {slot}";
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
                player.transform.position = new Vector3(safeSpawn.x, safeSpawn.y, 0f);
            }
            _currentGoalIndex = 0;
            _currentGuildOrderIndex = 0;
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
            _draggingHotbarType = null;
            _inventoryScroll = 0f;
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

            const int cost = 30;
            if (_coins < cost)
            {
                _hint = "Недостаточно монет для апгрейда добычи";
                return;
            }

            _coins -= cost;
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

            const int cost = 50;
            if (_coins < cost)
            {
                _hint = "Недостаточно монет для апгрейда смесителя";
                return;
            }

            _coins -= cost;
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

            const int cost = 70;
            if (_coins < cost)
            {
                _hint = "Недостаточно монет для апгрейда энергосети";
                return;
            }

            _coins -= cost;
            _upgPower++;
            _world.ApplyPowerReachUpgrade(1);
            _hint = "Куплен апгрейд энергосети (+1 дальность)";
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
                return "Текущая цель: кампания завершена";
            }

            var goal = _goals[_currentGoalIndex];
            var current = _world.GetInventoryAmount(goal.ResourceKey);
            return $"Текущая цель: {goal.Title} ({goal.ResourceKey} {current:0.##}/{goal.RequiredAmount:0.##})";
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
            return new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
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
                var settingsPanel = new Rect((sw - 420f) * 0.5f, (sh - 246f) * 0.5f, 420f, 246f);
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

        private sealed class Goal
        {
            public string Title { get; }
            public string ResourceKey { get; }
            public float RequiredAmount { get; }

            public Goal(string title, string resourceKey, float requiredAmount)
            {
                Title = title;
                ResourceKey = resourceKey;
                RequiredAmount = requiredAmount;
            }
        }

        private sealed class GuildOrder
        {
            public string Title { get; }
            public string ResourceKey { get; }
            public float RequiredAmount { get; }
            public int RewardCoins { get; }

            public GuildOrder(string title, string resourceKey, float requiredAmount, int rewardCoins)
            {
                Title = title;
                ResourceKey = resourceKey;
                RequiredAmount = requiredAmount;
                RewardCoins = rewardCoins;
            }
        }

        private struct InventoryEntry
        {
            public bool IsBuilding;
            public BuildingType BuildingType;
            public string DisplayName;
            public float Count;
            public string CountText;
            public Texture2D Icon;
            public int Group;
        }
    }
}
