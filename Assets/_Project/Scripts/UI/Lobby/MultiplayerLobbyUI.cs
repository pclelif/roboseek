using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Robot.Multiplayer;
using Robot.Robots.Customization;

namespace Robot.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class MultiplayerLobbyUI : MonoBehaviour
    {
        private GameObject modalRoot;
        private readonly List<Text> playerSlotTexts = new List<Text>();
        private readonly List<Image> playerColorBadges = new List<Image>();
        private Button hostStartBtn;
        private Button readyToggleBtn;
        private Text readyToggleText;
        private bool isLocalPlayerReady = false;

        public event Action BackRequested;

        private void Awake()
        {
            BuildUI();
            Hide();
        }

        public void Show()
        {
            if (modalRoot != null) modalRoot.SetActive(true);
            UpdatePlayerSlots();
        }

        public void Hide()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
        }

        private void Update()
        {
            if (modalRoot != null && modalRoot.activeSelf)
            {
                UpdatePlayerSlots();
            }
        }

        private void UpdatePlayerSlots()
        {
            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
            bool isClient = NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;
            int connectedCount = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 0;

            for (int i = 0; i < 4; i++)
            {
                bool active = i < connectedCount;
                if (i < playerSlotTexts.Count)
                {
                    if (active)
                    {
                        string role = i == 0 ? "(HOST)" : "(CLIENT)";
                        playerSlotTexts[i].text = $"OYUNCU {i + 1} {role} — HAZIR (READY)";
                        playerSlotTexts[i].color = UITheme.TextWhite;
                    }
                    else
                    {
                        playerSlotTexts[i].text = $"SLOT {i + 1} — BEKLENİYOR (WAITING)...";
                        playerSlotTexts[i].color = UITheme.TextMuted;
                    }
                }
            }

            if (hostStartBtn != null)
            {
                hostStartBtn.gameObject.SetActive(isHost);
                hostStartBtn.interactable = connectedCount >= 1;
            }

            if (readyToggleBtn != null)
            {
                readyToggleBtn.gameObject.SetActive(isClient && !isHost);
            }
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                transform.SetParent(canvasObj.transform, false);
            }

            modalRoot = UITheme.CreatePanel(canvas.transform, "LobbyPanel", new Vector2(680f, 680f), UITheme.GlassDark, UITheme.GetPanelGlass());
            RectTransform modalRect = modalRoot.GetComponent<RectTransform>();
            modalRect.anchoredPosition = Vector2.zero;

            Text title = UITheme.CreateText(modalRoot.transform, "MULTIPLAYER LOBBY", 28, TextAnchor.MiddleCenter, UITheme.AccentYellow);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 280f);
            titleRect.sizeDelta = new Vector2(600f, 50f);

            // Connect Buttons: Host / Join
            Button hostBtn = UITheme.CreateButton(modalRoot.transform, "ODA KUR (HOST)", new Vector2(240f, 48f), () =>
            {
                if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
                    NetworkManager.Singleton.StartHost();
            }, UITheme.PrimaryBlue);
            RectTransform hostRect = hostBtn.GetComponent<RectTransform>();
            hostRect.anchoredPosition = new Vector2(-130f, 200f);

            Button joinBtn = UITheme.CreateButton(modalRoot.transform, "KATIL (CLIENT JOIN)", new Vector2(240f, 48f), () =>
            {
                if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
                    NetworkManager.Singleton.StartClient();
            }, UITheme.PrimaryBlue);
            RectTransform joinRect = joinBtn.GetComponent<RectTransform>();
            joinRect.anchoredPosition = new Vector2(130f, 200f);

            // Player Slots
            float startY = 110f;
            float stepY = 64f;
            for (int i = 0; i < 4; i++)
            {
                GameObject slot = UITheme.CreatePanel(modalRoot.transform, $"Slot_{i}", new Vector2(560f, 52f), UITheme.GlassBackground, UITheme.GetPanelRectangle());
                RectTransform sRect = slot.GetComponent<RectTransform>();
                sRect.anchoredPosition = new Vector2(0f, startY - stepY * i);

                Text slotText = UITheme.CreateText(slot.transform, $"SLOT {i + 1} — BEKLENİYOR...", 16, TextAnchor.MiddleLeft, UITheme.TextMuted);
                RectTransform stRect = slotText.GetComponent<RectTransform>();
                stRect.anchoredPosition = new Vector2(30f, 0f);
                stRect.sizeDelta = new Vector2(480f, 40f);
                playerSlotTexts.Add(slotText);
            }

            // Action Buttons
            hostStartBtn = UITheme.CreateButton(modalRoot.transform, "OYUNU BAŞLAT (START GAME)", new Vector2(320f, 54f), () =>
            {
                if (NetworkRoundManager.Instance != null)
                {
                    NetworkRoundManager.Instance.HostStartGame();
                    Hide();
                }
            }, UITheme.SuccessGreen);
            RectTransform startRect = hostStartBtn.GetComponent<RectTransform>();
            startRect.anchoredPosition = new Vector2(0f, -180f);

            readyToggleBtn = UITheme.CreateButton(modalRoot.transform, "HAZIR (READY)", new Vector2(260f, 50f), () =>
            {
                isLocalPlayerReady = !isLocalPlayerReady;
                if (readyToggleText != null)
                    readyToggleText.text = isLocalPlayerReady ? "HAZIR (READY) ✓" : "HAZIR DEĞİL";
            }, UITheme.PrimaryBlue);
            RectTransform readyRect = readyToggleBtn.GetComponent<RectTransform>();
            readyRect.anchoredPosition = new Vector2(0f, -180f);
            readyToggleText = readyToggleBtn.GetComponentInChildren<Text>();

            Button backBtn = UITheme.CreateButton(modalRoot.transform, "GERİ (BACK)", new Vector2(220f, 44f), () =>
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    NetworkManager.Singleton.Shutdown();
                Hide();
                BackRequested?.Invoke();
            }, UITheme.DangerRed);
            RectTransform backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchoredPosition = new Vector2(0f, -260f);
        }
    }
}
