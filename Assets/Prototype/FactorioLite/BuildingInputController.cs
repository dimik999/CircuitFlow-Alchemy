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
        private enum HudTab
        {
            Build,
            Orders,
            Inventory,
            Upgrades
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

        private WorldGridSystem _world;
        private UiScreen _screen = UiScreen.InGame;
        private BuildingType _selected = BuildingType.Pipe;
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
        private int _activeSlot = 1;
        private HudTab _activeTab = HudTab.Build;
        private float _pauseAnim;
        private float _settingsAnim;
        private string _hoverTooltip;
        private readonly Dictionary<BuildingType, Texture2D> _buildIcons = new Dictionary<BuildingType, Texture2D>();
        private readonly Dictionary<BuildingType, int> _craftedItems = new Dictionary<BuildingType, int>();
        private readonly List<BuildingType> _craftQueue = new List<BuildingType>();
        private GUIStyle _slotLabelStyle;
        private AudioSource _uiAudioSource;
        private AudioClip _uiHoverClip;
        private AudioClip _uiClickClip;
        private string _lastHoveredControlId;
        private bool _showCraftWindow;
        private BuildingType? _activeCraftType;
        private float _activeCraftProgress;

        private void Start()
        {
            _world = FindFirstObjectByType<WorldGridSystem>();
            _hint = "1-Труба | 2-Экстрактор | 3-Хранилище | 4-Смеситель | R-поворот";
            _goals = new[]
            {
                new Goal("1-1 Собрать воду", "Aqua", 8f),
                new Goal("1-2 Собрать огонь", "Ignis", 8f),
                new Goal("1-3 Получить пар", "Aeris", 6f),
                new Goal("1-4 Запустить базовую фабрику", "Terra", 6f)
            };
            _guildOrders = new[]
            {
                new GuildOrder("Заказ Гильдии: Первые поставки", "Aqua", 10f, 40),
                new GuildOrder("Заказ Гильдии: Пламя для кузни", "Ignis", 10f, 50),
                new GuildOrder("Заказ Гильдии: Паровая линия", "Aeris", 8f, 80),
                new GuildOrder("Заказ Гильдии: Электрификация", "PowerSetup", 1f, 120)
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

            if (_screen != UiScreen.InGame)
            {
                if (_preview != null) _preview.SetActive(false);
                return;
            }

            if (PressedEscape())
            {
                _isPauseMenuOpen = !_isPauseMenuOpen;
                _showPauseLoadSlots = false;
                if (_isPauseMenuOpen)
                {
                    _showSettings = false;
                }
            }

            if (_isPauseMenuOpen || _showSettings || _showCraftWindow)
            {
                if (_preview != null) _preview.SetActive(false);
                return;
            }

            if (_preview != null && !_preview.activeSelf) _preview.SetActive(true);

            if (PressedDigit1()) _selected = BuildingType.Pipe;
            if (PressedDigit2()) _selected = BuildingType.Extractor;
            if (PressedDigit3()) _selected = BuildingType.Storage;
            if (PressedDigit4() && IsMixerUnlocked()) _selected = BuildingType.Mixer;
            if (PressedDigit5() && IsPowerUnlocked()) _selected = BuildingType.Generator;
            if (PressedDigit6() && IsPowerUnlocked()) _selected = BuildingType.PowerPole;
            if (PressedDigit7() && IsPowerUnlocked()) _selected = BuildingType.MarketTerminal;
            if (PressedDigit0()) _selected = BuildingType.None;
            if (PressedRotate()) RotateDirectionClockwise();
            if (PressedSave()) SaveGame();
            if (PressedLoad()) LoadGame();
            if (PressedToggleInventory()) _activeTab = _activeTab == HudTab.Inventory ? HudTab.Build : HudTab.Inventory;
            if (PressedToggleCraftWindow()) _showCraftWindow = !_showCraftWindow;
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

            if (PressedHandGather())
            {
                bool gathered = _world.TryGatherByHand(cell, 1f);
                _hint = gathered ? "Собрано вручную +1" : "Здесь нельзя собрать ресурс рукой";
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
                _world.TryRemove(cell);
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
        }

        private void ApplyUiModeState()
        {
            bool gameplay = _screen == UiScreen.InGame && !_isPauseMenuOpen && !_showSettings;
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
            if (StyledButton(new Rect(hud.x + 242, tabY, 110, 28), "Inventory", "tab_inventory", _activeTab == HudTab.Inventory ? UiTheme.Button : UiTheme.TabButton)) _activeTab = HudTab.Inventory;
            if (StyledButton(new Rect(hud.x + 358, tabY, 110, 28), "Upgrades", "tab_upgrades", _activeTab == HudTab.Upgrades ? UiTheme.Button : UiTheme.TabButton)) _activeTab = HudTab.Upgrades;

            switch (_activeTab)
            {
                case HudTab.Build:
                    DrawBuildTab(hud);
                    break;
                case HudTab.Orders:
                    DrawOrdersTab(hud);
                    break;
                case HudTab.Inventory:
                    DrawInventoryTab(hud);
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
            DrawBuildSlot(startX + (slotW + gap) * 0, y, "1", "Pipe", BuildingType.Pipe);
            DrawBuildSlot(startX + (slotW + gap) * 1, y, "2", "Extract", BuildingType.Extractor);
            DrawBuildSlot(startX + (slotW + gap) * 2, y, "3", "Store", BuildingType.Storage);
            DrawBuildSlot(startX + (slotW + gap) * 3, y, "4", "Mixer", BuildingType.Mixer);
            DrawBuildSlot(startX + (slotW + gap) * 4, y, "5", "Gen", BuildingType.Generator);
            DrawBuildSlot(startX + (slotW + gap) * 5, y, "6", "Pole", BuildingType.PowerPole);
            DrawBuildSlot(startX + (slotW + gap) * 6, y, "7", "Market", BuildingType.MarketTerminal);
            DrawBuildSlot(startX + (slotW + gap) * 7, y, "0", "Hand", BuildingType.None);
            if (StyledButton(new Rect(hud.x + hud.width - 132, y + 12, 120, 34), "Craft (K)", "open_craft_window", UiTheme.Button))
            {
                _showCraftWindow = !_showCraftWindow;
            }
            if (GameSettings.ShowHints)
            {
                GUI.Label(new Rect(hud.x + 12, hud.y + 136, hud.width - 24, 20), $"Подсказка: {_hint}", UiTheme.Label);
            }
        }

        private void DrawBuildSlot(float x, float y, string key, string name, BuildingType type)
        {
            bool selected = _selected == type;
            var rect = new Rect(x, y, 100, 60);
            bool hovered = rect.Contains(Event.current.mousePosition);
            var prev = GUI.color;
            GUI.color = selected
                ? new Color(0.27f, 0.52f, 0.92f, 0.95f)
                : (hovered ? new Color(0.2f, 0.28f, 0.38f, 0.95f) : new Color(0.12f, 0.15f, 0.2f, 0.9f));
            GUI.Box(rect, GUIContent.none, UiTheme.Panel);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x + 8, y + 8, 20, 20), GetBuildIcon(type));
            GUI.color = prev;
            GUI.Label(new Rect(x + 32, y + 6, 62, 18), $"[{key}]", _slotLabelStyle);
            GUI.Label(new Rect(x + 30, y + 24, 66, 26), name, _slotLabelStyle);
            if (type != BuildingType.None)
            {
                GUI.Label(new Rect(x + 4, y + 42, 92, 14), $"x{GetCraftedCount(type)}", _slotLabelStyle);
            }
            if (hovered)
            {
                string hoverId = $"build_slot_{type}";
                if (_lastHoveredControlId != hoverId)
                {
                    _lastHoveredControlId = hoverId;
                    PlayUiHover();
                }
                _hoverTooltip = type == BuildingType.None
                    ? "Свободная рука: режим без строительства"
                    : $"{type}: в кармане {GetCraftedCount(type)} | крафт: {GetBuildCostText(type)}";
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                if (IsBuildingUnlocked(type))
                {
                    _selected = type;
                    PlayUiClick();
                }
                else
                {
                    _hint = $"Объект {type} пока не разблокирован";
                }
                Event.current.Use();
            }
        }

        private void DrawOrdersTab(Rect hud)
        {
            GUI.Label(new Rect(hud.x + 12, hud.y + 48, hud.width - 24, 20), GoalText(), UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 74, hud.width - 24, 20), $"Гильдия: {GuildOrderText()}", UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 100, hud.width - 24, 20), $"Прогресс кампании: {_currentGoalIndex}/{_goals.Length}", UiTheme.Label);
        }

        private void DrawInventoryTab(Rect hud)
        {
            float aqua = GetInventory("Aqua");
            float ignis = GetInventory("Ignis");
            float terra = GetInventory("Terra");
            float aeris = GetInventory("Aeris");
            GUI.Label(new Rect(hud.x + 12, hud.y + 48, hud.width - 24, 20), $"Aqua: {aqua:0.##} | Ignis: {ignis:0.##}", UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 74, hud.width - 24, 20), $"Terra: {terra:0.##} | Aeris: {aeris:0.##}", UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 100, hud.width - 24, 20), $"Монеты: {_coins} | Слот: {_activeSlot}", UiTheme.Label);
        }

        private void DrawUpgradesTab(Rect hud)
        {
            GUI.Label(new Rect(hud.x + 12, hud.y + 48, hud.width - 24, 20), UpgradeText(), UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 74, hud.width - 24, 20), "U: добыча (+50%) | I: смеситель (+25%) | O: энергосеть (+1)", UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 100, hud.width - 24, 20), "Для покупки апгрейдов нужен Market (7).", UiTheme.Label);
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
            }

            if (StyledButton(new Rect(panel.x + 28, panel.y + 98, panel.width - 56, 38), "Сохранить игру (слот 1)", "pause_save", UiTheme.Button))
            {
                _activeSlot = 1;
                SaveGame(1);
            }

            if (StyledButton(new Rect(panel.x + 28, panel.y + 142, panel.width - 56, 38), "Загрузить игру", "pause_load", UiTheme.Button))
            {
                _showPauseLoadSlots = !_showPauseLoadSlots;
            }

            if (_showPauseLoadSlots)
            {
                if (StyledButton(new Rect(panel.x + 28, panel.y + 186, 116, 32), "Слот 1", "pause_slot1", UiTheme.Button))
                {
                    LoadGame(1);
                }
                if (StyledButton(new Rect(panel.x + 152, panel.y + 186, 116, 32), "Слот 2", "pause_slot2", UiTheme.Button))
                {
                    LoadGame(2);
                }
                if (StyledButton(new Rect(panel.x + 276, panel.y + 186, 116, 32), "Слот 3", "pause_slot3", UiTheme.Button))
                {
                    LoadGame(3);
                }
            }

            if (StyledButton(new Rect(panel.x + 28, panel.y + 226, panel.width - 56, 38), "Настройки", "pause_settings", UiTheme.Button))
            {
                _showSettings = !_showSettings;
            }

            if (StyledButton(new Rect(panel.x + 28, panel.y + 270, panel.width - 56, 38), "Выход в главное меню", "pause_mainmenu", UiTheme.Button))
            {
                _isPauseMenuOpen = false;
                _showPauseLoadSlots = false;
                SceneFlow.OpenMenuScene();
            }
        }

        private void DrawSettingsPanel(float sw, float sh)
        {
            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.62f * _settingsAnim);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = prevColor;

            var w = 420f;
            var h = 210f;
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

            bool fullscreen = Screen.fullScreen;
            bool nextFullscreen = GUI.Toggle(new Rect(panel.x + 16, panel.y + 142, 220, 24), fullscreen, "Полноэкранный режим");
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
            float h = 330f;
            var panel = new Rect((sw - w) * 0.5f, (sh - h) * 0.5f, w, h);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(panel.x + 14, panel.y + 10, panel.width - 28, 26), "Крафт построек", UiTheme.Title);

            if (_activeCraftType.HasValue)
            {
                float total = GetCraftDuration(_activeCraftType.Value);
                float p = total > 0.001f ? Mathf.Clamp01(_activeCraftProgress / total) : 0f;
                GUI.Label(new Rect(panel.x + 14, panel.y + 34, panel.width - 28, 18), $"В работе: {_activeCraftType.Value} ({(int)(p * 100f)}%)  |  В очереди: {_craftQueue.Count}", UiTheme.Label);
                GUI.Box(new Rect(panel.x + 14, panel.y + 54, panel.width - 28, 10), GUIContent.none, UiTheme.Panel);
                GUI.color = new Color(0.25f, 0.75f, 1f, 0.9f);
                GUI.DrawTexture(new Rect(panel.x + 16, panel.y + 56, (panel.width - 32) * p, 6), Texture2D.whiteTexture);
                GUI.color = prev;
            }
            else
            {
                GUI.Label(new Rect(panel.x + 14, panel.y + 36, panel.width - 28, 18), $"В очереди: {_craftQueue.Count}", UiTheme.Label);
            }

            DrawCraftQueueStrip(panel);

            DrawCraftRow(panel, 0, BuildingType.Pipe, "Труба");
            DrawCraftRow(panel, 1, BuildingType.Extractor, "Экстрактор");
            DrawCraftRow(panel, 2, BuildingType.Storage, "Хранилище");
            DrawCraftRow(panel, 3, BuildingType.Mixer, "Смеситель");
            DrawCraftRow(panel, 4, BuildingType.Generator, "Генератор");
            DrawCraftRow(panel, 5, BuildingType.PowerPole, "Столб");
            DrawCraftRow(panel, 6, BuildingType.MarketTerminal, "Рынок");

            GUI.enabled = _craftQueue.Count > 0;
            if (StyledButton(new Rect(panel.x + 14, panel.y + panel.height - 38, 120, 28), "Cancel Last", "craft_cancel_last", UiTheme.Button))
            {
                CancelLastQueuedCraft();
            }
            GUI.enabled = true;

            if (StyledButton(new Rect(panel.x + panel.width - 132, panel.y + panel.height - 38, 120, 28), "Закрыть", "craft_close", UiTheme.Button))
            {
                _showCraftWindow = false;
            }
        }

        private void DrawCraftQueueStrip(Rect panel)
        {
            var strip = new Rect(panel.x + 14, panel.y + 66, panel.width - 28, 26);
            GUI.Box(strip, GUIContent.none, UiTheme.Panel);
            int maxShown = Mathf.Min(10, _craftQueue.Count);
            for (int i = 0; i < maxShown; i++)
            {
                float x = strip.x + 6 + i * 24;
                GUI.DrawTexture(new Rect(x, strip.y + 4, 18, 18), GetBuildIcon(_craftQueue[i]));
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
            _preview.transform.rotation = Quaternion.Euler(0f, 0f, DirToAngle(_direction));
            _previewRenderer.sprite = BuildingSpriteFactory.GetWorldSprite(_selected);
            _previewRenderer.color = canPlace
                ? new Color(0.3f, 1f, 0.4f, 0.35f)
                : new Color(1f, 0.25f, 0.25f, 0.35f);
            if (_previewArrowRenderer != null)
            {
                _previewArrowRenderer.enabled = _selected != BuildingType.Storage;
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
            if (type == BuildingType.None || type == BuildingType.Pipe || type == BuildingType.Extractor || type == BuildingType.Storage)
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
            _hint = $"Отменено: {type} (ресурсы возвращены)";
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
                case BuildingType.Pipe: return 0.35f;
                case BuildingType.PowerPole: return 0.45f;
                case BuildingType.Storage: return 0.65f;
                case BuildingType.Extractor: return 0.8f;
                case BuildingType.Mixer: return 1.0f;
                case BuildingType.Generator: return 1.2f;
                case BuildingType.MarketTerminal: return 1.4f;
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
                case BuildingType.Extractor:
                    list.Add(("Terra", 4f));
                    list.Add(("Ignis", 1f));
                    break;
                case BuildingType.Storage:
                    list.Add(("Terra", 3f));
                    list.Add(("Aqua", 1f));
                    break;
                case BuildingType.Mixer:
                    list.Add(("Terra", 4f));
                    list.Add(("Aqua", 2f));
                    list.Add(("Ignis", 2f));
                    break;
                case BuildingType.Generator:
                    list.Add(("Terra", 5f));
                    list.Add(("Ignis", 3f));
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
            SaveGame(_activeSlot);
        }

        private void LoadGame()
        {
            LoadGame(_activeSlot);
        }

        private void SaveGame(int slot)
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
            PlayerPrefs.Save();
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
            _craftQueue.Clear();
            _activeCraftType = null;
            _activeCraftProgress = 0f;
            SyncUpgradeLevelsFromWorld();
            _hint = $"Загрузка из слота {slot}";
        }

        private bool HasSave(int slot)
        {
            return PlayerPrefs.HasKey(KeyForSlot(SaveKeyWorld, slot));
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

        private void StartNewGame()
        {
            _world.ResetRuntimeState();
            var player = FindFirstObjectByType<PlayerController2D>();
            if (player != null)
            {
                player.transform.position = Vector3.zero;
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
            _showCraftWindow = false;
            _selected = BuildingType.Pipe;
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
    }
}
