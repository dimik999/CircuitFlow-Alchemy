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

            if (_showBuildingWindow)
            {
                DrawBuildingWindow(sw, sh);
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

            if (StyledButton(new Rect(hud.x + hud.width - 132, hud.y + 112, 120, 34), "Craft (K)", "open_craft_window", UiTheme.Button))
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
            string progressLine = _goals != null && _currentGoalIndex >= _goals.Length
                ? $"Прогресс кампании: {_goals.Length}/{_goals.Length} — Акт 1 закрыт"
                : $"Прогресс кампании: {Mathf.Min(_currentGoalIndex + 1, _goals?.Length ?? 0)}/{_goals?.Length ?? 0}";
            GUI.Label(new Rect(hud.x + 12, hud.y + 100, hud.width - 24, 20), progressLine, UiTheme.Label);
            if (_actOneFinaleComplete)
            {
                GUI.Label(new Rect(hud.x + 12, hud.y + 124, hud.width - 24, 40),
                    "Финал: «Кольцо первой цепи». Награда: заявка на Восточный карьер (контент Акта 2).",
                    UiTheme.Label);
            }
        }

        private void DrawUpgradesTab(Rect hud)
        {
            GUI.Label(new Rect(hud.x + 12, hud.y + 48, hud.width - 24, 20), UpgradeText(), UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 74, hud.width - 24, 20), "Покупка и подробный просмотр апгрейдов доступны только в окне Market.", UiTheme.Label);
            GUI.Label(new Rect(hud.x + 12, hud.y + 100, hud.width - 24, 20), "Открой Market свободной рукой (клик по постройке).", UiTheme.Label);
        }

        private void DrawHudCraftQueueControls(Rect hud)
        {
            var panel = new Rect(hud.x + hud.width - 316, hud.y + 8, 304, 40);
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
            if (StyledButton(new Rect(panel.x + 186, panel.y + 16, 28, 20), "↑", "hud_q_up", UiTheme.Button)) MoveSelectedCraftQueueItem(-1);
            if (StyledButton(new Rect(panel.x + 218, panel.y + 16, 28, 20), "↓", "hud_q_down", UiTheme.Button)) MoveSelectedCraftQueueItem(1);
            if (StyledButton(new Rect(panel.x + 250, panel.y + 16, 48, 20), "Top", "hud_q_top", UiTheme.Button)) MoveSelectedCraftQueueToFront();
            GUI.enabled = true;
        }

        private void DrawBuildingWindow(float sw, float sh)
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.58f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = prev;

            const float w = 520f;
            const float h = 360f;
            var panel = new Rect((sw - w) * 0.5f, (sh - h) * 0.5f, w, h);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(panel.x + 14, panel.y + 10, panel.width - 28, 24), $"Постройка: {_openedBuildingType}", UiTheme.Title);

            if (!_world.TryGetBuildingType(_openedBuildingCell, out var currentType) || currentType != _openedBuildingType)
            {
                GUI.Label(new Rect(panel.x + 14, panel.y + 44, panel.width - 28, 24), "Постройка больше не существует.", UiTheme.Label);
            }
            else if (currentType == BuildingType.Storage)
            {
                DrawStorageContents(panel, _openedBuildingCell);
            }
            else if (currentType == BuildingType.Mixer)
            {
                DrawMixerContents(panel, _openedBuildingCell);
            }
            else
            {
                DrawSpecializedBuildingInfo(panel, _openedBuildingCell, currentType);
            }

            if (StyledButton(new Rect(panel.x + panel.width - 132, panel.y + panel.height - 36, 120, 26), "Закрыть", "building_close", UiTheme.Button))
            {
                _showBuildingWindow = false;
            }
        }

        private void DrawStorageContents(Rect panel, Vector2Int storageCell)
        {
            if (_world.TryGetBuildingDirection(storageCell, out var sDir))
            {
                GUI.Label(new Rect(panel.x + 14, panel.y + 40, panel.width - 28, 40),
                    $"Порты (поворот R): вход {FormatDir(-sDir)} → выход {FormatDir(sDir)}", UiTheme.Label);
            }
            GUI.Label(new Rect(panel.x + 14, panel.y + 84, panel.width - 28, 22), "Содержимое хранилища (ручной забор):", UiTheme.Label);
            var contentRect = new Rect(panel.x + 14, panel.y + 110, panel.width - 28, panel.height - 156);
            GUI.Box(contentRect, GUIContent.none, UiTheme.Panel);

            var entries = _world.GetStorageSnapshot(storageCell);
            var keys = new List<string>(entries.Keys);
            keys.Sort(StringComparer.Ordinal);

            float rowH = 26f;
            float contentH = Mathf.Max(1, keys.Count) * rowH;
            float maxScroll = Mathf.Max(0f, contentH - contentRect.height + 4f);
            _storageWindowScroll.y = Mathf.Clamp(_storageWindowScroll.y, 0f, maxScroll);

            if (contentRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.ScrollWheel)
            {
                _storageWindowScroll.y = Mathf.Clamp(_storageWindowScroll.y + Event.current.delta.y * 14f, 0f, maxScroll);
                Event.current.Use();
            }

            if (keys.Count == 0)
            {
                GUI.Label(new Rect(contentRect.x + 8, contentRect.y + 8, contentRect.width - 16, 22), "Пусто", UiTheme.Label);
                return;
            }

            int start = Mathf.FloorToInt(_storageWindowScroll.y / rowH);
            int visible = Mathf.CeilToInt(contentRect.height / rowH) + 1;
            int end = Mathf.Min(keys.Count, start + visible);
            float yOffset = contentRect.y + 4f - (_storageWindowScroll.y - start * rowH);

            for (int i = start; i < end; i++)
            {
                string key = keys[i];
                float amount = entries[key];
                float y = yOffset + (i - start) * rowH;
                var row = new Rect(contentRect.x + 4, y, contentRect.width - 8, rowH - 2f);
                GUI.DrawTexture(new Rect(row.x + 2, row.y + 5, 16, 16), GetResourceIcon(key));
                GUI.Label(new Rect(row.x + 24, row.y + 3, 190, 20), key, UiTheme.Label);
                GUI.Label(new Rect(row.x + 220, row.y + 3, 90, 20), $"{amount:0.##}", UiTheme.Label);

                if (StyledButton(new Rect(row.x + row.width - 124, row.y + 2, 56, 22), "+1", $"stor_take1_{i}", UiTheme.Button))
                {
                    if (_world.WithdrawFromStorage(storageCell, key, 1f))
                    {
                        _hint = $"Взято из хранилища: {key} x1";
                    }
                }
                if (StyledButton(new Rect(row.x + row.width - 62, row.y + 2, 56, 22), "Все", $"stor_takeall_{i}", UiTheme.Button))
                {
                    if (_world.WithdrawFromStorage(storageCell, key, amount))
                    {
                        _hint = $"Взято из хранилища: {key} x{amount:0.##}";
                    }
                }
            }

            if (maxScroll > 0.001f)
            {
                float thumbH = Mathf.Max(24f, contentRect.height * contentRect.height / (contentH + 0.01f));
                float thumbY = contentRect.y + (_storageWindowScroll.y / maxScroll) * (contentRect.height - thumbH);
                GUI.DrawTexture(new Rect(contentRect.x + contentRect.width - 8, thumbY, 6, thumbH), Texture2D.whiteTexture);
            }
        }

        private void DrawMixerContents(Rect panel, Vector2Int mixerCell)
        {
            GUI.Label(new Rect(panel.x + 14, panel.y + 44, panel.width - 28, 22), "Входные ресурсы -> Выход", UiTheme.Label);
            var leftRect = new Rect(panel.x + 14, panel.y + 70, (panel.width - 52) * 0.45f, panel.height - 116);
            var rightRect = new Rect(panel.x + 28 + leftRect.width, panel.y + 70, (panel.width - 52) * 0.45f, panel.height - 116);
            GUI.Box(leftRect, GUIContent.none, UiTheme.Panel);
            GUI.Box(rightRect, GUIContent.none, UiTheme.Panel);

            var inputs = _world.GetMixerInputSnapshot(mixerCell);
            var inputKeys = new List<string>(inputs.Keys);
            inputKeys.Sort(StringComparer.Ordinal);
            if (inputKeys.Count == 0)
            {
                GUI.Label(new Rect(leftRect.x + 8, leftRect.y + 8, leftRect.width - 16, 22), "Нет входов", UiTheme.Label);
            }
            else
            {
                for (int i = 0; i < inputKeys.Count && i < 8; i++)
                {
                    string key = inputKeys[i];
                    float y = leftRect.y + 6 + i * 24;
                    GUI.DrawTexture(new Rect(leftRect.x + 6, y + 3, 16, 16), GetResourceIcon(key));
                    GUI.Label(new Rect(leftRect.x + 26, y, leftRect.width - 32, 20), $"{key}: {inputs[key]:0.##}", UiTheme.Label);
                }
            }

            if (_world.TryGetBuildingOutput(mixerCell, out var outKey, out var outAmount))
            {
                GUI.DrawTexture(new Rect(rightRect.x + 6, rightRect.y + 9, 16, 16), GetResourceIcon(outKey));
                GUI.Label(new Rect(rightRect.x + 26, rightRect.y + 6, rightRect.width - 32, 22), $"{outKey}: {outAmount:0.##}", UiTheme.Label);
            }
            else
            {
                GUI.Label(new Rect(rightRect.x + 8, rightRect.y + 8, rightRect.width - 16, 22), "Выход пуст", UiTheme.Label);
            }
        }

        private void DrawSpecializedBuildingInfo(Rect panel, Vector2Int cell, BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Extractor:
                    DrawExtractorInfo(panel, cell);
                    return;
                case BuildingType.Pipe:
                    DrawPipeInfo(panel, cell);
                    return;
                case BuildingType.PipeCorner:
                    DrawPipeCornerInfo(panel, cell);
                    return;
                case BuildingType.PipeConnector:
                    DrawConnectorInfo(panel, cell);
                    return;
                case BuildingType.PipeSplitter:
                    DrawSplitterInfo(panel, cell);
                    return;
                case BuildingType.Generator:
                    DrawGeneratorInfo(panel, cell);
                    return;
                case BuildingType.PowerPole:
                    DrawPowerPoleInfo(panel, cell);
                    return;
                case BuildingType.MarketTerminal:
                    DrawMarketInfo(panel, cell);
                    return;
                default:
                    DrawDefaultBuildingInfo(panel, cell, type);
                    return;
            }
        }

        private void DrawExtractorInfo(Rect panel, Vector2Int cell)
        {
            GUI.Label(new Rect(panel.x + 14, panel.y + 44, panel.width - 28, 24), "Экстрактор", UiTheme.Label);
            DrawPoweredLine(panel, cell, 70f);
            DrawDirectionLine(panel, cell, 96f, "Выход");
            DrawOutputBufferLine(panel, cell, 122f);
        }

        private void DrawPipeInfo(Rect panel, Vector2Int cell)
        {
            GUI.Label(new Rect(panel.x + 14, panel.y + 44, panel.width - 28, 24), "Труба", UiTheme.Label);
            DrawDirectionLine(panel, cell, 70f, "Приоритет");
            if (_world.TryGetPipeConnectionMask(cell, out int mask))
            {
                GUI.Label(new Rect(panel.x + 14, panel.y + 96, panel.width - 28, 24), $"Подключения (маска): {mask}", UiTheme.Label);
            }
            DrawOutputBufferLine(panel, cell, 122f);
            GUI.Label(new Rect(panel.x + 14, panel.y + 148, panel.width - 28, 24), "Поток определяется по фактическим подключениям.", UiTheme.Label);
        }

        private void DrawPipeCornerInfo(Rect panel, Vector2Int cell)
        {
            GUI.Label(new Rect(panel.x + 14, panel.y + 44, panel.width - 28, 24), "PipeCorner", UiTheme.Label);
            DrawDirectionLine(panel, cell, 70f, "Выход");
            DrawOutputBufferLine(panel, cell, 96f);
            GUI.Label(new Rect(panel.x + 14, panel.y + 122, panel.width - 28, 24), "Угол двухпортовый: поток идет между двумя портами.", UiTheme.Label);

            var schemeRect = new Rect(panel.x + 14, panel.y + 150, panel.width - 28, 50);
            GUI.Box(schemeRect, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(schemeRect.x + 8, schemeRect.y + 6, schemeRect.width - 16, 18), "Схема потока (текущий поворот):", UiTheme.Label);
            GUI.Label(new Rect(schemeRect.x + 8, schemeRect.y + 24, schemeRect.width - 16, 20), GetPipeCornerFlowText(cell), UiTheme.Label);
            GUI.Label(new Rect(panel.x + 14, panel.y + 206, panel.width - 28, 22), $"Последний вход: {GetPipeCornerLastInputText(cell)}", UiTheme.Label);
        }

        private string GetPipeCornerFlowText(Vector2Int cell)
        {
            if (!_world.TryGetBuildingDirection(cell, out var outDir))
            {
                return "Порты: - <-> -";
            }

            if (outDir == Vector2Int.zero)
            {
                outDir = Vector2Int.right;
            }

            var sideB = new Vector2Int(outDir.y, -outDir.x);
            return $"Порты: {FormatDir(outDir)} <-> {FormatDir(sideB)}";
        }

        private string GetPipeCornerLastInputText(Vector2Int cell)
        {
            if (!_world.TryGetBuildingLastIncomingDirection(cell, out var dir) || dir == Vector2Int.zero)
            {
                return "-";
            }

            return FormatDir(dir);
        }

        private void DrawConnectorInfo(Rect panel, Vector2Int cell)
        {
            GUI.Label(new Rect(panel.x + 14, panel.y + 44, panel.width - 28, 24), "Соединитель труб", UiTheme.Label);
            DrawDirectionLine(panel, cell, 70f, "Выход");
            DrawOutputBufferLine(panel, cell, 96f);
            GUI.Label(new Rect(panel.x + 14, panel.y + 122, panel.width - 28, 24), "Входы: все стороны кроме стороны выхода.", UiTheme.Label);
        }

        private void DrawSplitterInfo(Rect panel, Vector2Int cell)
        {
            GUI.Label(new Rect(panel.x + 14, panel.y + 44, panel.width - 28, 24), "Разделитель труб", UiTheme.Label);
            DrawDirectionLine(panel, cell, 70f, "Основной выход");
            DrawOutputBufferLine(panel, cell, 96f);
            GUI.Label(new Rect(panel.x + 14, panel.y + 122, panel.width - 28, 24), "Вход: только с обратной стороны.", UiTheme.Label);
            GUI.Label(new Rect(panel.x + 14, panel.y + 146, panel.width - 28, 24), "Выход: вперед + 2 боковых стороны.", UiTheme.Label);
        }

        private void DrawGeneratorInfo(Rect panel, Vector2Int cell)
        {
            GUI.Label(new Rect(panel.x + 14, panel.y + 44, panel.width - 28, 24), "Генератор", UiTheme.Label);
            DrawPoweredLine(panel, cell, 70f);
            GUI.Label(new Rect(panel.x + 14, panel.y + 96, panel.width - 28, 24), "Источник питания для ближайшей сети.", UiTheme.Label);
            DrawOutputBufferLine(panel, cell, 122f);
        }

        private void DrawPowerPoleInfo(Rect panel, Vector2Int cell)
        {
            GUI.Label(new Rect(panel.x + 14, panel.y + 44, panel.width - 28, 24), "Энергетический столб", UiTheme.Label);
            DrawPoweredLine(panel, cell, 70f);
            GUI.Label(new Rect(panel.x + 14, panel.y + 96, panel.width - 28, 24), "Передает энергию в радиусе сети.", UiTheme.Label);
            GUI.Label(new Rect(panel.x + 14, panel.y + 122, panel.width - 28, 24), "Поворот визуально отключен (фиксированная ориентация).", UiTheme.Label);
        }

        private void DrawMarketInfo(Rect panel, Vector2Int cell)
        {
            GUI.Label(new Rect(panel.x + 14, panel.y + 44, panel.width - 28, 24), "Рынок", UiTheme.Label);
            DrawPoweredLine(panel, cell, 70f);
            GUI.Label(new Rect(panel.x + 14, panel.y + 96, panel.width - 28, 20), "Центр апгрейдов (покупка за ресурсы инвентаря):", UiTheme.Label);

            DrawMarketUpgradeRow(panel, 0, "Добыча", _upgExtractor, 6, GetExtractorUpgradeCost(), "market_upg_extractor", TryBuyUpgradeExtractor);
            DrawMarketUpgradeRow(panel, 1, "Смеситель", _upgMixer, 8, GetMixerUpgradeCost(), "market_upg_mixer", TryBuyUpgradeMixer);
            DrawMarketUpgradeRow(panel, 2, "Энергосеть", _upgPower, 4, GetPowerUpgradeCost(), "market_upg_power", TryBuyUpgradePower);

            GUI.Label(new Rect(panel.x + 14, panel.y + panel.height - 88, panel.width - 28, 22), "Принимает ресурсный поток по трубам.", UiTheme.Label);
        }

        private void DrawMarketUpgradeRow(Rect panel, int rowIndex, string title, int level, int maxLevel, ResourceCost[] cost, string id, Action buyAction)
        {
            float y = panel.y + 122 + rowIndex * 58f;
            bool canBuy = level < maxLevel && HasUpgradeResources(cost);
            string levelText = $"Lv {level}/{maxLevel}";
            string costText = level >= maxLevel ? "Достигнут максимум" : $"Цена: {FormatUpgradeCost(cost)}";

            GUI.Label(new Rect(panel.x + 14, y, 138, 22), $"{title} ({levelText})", UiTheme.Label);
            GUI.Label(new Rect(panel.x + 154, y, panel.width - 292, 22), costText, UiTheme.Label);

            GUI.enabled = canBuy;
            if (StyledButton(new Rect(panel.x + panel.width - 132, y - 1, 120, 24), "Купить", id, UiTheme.Button))
            {
                buyAction?.Invoke();
            }
            GUI.enabled = true;
        }

        private void DrawDefaultBuildingInfo(Rect panel, Vector2Int cell, BuildingType type)
        {
            GUI.Label(new Rect(panel.x + 14, panel.y + 44, panel.width - 28, 24), "Панель мониторинга постройки", UiTheme.Label);
            GUI.Label(new Rect(panel.x + 14, panel.y + 70, panel.width - 28, 24), $"Тип: {type}", UiTheme.Label);
            DrawOutputBufferLine(panel, cell, 96f);
        }

        private void DrawPoweredLine(Rect panel, Vector2Int cell, float yOffset)
        {
            bool powered = _world.IsBuildingPoweredAt(cell);
            GUI.Label(new Rect(panel.x + 14, panel.y + yOffset, panel.width - 28, 24), $"Питание: {(powered ? "Есть" : "Нет")}", UiTheme.Label);
        }

        private void DrawDirectionLine(Rect panel, Vector2Int cell, float yOffset, string caption)
        {
            if (_world.TryGetBuildingDirection(cell, out var dir))
            {
                GUI.Label(new Rect(panel.x + 14, panel.y + yOffset, panel.width - 28, 24), $"{caption}: {FormatDir(dir)}", UiTheme.Label);
            }
            else
            {
                GUI.Label(new Rect(panel.x + 14, panel.y + yOffset, panel.width - 28, 24), $"{caption}: -", UiTheme.Label);
            }
        }

        private void DrawOutputBufferLine(Rect panel, Vector2Int cell, float yOffset)
        {
            if (_world.TryGetBuildingOutput(cell, out var outKey, out var outAmount))
            {
                GUI.DrawTexture(new Rect(panel.x + 14, panel.y + yOffset + 3, 16, 16), GetResourceIcon(outKey));
                GUI.Label(new Rect(panel.x + 36, panel.y + yOffset, panel.width - 42, 24), $"Текущий буфер: {outKey} {outAmount:0.##}", UiTheme.Label);
            }
            else
            {
                GUI.Label(new Rect(panel.x + 14, panel.y + yOffset, panel.width - 28, 24), "Текущий буфер: пуст", UiTheme.Label);
            }
        }

        private static string FormatDir(Vector2Int dir)
        {
            if (dir == Vector2Int.up) return "Вверх";
            if (dir == Vector2Int.right) return "Вправо";
            if (dir == Vector2Int.down) return "Вниз";
            if (dir == Vector2Int.left) return "Влево";
            return "-";
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
            var h = 272f;
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
            float zoomSpeed = GUI.HorizontalSlider(new Rect(panel.x + 170, panel.y + 148, panel.width - 186, 18), GameSettings.CameraZoomSpeed, 4f, 32f);
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

            bool showFlow = GUI.Toggle(new Rect(panel.x + 16, panel.y + 200, 260, 24), GameSettings.ShowFlowIndicators, "Показывать индикаторы потока");
            if (showFlow != GameSettings.ShowFlowIndicators)
            {
                GameSettings.SetShowFlowIndicators(showFlow);
                GameSettings.Save();
                _world?.RefreshFlowIndicators();
            }
            _world?.SetFlowIndicatorsVisible(GameSettings.ShowFlowIndicators);

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
            float h = 460f;
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
            DrawCraftRow(panel, 1, BuildingType.PipeCorner, "PipeCorner");
            DrawCraftRow(panel, 2, BuildingType.PipeConnector, "Соединитель");
            DrawCraftRow(panel, 3, BuildingType.PipeSplitter, "Разделитель");
            DrawCraftRow(panel, 4, BuildingType.Extractor, "Экстрактор");
            DrawCraftRow(panel, 5, BuildingType.Storage, "Хранилище");
            DrawCraftRow(panel, 6, BuildingType.Mixer, "Смеситель");
            DrawCraftRow(panel, 7, BuildingType.Generator, "Генератор");
            DrawCraftRow(panel, 8, BuildingType.PowerPole, "Столб");
            DrawCraftRow(panel, 9, BuildingType.MarketTerminal, "Рынок");

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
            float y = panel.y + 96 + row * 30;
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
    }
}
