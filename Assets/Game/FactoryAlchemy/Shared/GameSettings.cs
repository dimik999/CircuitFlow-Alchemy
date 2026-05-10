using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public static class GameSettings
    {
        private const string KeyMasterVolume = "CFA_SETTINGS_MASTER_VOLUME";
        private const string KeyUiScale = "CFA_SETTINGS_UI_SCALE";
        private const string KeyShowHints = "CFA_SETTINGS_SHOW_HINTS";
        private const string KeyCameraZoomSpeed = "CFA_SETTINGS_CAMERA_ZOOM_SPEED";
        private const string KeyShowFlowIndicators = "CFA_SETTINGS_SHOW_FLOW_INDICATORS";

        public static float MasterVolume { get; private set; } = 0.8f;
        public static float UiScale { get; private set; } = 1f;
        public static bool ShowHints { get; private set; } = true;
        public static float CameraZoomSpeed { get; private set; } = 12f;
        public static bool ShowFlowIndicators { get; private set; } = true;

        static GameSettings()
        {
            Load();
            Apply();
        }

        public static void Load()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMasterVolume, 0.8f));
            UiScale = Mathf.Clamp(PlayerPrefs.GetFloat(KeyUiScale, 1f), 0.8f, 1.4f);
            ShowHints = PlayerPrefs.GetInt(KeyShowHints, 1) == 1;
            CameraZoomSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(KeyCameraZoomSpeed, 12f), 4f, 32f);
            ShowFlowIndicators = PlayerPrefs.GetInt(KeyShowFlowIndicators, 1) == 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
            PlayerPrefs.SetFloat(KeyUiScale, UiScale);
            PlayerPrefs.SetInt(KeyShowHints, ShowHints ? 1 : 0);
            PlayerPrefs.SetFloat(KeyCameraZoomSpeed, CameraZoomSpeed);
            PlayerPrefs.SetInt(KeyShowFlowIndicators, ShowFlowIndicators ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void Apply()
        {
            AudioListener.volume = MasterVolume;
        }

        public static void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            Apply();
        }

        public static void SetUiScale(float value)
        {
            UiScale = Mathf.Clamp(value, 0.8f, 1.4f);
        }

        public static void SetShowHints(bool value)
        {
            ShowHints = value;
        }

        public static void SetCameraZoomSpeed(float value)
        {
            CameraZoomSpeed = Mathf.Clamp(value, 4f, 32f);
        }

        public static void SetShowFlowIndicators(bool value)
        {
            ShowFlowIndicators = value;
        }
    }
}
