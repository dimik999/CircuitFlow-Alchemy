using System;
using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public partial class BuildingInputController
    {
        private Vector2 _referenceScrollPos;

        private void DrawReferenceWindow(float sw, float sh)
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = prev;

            const float w = 620f;
            const float h = 500f;
            var panel = new Rect((sw - w) * 0.5f, (sh - h) * 0.5f, w, h);

            if (panel.Contains(Event.current.mousePosition) && Event.current.type == EventType.ScrollWheel)
            {
                Event.current.Use();
            }

            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(panel.x + 14, panel.y + 10, panel.width - 28, 26), "Справочник производства", UiTheme.Title);

            float tabY = panel.y + 40f;
            float tabX = panel.x + 12f;
            const float tabW = 112f;
            const float tabGap = 6f;
            var tabs = (ReferenceTab[])Enum.GetValues(typeof(ReferenceTab));
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                bool active = _referenceTab == tab;
                if (StyledButton(new Rect(tabX + (tabW + tabGap) * i, tabY, tabW, 26),
                        GameReferenceCatalog.GetTabTitle(tab),
                        $"ref_tab_{tab}",
                        active ? UiTheme.Button : UiTheme.TabButton))
                {
                    _referenceTab = tab;
                    _referenceScrollPos = Vector2.zero;
                }
            }

            var contentRect = new Rect(panel.x + 14, panel.y + 76, panel.width - 28, panel.height - 124);
            float innerWidth = contentRect.width - 22f;
            float contentHeight = MeasureReferenceContentHeight(_referenceTab, innerWidth);

            _referenceScrollPos = GUI.BeginScrollView(
                contentRect,
                _referenceScrollPos,
                new Rect(0f, 0f, innerWidth, contentHeight),
                false,
                contentHeight > contentRect.height + 1f);

            float y = 0f;
            switch (_referenceTab)
            {
                case ReferenceTab.Essences:
                    y = DrawEssencesReference(0f, innerWidth, y);
                    break;
                case ReferenceTab.Mixer:
                    y = DrawMixerReference(0f, innerWidth, y);
                    break;
                case ReferenceTab.Buildings:
                    y = DrawBuildingsReference(0f, innerWidth, y);
                    break;
                case ReferenceTab.Logistics:
                    y = DrawNotesReference(0f, innerWidth, y, GameReferenceCatalog.LogisticsNotes);
                    break;
                case ReferenceTab.Power:
                    y = DrawNotesReference(0f, innerWidth, y, GameReferenceCatalog.PowerNotes);
                    break;
            }

            GUI.EndScrollView();

            string mixerHint = _world != null
                ? $"Текущий множитель смесителя: ×{_world.MixerOutputMultiplier:0.##}"
                : string.Empty;
            if (!string.IsNullOrEmpty(mixerHint) && _referenceTab == ReferenceTab.Mixer)
            {
                GUI.Label(new Rect(panel.x + 14, panel.y + panel.height - 58, panel.width - 150, 20), mixerHint, UiTheme.Label);
            }

            if (StyledButton(new Rect(panel.x + panel.width - 132, panel.y + panel.height - 38, 120, 28), "Закрыть (H)", "reference_close", UiTheme.Button))
            {
                _showReferenceWindow = false;
            }
        }

        private static float WrappedHeight(string text, float width, GUIStyle style, float spacingAfter = 4f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return spacingAfter;
            }

            return style.CalcHeight(new GUIContent(text), width) + spacingAfter;
        }

        private static float DrawWrapped(float x, float y, float width, string text, GUIStyle style, float spacingAfter = 4f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return y + spacingAfter;
            }

            float h = style.CalcHeight(new GUIContent(text), width);
            GUI.Label(new Rect(x, y, width, h), text, style);
            return y + h + spacingAfter;
        }

        private float MeasureReferenceContentHeight(ReferenceTab tab, float width)
        {
            switch (tab)
            {
                case ReferenceTab.Essences:
                    return MeasureEssencesHeight(width);
                case ReferenceTab.Mixer:
                    return MeasureMixerHeight(width);
                case ReferenceTab.Buildings:
                    return MeasureBuildingsHeight(width);
                case ReferenceTab.Logistics:
                    return MeasureNotesHeight(GameReferenceCatalog.LogisticsNotes, width);
                case ReferenceTab.Power:
                    return MeasureNotesHeight(GameReferenceCatalog.PowerNotes, width);
                default:
                    return 200f;
            }
        }

        private static float MeasureEssencesHeight(float width)
        {
            float h = WrappedHeight("Базовые эссенции и способы получения:", width, UiTheme.Label, 8f);
            var essences = GameReferenceCatalog.Essences;
            for (int i = 0; i < essences.Count; i++)
            {
                var e = essences[i];
                string status = e.InCurrentBuild ? string.Empty : "  [скоро]";
                h += 24f;
                h += WrappedHeight(e.Description, width - 8f, UiTheme.WrapLabel);
                h += WrappedHeight("Получение: " + e.HowToObtain, width - 8f, UiTheme.WrapLabel, 10f);
            }

            return h + 8f;
        }

        private static float MeasureMixerHeight(float width)
        {
            float h = WrappedHeight("Рецепты смесителя (автоматическое производство):", width, UiTheme.Label, 8f);
            var recipes = GameReferenceCatalog.MixerRecipes;
            for (int i = 0; i < recipes.Count; i++)
            {
                var r = recipes[i];
                h += 24f;
                h += WrappedHeight("Вход: " + GameReferenceCatalog.FormatCostList(r.Inputs), width - 8f, UiTheme.WrapLabel);
                h += WrappedHeight($"Выход: {r.Output} ×{r.OutputAmount:0.##} за цикл", width - 8f, UiTheme.WrapLabel);
                h += WrappedHeight(r.Conditions, width - 8f, UiTheme.WrapLabel);
                h += WrappedHeight(r.Ports, width - 8f, UiTheme.WrapLabel, 10f);
            }

            h += WrappedHeight(
                "Подсказка: откройте смеситель ПКМ в мире, чтобы увидеть текущие буферы входа и выхода.",
                width,
                UiTheme.WrapLabel,
                8f);
            return h + 8f;
        }

        private static float MeasureBuildingsHeight(float width)
        {
            float h = WrappedHeight("Крафт построек (окно K) — стоимость из инвентаря:", width, UiTheme.Label, 8f);
            var buildings = GameReferenceCatalog.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                h += 24f;
                h += WrappedHeight(b.Role + "  |  " + b.UnlockHint, width - 8f, UiTheme.WrapLabel, 8f);
            }

            return h + 8f;
        }

        private static float MeasureNotesHeight(System.Collections.Generic.IReadOnlyList<ReferenceNote> notes, float width)
        {
            float h = 8f;
            for (int i = 0; i < notes.Count; i++)
            {
                h += 24f;
                for (int j = 0; j < notes[i].Lines.Length; j++)
                {
                    h += WrappedHeight("• " + notes[i].Lines[j], width - 8f, UiTheme.WrapLabel);
                }

                h += 6f;
            }

            return h + 8f;
        }

        private float DrawEssencesReference(float x, float width, float y)
        {
            y = DrawWrapped(x, y, width, "Базовые эссенции и способы получения:", UiTheme.Label, 8f);

            var essences = GameReferenceCatalog.Essences;
            for (int i = 0; i < essences.Count; i++)
            {
                var e = essences[i];
                string status = e.InCurrentBuild ? string.Empty : "  [скоро]";
                GUI.Label(new Rect(x, y, width, 22), e.Title + status, UiTheme.Title);
                y += 24f;
                y = DrawWrapped(x + 8f, y, width - 8f, e.Description, UiTheme.WrapLabel);
                y = DrawWrapped(x + 8f, y, width - 8f, "Получение: " + e.HowToObtain, UiTheme.WrapLabel, 10f);
            }

            return y;
        }

        private float DrawMixerReference(float x, float width, float y)
        {
            y = DrawWrapped(x, y, width, "Рецепты смесителя (автоматическое производство):", UiTheme.Label, 8f);

            var recipes = GameReferenceCatalog.MixerRecipes;
            for (int i = 0; i < recipes.Count; i++)
            {
                var r = recipes[i];
                GUI.Label(new Rect(x, y, width, 22), r.Name, UiTheme.Title);
                y += 24f;

                y = DrawWrapped(x + 8f, y, width - 8f, "Вход: " + GameReferenceCatalog.FormatCostList(r.Inputs), UiTheme.WrapLabel);
                y = DrawWrapped(x + 8f, y, width - 8f, $"Выход: {r.Output} ×{r.OutputAmount:0.##} за цикл", UiTheme.WrapLabel);
                y = DrawWrapped(x + 8f, y, width - 8f, r.Conditions, UiTheme.WrapLabel);
                y = DrawWrapped(x + 8f, y, width - 8f, r.Ports, UiTheme.WrapLabel, 10f);
            }

            y = DrawWrapped(
                x,
                y,
                width,
                "Подсказка: откройте смеситель ПКМ в мире, чтобы увидеть текущие буферы входа и выхода.",
                UiTheme.WrapLabel,
                8f);
            return y;
        }

        private float DrawBuildingsReference(float x, float width, float y)
        {
            y = DrawWrapped(x, y, width, "Крафт построек (окно K) — стоимость из инвентаря:", UiTheme.Label, 8f);

            var buildings = GameReferenceCatalog.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                GUI.Label(new Rect(x, y, width * 0.42f, 22), b.Title, UiTheme.Title);
                GUI.Label(new Rect(x + width * 0.44f, y, width * 0.56f, 22),
                    GameReferenceCatalog.FormatCostList(b.CraftCost),
                    UiTheme.WrapLabel);
                y += 24f;
                y = DrawWrapped(x + 8f, y, width - 8f, b.Role + "  |  " + b.UnlockHint, UiTheme.WrapLabel, 8f);
            }

            return y;
        }

        private static float DrawNotesReference(float x, float width, float y, System.Collections.Generic.IReadOnlyList<ReferenceNote> notes)
        {
            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                GUI.Label(new Rect(x, y, width, 22), note.Title, UiTheme.Title);
                y += 24f;
                for (int j = 0; j < note.Lines.Length; j++)
                {
                    y = DrawWrapped(x + 8f, y, width - 8f, "• " + note.Lines[j], UiTheme.WrapLabel);
                }

                y += 6f;
            }

            return y;
        }

        public void OpenReferenceWindow(ReferenceTab tab)
        {
            _referenceTab = tab;
            _referenceScrollPos = Vector2.zero;
            _showReferenceWindow = true;
            _showCraftWindow = false;
            _showInventoryWindow = false;
            _showBuildingWindow = false;
            _isPauseMenuOpen = false;
            _showSettings = false;
        }
    }
}
