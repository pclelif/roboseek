using System;
using UnityEngine;
using UnityEngine.UI;
using Robot.Combat;

namespace Robot.UI.HUD
{
    [DisallowMultipleComponent]
    public sealed class CombatHealthHUD : MonoBehaviour
    {
        private CombatHealth playerHealth;
        private Image healthFill;
        private Text healthText;
        private GameObject knockoutOverlay;
        private Text knockoutCountdownText;

        private void Start()
        {
            BuildUI();
            FindLocalPlayerHealth();
        }

        private void Update()
        {
            if (playerHealth == null)
            {
                FindLocalPlayerHealth();
                return;
            }

            // Update health bar
            float ratio = Mathf.Clamp01(playerHealth.CurrentHealth / Mathf.Max(1f, playerHealth.MaxHealth));
            if (healthFill != null)
            {
                healthFill.fillAmount = ratio;
                healthFill.color = ratio > 0.35f ? UITheme.PrimaryBlue : UITheme.DangerRed;
            }

            if (healthText != null)
            {
                healthText.text = $"HP: {Mathf.CeilToInt(playerHealth.CurrentHealth)} / {Mathf.CeilToInt(playerHealth.MaxHealth)}";
            }

            // Knockout overlay
            if (knockoutOverlay != null)
            {
                bool isKO = playerHealth.IsKnockedOut;
                knockoutOverlay.SetActive(isKO);
                if (isKO && knockoutCountdownText != null)
                {
                    knockoutCountdownText.text = $"BAYILDIN! (KNOCKED OUT)\nCANLANMA: {playerHealth.KnockoutTimeRemaining:F1}s";
                }
            }
        }

        private void FindLocalPlayerHealth()
        {
            // 1. Tag Player
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerHealth = p.GetComponent<CombatHealth>();

            // 2. Network owner player
            if (playerHealth == null)
            {
                var netPlayers = FindObjectsByType<Robot.Multiplayer.NetworkRobotPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var np in netPlayers)
                {
                    if (np != null && np.IsOwner)
                    {
                        playerHealth = np.GetComponent<CombatHealth>();
                        break;
                    }
                }
            }
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("HealthCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                transform.SetParent(canvasObj.transform, false);
            }

            // Bottom-Left Health Bar Panel
            GameObject healthPanel = UITheme.CreatePanel(canvas.transform, "HealthBarPanel", new Vector2(280f, 48f), UITheme.GlassDark, UITheme.GetPanelRectangle());
            RectTransform hRect = healthPanel.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 0f);
            hRect.anchorMax = new Vector2(0f, 0f);
            hRect.pivot = new Vector2(0f, 0f);
            hRect.anchoredPosition = new Vector2(28f, 28f);

            GameObject barBg = UITheme.CreatePanel(healthPanel.transform, "BarBg", new Vector2(250f, 22f), new Color(0.12f, 0.16f, 0.22f, 1f));
            RectTransform bgRect = barBg.GetComponent<RectTransform>();
            bgRect.anchoredPosition = new Vector2(0f, 0f);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(barBg.transform, false);
            RectTransform fRect = fillObj.GetComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.offsetMin = Vector2.zero;
            fRect.offsetMax = Vector2.zero;

            healthFill = fillObj.GetComponent<Image>();
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillAmount = 1f;
            healthFill.color = UITheme.PrimaryBlue;

            healthText = UITheme.CreateText(healthPanel.transform, "HP: 100 / 100", 14, TextAnchor.MiddleCenter, UITheme.TextWhite);
            RectTransform txtRect = healthText.GetComponent<RectTransform>();
            txtRect.anchoredPosition = new Vector2(0f, 0f);
            txtRect.sizeDelta = new Vector2(250f, 24f);

            // Knockout Center Overlay
            knockoutOverlay = UITheme.CreatePanel(canvas.transform, "KnockoutOverlay", new Vector2(480f, 160f), new Color(0.6f, 0.1f, 0.1f, 0.92f), UITheme.GetPanelGlass());
            RectTransform koRect = knockoutOverlay.GetComponent<RectTransform>();
            koRect.anchoredPosition = new Vector2(0f, 0f);

            knockoutCountdownText = UITheme.CreateText(knockoutOverlay.transform, "BAYILDIN!\nCANLANMA: 5.0s", 22, TextAnchor.MiddleCenter, UITheme.TextWhite);
            RectTransform koTxtRect = knockoutCountdownText.GetComponent<RectTransform>();
            koTxtRect.anchoredPosition = Vector2.zero;
            koTxtRect.sizeDelta = new Vector2(440f, 140f);

            knockoutOverlay.SetActive(false);
        }
    }
}
