using UnityEngine;

namespace CircuitFlowAlchemy.Prototype.FactorioLite
{
    public class MainMenuSceneController : MonoBehaviour
    {
        private bool _showSettings;
        private string _lastHoveredId;
        private AudioSource _audioSource;
        private AudioClip _hoverClip;
        private AudioClip _clickClip;

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = 0.4f;
            _hoverClip = CreateToneClip(900f, 0.04f, 0.12f);
            _clickClip = CreateToneClip(660f, 0.06f, 0.18f);
        }

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * GameSettings.UiScale);
            float invScale = 1f / GameSettings.UiScale;
            var sw = Screen.width * invScale;
            var sh = Screen.height * invScale;

            const float panelW = 460f;
            const float panelH = 390f;
            var panel = new Rect((sw - panelW) * 0.5f, (sh - panelH) * 0.5f, panelW, panelH);

            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(panel.x + 20, panel.y + 20, panel.width - 40, 36), "CircuitFlow Alchemy", UiTheme.Title);
            GUI.Label(new Rect(panel.x + 20, panel.y + 56, panel.width - 40, 24), "Главное меню", UiTheme.Label);

            if (!_showSettings && StyledButton(new Rect(panel.x + 40, panel.y + 96, panel.width - 80, 42), "Новая игра", "menu_new"))
            {
                SceneFlow.OpenGameSceneNew();
            }

            if (!_showSettings && StyledButton(new Rect(panel.x + 40, panel.y + 146, panel.width - 80, 38), "Загрузить слот 1", "menu_s1"))
            {
                SceneFlow.OpenGameSceneLoad(1);
            }

            if (!_showSettings && StyledButton(new Rect(panel.x + 40, panel.y + 190, panel.width - 80, 38), "Загрузить слот 2", "menu_s2"))
            {
                SceneFlow.OpenGameSceneLoad(2);
            }

            if (!_showSettings && StyledButton(new Rect(panel.x + 40, panel.y + 234, panel.width - 80, 38), "Загрузить слот 3", "menu_s3"))
            {
                SceneFlow.OpenGameSceneLoad(3);
            }

            if (StyledButton(new Rect(panel.x + 40, panel.y + 282, panel.width - 80, 38), "Настройки", "menu_settings"))
            {
                _showSettings = !_showSettings;
            }

            if (!_showSettings && StyledButton(new Rect(panel.x + 40, panel.y + 326, panel.width - 80, 38), "Выход из игры", "menu_quit"))
            {
                Application.Quit();
            }

            if (_showSettings)
            {
                var prev = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.55f);
                GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
                GUI.color = prev;
                DrawSettings(new Rect(panel.x + 20, panel.y + 326, panel.width - 40, 54));
            }
        }

        private bool StyledButton(Rect rect, string label, string id)
        {
            bool hovered = rect.Contains(Event.current.mousePosition);
            if (hovered && _lastHoveredId != id)
            {
                _lastHoveredId = id;
                if (_audioSource != null && _hoverClip != null) _audioSource.PlayOneShot(_hoverClip, 0.7f);
            }

            bool clicked = GUI.Button(rect, label, UiTheme.Button);
            if (clicked && _audioSource != null && _clickClip != null)
            {
                _audioSource.PlayOneShot(_clickClip, 0.9f);
            }

            return clicked;
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

            var clip = AudioClip.Create($"ui_menu_{frequency}", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void DrawSettings(Rect area)
        {
            GUI.Box(area, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(area.x + 10, area.y + 6, 160, 20), $"Громкость: {(int)(GameSettings.MasterVolume * 100)}%", UiTheme.Label);
            float vol = GUI.HorizontalSlider(new Rect(area.x + 170, area.y + 10, area.width - 180, 18), GameSettings.MasterVolume, 0f, 1f);
            if (Mathf.Abs(vol - GameSettings.MasterVolume) > 0.001f)
            {
                GameSettings.SetMasterVolume(vol);
                GameSettings.Save();
            }

            GUI.Label(new Rect(area.x + 10, area.y + 30, 160, 20), $"Масштаб UI: {GameSettings.UiScale:0.00}", UiTheme.Label);
            float scale = GUI.HorizontalSlider(new Rect(area.x + 170, area.y + 34, area.width - 180, 18), GameSettings.UiScale, 0.8f, 1.4f);
            if (Mathf.Abs(scale - GameSettings.UiScale) > 0.001f)
            {
                GameSettings.SetUiScale(scale);
                GameSettings.Save();
            }
        }
    }
}
