using UnityEngine;
using UnityEngine.UI;
using Robot.UI.Settings;
using Robot.Player.CameraControl;
namespace Robot.UI.Production
{
    public sealed class SettingsPanelController : MonoBehaviour
    {
        public UIStateManager state;
        public GraphicsSettingsController graphics;
        public ThirdPersonCameraController cameraController;
        public GameObject[] categories;
        public Slider master, music, sfx, sensitivity;
        public Button display, resolution, quality, vsync;
        private SettingsManager settings;
        public void Initialize(SettingsManager manager)
        {
            settings = manager;
            master.SetValueWithoutNotify(settings.MasterVolume); music.SetValueWithoutNotify(settings.MusicVolume);
            sfx.SetValueWithoutNotify(settings.SFXVolume); sensitivity.SetValueWithoutNotify(settings.Sensitivity);
            master.onValueChanged.AddListener(settings.SetMasterVolume); music.onValueChanged.AddListener(settings.SetMusicVolume);
            sfx.onValueChanged.AddListener(settings.SetSFXVolume); sensitivity.onValueChanged.AddListener(settings.SetSensitivity);
            display.onClick.AddListener(graphics.NextDisplayMode); resolution.onClick.AddListener(graphics.NextResolution);
            quality.onClick.AddListener(graphics.NextQuality); vsync.onClick.AddListener(graphics.ToggleVSync);
            graphics.Changed += RefreshGraphics; settings.SettingsChanged += ApplySensitivity;
            RefreshGraphics(); ApplySensitivity(); SelectCategory(0);
        }
        public void SelectCategory(int value) { for (int i = 0; i < categories.Length; i++) categories[i].SetActive(i == value); }
        public void OpenControls() => state.Open(UIScreen.Controls);
        private void ApplySensitivity() => cameraController?.SetSensitivityMultiplier(settings.Sensitivity);
        private void RefreshGraphics()
        {
            display.gameObject.SetActive(graphics.SupportsDisplay); resolution.gameObject.SetActive(graphics.SupportsDisplay && graphics.Resolutions.Count > 1);
            quality.gameObject.SetActive(graphics.QualityNames.Length > 1); vsync.gameObject.SetActive(graphics.SupportsVSync);
            display.GetComponentInChildren<Text>().text = UILocalization.Translate("DISPLAY MODE") + "    " + graphics.ModeLabel;
            if (graphics.Resolutions.Count > 0)
            {
                var size = graphics.Resolutions[graphics.ResolutionIndex]; resolution.GetComponentInChildren<Text>().text = $"{UILocalization.Translate("RESOLUTION")}    {size.x} × {size.y}";
            }
            if (graphics.QualityNames.Length > 0) quality.GetComponentInChildren<Text>().text = UILocalization.Translate("QUALITY") + "    " + graphics.QualityNames[QualitySettings.GetQualityLevel()].ToUpperInvariant();
            vsync.GetComponentInChildren<Text>().text = UILocalization.Translate("VSYNC") + "    " + (QualitySettings.vSyncCount > 0 ? "ON" : "OFF");
        }
        private void OnDestroy()
        {
            if (graphics != null) graphics.Changed -= RefreshGraphics;
            if (settings != null) settings.SettingsChanged -= ApplySensitivity;
        }
    }
}
