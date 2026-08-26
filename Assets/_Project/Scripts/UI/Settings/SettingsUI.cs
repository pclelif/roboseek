using System;
using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI.Settings
{
    [DisallowMultipleComponent]
    public sealed class SettingsUI : MonoBehaviour
    {
        private GameObject modalRoot;
        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private Slider sensitivitySlider;
        private Text langButtonText;

        private void Awake()
        {
            BuildUI();
            Hide();
        }

        public void Show()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
                SyncFromManager();
            }
        }

        public void Hide()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
        }

        private void SyncFromManager()
        {
            SettingsManager sm = SettingsManager.Instance ?? FindFirstObjectByType<SettingsManager>();
            if (sm == null) return;
            if (masterSlider != null) masterSlider.value = sm.MasterVolume;
            if (musicSlider != null) musicSlider.value = sm.MusicVolume;
            if (sfxSlider != null) sfxSlider.value = sm.SFXVolume;
            if (sensitivitySlider != null) sensitivitySlider.value = sm.Sensitivity;
            if (langButtonText != null) langButtonText.text = $"DİL / LANG: {sm.Language}";
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("SettingsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                transform.SetParent(canvasObj.transform, false);
            }

            modalRoot = UITheme.CreatePanel(canvas.transform, "SettingsModal", new Vector2(560f, 620f), UITheme.GlassDark, UITheme.GetPanelGlass());
            RectTransform modalRect = modalRoot.GetComponent<RectTransform>();
            modalRect.anchoredPosition = Vector2.zero;

            Text title = UITheme.CreateText(modalRoot.transform, "AYARLAR / SETTINGS", 26, TextAnchor.MiddleCenter, UITheme.AccentYellow);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 240f);
            titleRect.sizeDelta = new Vector2(500f, 50f);

            float startY = 160f;
            float stepY = 80f;

            masterSlider = CreateSettingSlider(modalRoot.transform, "ANA SES (MASTER)", 0f, 1f, startY, (v) => SettingsManager.Instance?.SetMasterVolume(v));
            musicSlider = CreateSettingSlider(modalRoot.transform, "MÜZİK (MUSIC)", 0f, 1f, startY - stepY, (v) => SettingsManager.Instance?.SetMusicVolume(v));
            sfxSlider = CreateSettingSlider(modalRoot.transform, "EFEKTLER (SFX)", 0f, 1f, startY - stepY * 2, (v) => SettingsManager.Instance?.SetSFXVolume(v));
            sensitivitySlider = CreateSettingSlider(modalRoot.transform, "HASSASİYET (SENSITIVITY)", 0.2f, 3.0f, startY - stepY * 3, (v) => SettingsManager.Instance?.SetSensitivity(v));

            Button langBtn = UITheme.CreateButton(modalRoot.transform, "DİL: TÜRKÇE", new Vector2(340f, 48f), () =>
            {
                SettingsManager sm = SettingsManager.Instance ?? FindFirstObjectByType<SettingsManager>();
                if (sm != null)
                {
                    string newLang = sm.Language == "Türkçe" ? "English" : "Türkçe";
                    sm.SetLanguage(newLang);
                    if (langButtonText != null) langButtonText.text = $"DİL / LANG: {newLang}";
                }
            }, UITheme.PrimaryBlue);
            RectTransform langRect = langBtn.GetComponent<RectTransform>();
            langRect.anchoredPosition = new Vector2(0f, startY - stepY * 4);
            langButtonText = langBtn.GetComponentInChildren<Text>();

            Button closeBtn = UITheme.CreateButton(modalRoot.transform, "KAPAT / CLOSE", new Vector2(240f, 48f), Hide, UITheme.DangerRed);
            RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchoredPosition = new Vector2(0f, -250f);
        }

        private Slider CreateSettingSlider(Transform parent, string label, float min, float max, float yPos, Action<float> onValueChanged)
        {
            GameObject group = new GameObject($"Group_{label}", typeof(RectTransform));
            group.transform.SetParent(parent, false);
            RectTransform groupRect = group.GetComponent<RectTransform>();
            groupRect.anchoredPosition = new Vector2(0f, yPos);
            groupRect.sizeDelta = new Vector2(460f, 60f);

            Text lbl = UITheme.CreateText(group.transform, label, 15, TextAnchor.MiddleLeft, UITheme.TextMuted);
            RectTransform lblRect = lbl.GetComponent<RectTransform>();
            lblRect.anchoredPosition = new Vector2(0f, 15f);
            lblRect.sizeDelta = new Vector2(460f, 25f);

            GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(group.transform, false);
            RectTransform sRect = sliderObj.GetComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, -12f);
            sRect.sizeDelta = new Vector2(460f, 24f);

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;

            GameObject bg = UITheme.CreatePanel(sliderObj.transform, "Background", new Vector2(460f, 12f), new Color(0.15f, 0.2f, 0.28f, 1f));
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fill = UITheme.CreatePanel(fillArea.transform, "Fill", Vector2.zero, UITheme.PrimaryBlue);
            RectTransform fRect = fill.GetComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.offsetMin = Vector2.zero;
            fRect.offsetMax = Vector2.zero;

            slider.targetGraphic = fill.GetComponent<Image>();
            slider.fillRect = fRect;

            if (onValueChanged != null) slider.onValueChanged.AddListener((v) => onValueChanged(v));
            return slider;
        }
    }
}
