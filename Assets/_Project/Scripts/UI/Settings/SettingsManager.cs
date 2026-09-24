using System;
using UnityEngine;

namespace Robot.UI.Settings
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public const string KeyMasterVol = "Settings_MasterVol";
        public const string KeyMusicVol = "Settings_MusicVol";
        public const string KeySFXVol = "Settings_SFXVol";
        public const string KeySensitivity = "Settings_Sensitivity";
        public const string KeyLanguage = "Settings_Language";

        public float MasterVolume { get; private set; } = 1.0f;
        public float MusicVolume { get; private set; } = 0.8f;
        public float SFXVolume { get; private set; } = 1.0f;
        public float Sensitivity { get; private set; } = 1.0f;
        public string Language { get; private set; } = "Türkçe";

        public event Action SettingsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void LoadSettings()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMasterVol, 1.0f));
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusicVol, 0.8f));
            SFXVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySFXVol, 1.0f));
            Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(KeySensitivity, 1.0f), .2f, 3f);
            Language = PlayerPrefs.GetString(KeyLanguage, "Türkçe");

            ApplySettings();
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyMasterVol, MasterVolume);
            ApplySettings();
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyMusicVol, MusicVolume);
            ApplySettings();
        }

        public void SetSFXVolume(float value)
        {
            SFXVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeySFXVol, SFXVolume);
            ApplySettings();
        }

        public void SetSensitivity(float value)
        {
            Sensitivity = Mathf.Clamp(value, 0.2f, 3.0f);
            PlayerPrefs.SetFloat(KeySensitivity, Sensitivity);
            ApplySettings();
        }

        public void SetLanguage(string lang)
        {
            Language = lang;
            PlayerPrefs.SetString(KeyLanguage, Language);
            ApplySettings();
        }

        private void ApplySettings()
        {
            AudioListener.volume = MasterVolume;
            PlayerPrefs.Save();
            SettingsChanged?.Invoke();
        }
    }
}
