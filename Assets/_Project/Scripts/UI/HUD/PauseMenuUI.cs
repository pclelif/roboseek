using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Robot.UI.Settings;

namespace Robot.UI.HUD
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private SettingsUI settingsUI;
        private GameObject pausePanel;
        private bool isPaused = false;

        private void Awake()
        {
            if (!enabled || FindFirstObjectByType<Robot.UI.Production.UIStateManager>() != null) { enabled = false; return; }
            BuildUI();
            Hide();
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (isPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            isPaused = true;
            if (pausePanel != null) pausePanel.SetActive(true);

            // In Singleplayer, freeze time. In Multiplayer, keep network clock running!
            bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (!isMultiplayer)
            {
                Time.timeScale = 0f;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            isPaused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                transform.SetParent(canvasObj.transform, false);
            }

            pausePanel = UITheme.CreatePanel(canvas.transform, "PausePanel", new Vector2(440f, 480f), UITheme.GlassDark, UITheme.GetPanelGlass());
            RectTransform pRect = pausePanel.GetComponent<RectTransform>();
            pRect.anchoredPosition = Vector2.zero;

            Text title = UITheme.CreateText(pausePanel.transform, "DURAKLATILDI (PAUSED)", 26, TextAnchor.MiddleCenter, UITheme.AccentYellow);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 170f);
            titleRect.sizeDelta = new Vector2(400f, 50f);

            float startY = 80f;
            float stepY = 65f;

            Button resumeBtn = UITheme.CreateButton(pausePanel.transform, "DEVAM ET (RESUME)", new Vector2(320f, 50f), Resume, UITheme.PrimaryBlue);
            resumeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, startY);

            Button settingsBtn = UITheme.CreateButton(pausePanel.transform, "AYARLAR (SETTINGS)", new Vector2(320f, 50f), () =>
            {
                if (settingsUI != null) settingsUI.Show();
            }, UITheme.PrimaryBlue);
            settingsBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, startY - stepY);

            Button menuBtn = UITheme.CreateButton(pausePanel.transform, "ANA MENÜ (MAIN MENU)", new Vector2(320f, 50f), () =>
            {
                Time.timeScale = 1f;
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    NetworkManager.Singleton.Shutdown();
                if (Application.CanStreamedLevelBeLoaded("RoboSeek_Lobby"))
                    SceneManager.LoadScene("RoboSeek_Lobby");
                else
                    SceneManager.LoadScene("Assets/_Project/Scenes/RoboSeek_Lobby.unity");
            }, UITheme.DangerRed);
            menuBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, startY - stepY * 2);
        }

        private void Hide()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
        }
    }
}
