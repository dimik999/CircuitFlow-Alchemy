using UnityEngine;

namespace CircuitFlowAlchemy.Prototype.FactorioLite
{
    public static class UiTheme
    {
        private static GUIStyle _panel;
        private static GUIStyle _title;
        private static GUIStyle _label;
        private static GUIStyle _button;
        private static GUIStyle _tabButton;
        private static Texture2D _bg;
        private static Texture2D _btnNormal;
        private static Texture2D _btnHover;

        public static GUIStyle Panel
        {
            get
            {
                EnsureInitialized();
                return _panel;
            }
        }

        public static GUIStyle Title
        {
            get
            {
                EnsureInitialized();
                return _title;
            }
        }

        public static GUIStyle Label
        {
            get
            {
                EnsureInitialized();
                return _label;
            }
        }

        public static GUIStyle Button
        {
            get
            {
                EnsureInitialized();
                return _button;
            }
        }

        public static GUIStyle TabButton
        {
            get
            {
                EnsureInitialized();
                return _tabButton;
            }
        }

        private static void EnsureInitialized()
        {
            if (_panel != null)
            {
                return;
            }

            _bg = new Texture2D(1, 1);
            _bg.SetPixel(0, 0, new Color(0.09f, 0.11f, 0.14f, 0.9f));
            _bg.Apply();

            _panel = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _bg, textColor = new Color(0.87f, 0.92f, 1f) },
                fontSize = 14,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(12, 12, 10, 10)
            };

            _title = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(0.92f, 0.97f, 1f) },
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _label = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(0.87f, 0.92f, 1f) },
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };

            _btnNormal = new Texture2D(1, 1);
            _btnNormal.SetPixel(0, 0, new Color(0.18f, 0.24f, 0.33f, 0.95f));
            _btnNormal.Apply();
            _btnHover = new Texture2D(1, 1);
            _btnHover.SetPixel(0, 0, new Color(0.25f, 0.33f, 0.45f, 0.98f));
            _btnHover.Apply();

            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = _btnNormal, textColor = new Color(0.93f, 0.97f, 1f) },
                hover = { background = _btnHover, textColor = Color.white },
                active = { background = _btnHover, textColor = Color.white },
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(8, 8, 6, 6)
            };

            _tabButton = new GUIStyle(_button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal
            };
        }
    }
}
