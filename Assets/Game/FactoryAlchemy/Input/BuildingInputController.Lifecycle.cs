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
        private void Start()
        {
            _world = FindFirstObjectByType<WorldGridSystem>();
            _hint = "1-Труба | 2-Экстрактор | 3-Хранилище | 4-Смеситель | R-поворот";
            InitializeHotbarDefaults();
            _goals = new[]
            {
                new Goal("1-1 Собрать воду (награда 20 зол.)", "Aqua", 6f, 20),
                new Goal("1-2 Собрать огонь (награда 25 зол.)", "Ignis", 6f, 25),
                new Goal("1-3 Получить пар (награда 30 зол.)", "Aeris", 5f, 30),
                new Goal("1-4 Запустить базовую фабрику (награда 35 зол.)", "Terra", 5f, 35)
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
                    _showBuildingWindow = false;
                    _isPauseMenuOpen = false;
                    _showSettings = false;
                }
            }

            if (PressedToggleCraftWindow() && !_showInventoryWindow)
            {
                _showCraftWindow = !_showCraftWindow;
                if (_showCraftWindow)
                {
                    _showBuildingWindow = false;
                }
            }

            if (_isPauseMenuOpen || _showSettings || _showCraftWindow || _showInventoryWindow || _showBuildingWindow)
            {
                if (_preview != null) _preview.SetActive(false);
                return;
            }

            if (_preview != null && !_preview.activeSelf) _preview.SetActive(true);

            HandleHotbarDigitSelection();
            if (PressedRotate()) RotateDirectionClockwise();
            if (PressedSave()) SaveGame();
            if (PressedLoad()) LoadGame();

            Vector2Int cell = MouseToCell();
            Vector2Int interactionCell = cell;
            var mouseWorld = MouseToWorld();
            if (_world.TryGetBuildingAtWorldPosition(mouseWorld, out var hoveredCellByBounds, out _))
            {
                interactionCell = hoveredCellByBounds;
            }
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
                    ? WorldGridSystem.WorldToCell(player.transform.position)
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
                    if (_world.TryGetBuildingType(interactionCell, out var hoveredType) && hoveredType != BuildingType.None)
                    {
                        _openedBuildingCell = interactionCell;
                        _openedBuildingType = hoveredType;
                        _showBuildingWindow = true;
                        _hint = $"Открыто меню: {hoveredType}";
                    }
                    else
                    {
                        _hint = "Свободная рука: выбери объект для строительства";
                    }
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
                if (_world.TryRemove(interactionCell, out var removedType) && removedType != BuildingType.None)
                {
                    AddCraftedItem(removedType, 1);
                    _hint = $"Возвращено в карман: {removedType}";
                }
            }

            TryAdvanceGoals();
        }
    }
}
