using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Robot.Robots.Customization;
using Robot.UI.Lobby;
using Robot.UI.Settings;

namespace Robot.UI.MainMenu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private SettingsUI settingsUI;
        [SerializeField] private RobotCustomizationUI customizationUI;
        [SerializeField] private MultiplayerLobbyUI lobbyUI;

        private GameObject menuPanel;

        private void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                transform.SetParent(canvasObj.transform, false);
            }

            menuPanel = UITheme.CreatePanel(canvas.transform, "MainMenuPanel", new Vector2(480f, 620f), UITheme.GlassDark, UITheme.GetPanelGlass());
            RectTransform menuRect = menuPanel.GetComponent<RectTransform>();
            menuRect.anchoredPosition = new Vector2(0f, 0f);

            Text title = UITheme.CreateText(menuPanel.transform, "ROBOT HUNT", 36, TextAnchor.MiddleCenter, UITheme.AccentYellow);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 240f);
            titleRect.sizeDelta = new Vector2(440f, 60f);

            float startY = 140f;
            float stepY = 70f;

            // Singleplayer Button
            Button singleBtn = UITheme.CreateButton(menuPanel.transform, "SINGLEPLAYER", new Vector2(360f, 54f), () =>
            {
                SceneManager.LoadScene("Demo");
            }, UITheme.PrimaryBlue);
            singleBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, startY);

            // Multiplayer Button
            Button multiBtn = UITheme.CreateButton(menuPanel.transform, "MULTIPLAYER", new Vector2(360f, 54f), () =>
            {
                if (lobbyUI != null)
                {
                    lobbyUI.Show();
                }
                else
                {
                    SceneManager.LoadScene("Demo2");
                }
            }, UITheme.PrimaryBlue);
            multiBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, startY - stepY);

            // Customization Button
            Button customBtn = UITheme.CreateButton(menuPanel.transform, "ÖZELLEŞTİRME (CUSTOMIZE)", new Vector2(360f, 54f), () =>
            {
                if (customizationUI != null) customizationUI.Show();
            }, UITheme.PrimaryBlue);
            customBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, startY - stepY * 2);

            // Settings Button
            Button settingsBtn = UITheme.CreateButton(menuPanel.transform, "AYARLAR (SETTINGS)", new Vector2(360f, 54f), () =>
            {
                if (settingsUI != null) settingsUI.Show();
            }, UITheme.PrimaryBlue);
            settingsBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, startY - stepY * 3);

            // Quit Button
            Button quitBtn = UITheme.CreateButton(menuPanel.transform, "ÇIKIŞ (QUIT)", new Vector2(360f, 54f), () =>
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }, UITheme.DangerRed);
            quitBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, startY - stepY * 4);
        }
    }
}
