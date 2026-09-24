using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Robot.UI;

namespace Robot.UI.MainMenu
{
    public sealed class WorldSelectUI : MonoBehaviour
    {
        public static WorldSelectUI Instance { get; private set; }

        private GameObject panelRoot;
        private CanvasGroup canvasGroup;

        public static string SelectedWorldScene { get; private set; } = "Demo2"; // Default City scene

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void LoadWorld(string sceneName)
        {
            WorldThemeManager.SelectWorldByScene(sceneName);
            Debug.Log($"[ROBOSEEK] Loading World Scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        private Text titleText;

        private void BuildUI()
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            if (parentCanvas == null) return;

            panelRoot = new GameObject("WorldSelectModalPanel", typeof(RectTransform), typeof(CanvasGroup));
            panelRoot.transform.SetParent(parentCanvas.transform, false);

            RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.one;

            canvasGroup = panelRoot.GetComponent<CanvasGroup>();

            // Dark Backdrop
            GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(panelRoot.transform, false);
            RectTransform bdRect = backdrop.GetComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.one;
            backdrop.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.10f, 0.92f);

            // Container Panel
            GameObject container = new GameObject("Container", typeof(RectTransform), typeof(Image));
            container.transform.SetParent(panelRoot.transform, false);
            RectTransform cRect = container.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f);
            cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.anchoredPosition = Vector2.zero;
            cRect.sizeDelta = new Vector2(840f, 500f);
            container.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 0.96f);

            // Header Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(container.transform, false);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0f, -24f);
            tRect.sizeDelta = new Vector2(0f, 40f);

            titleText = titleObj.GetComponent<Text>();
            titleText.font = UITheme.GetKenneyFont();
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = WorldThemeManager.CurrentAccent;
            titleText.text = "HARİTA & DÜNYA SEÇİMİ";

            // World Cards Container
            GameObject cardsRow = new GameObject("CardsRow", typeof(RectTransform));
            cardsRow.transform.SetParent(container.transform, false);
            RectTransform crRect = cardsRow.GetComponent<RectTransform>();
            crRect.anchorMin = new Vector2(0.5f, 0.5f);
            crRect.anchorMax = new Vector2(0.5f, 0.5f);
            crRect.pivot = new Vector2(0.5f, 0.5f);
            crRect.anchoredPosition = new Vector2(0f, -10f);
            crRect.sizeDelta = new Vector2(780f, 320f);

            // Dynamically build cards for available worlds (World 1: City, World 2: Adventure)
            float startX = -190f;
            float stepX = 380f;
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                var world = WorldThemeManager.AvailableWorlds[index];
                Vector2 cardPos = new Vector2(startX + stepX * i, 0f);
                CreateWorldCard(cardsRow.transform, cardPos, index, world, () =>
                {
                    WorldThemeManager.SelectWorld(index);
                    if (titleText != null) titleText.color = WorldThemeManager.CurrentAccent;
                    Hide();
                });
            }

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(container.transform, false);
            RectTransform cbRect = closeBtnObj.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.5f, 0f);
            cbRect.anchorMax = new Vector2(0.5f, 0f);
            cbRect.pivot = new Vector2(0.5f, 0f);
            cbRect.anchoredPosition = new Vector2(0f, 20f);
            cbRect.sizeDelta = new Vector2(160f, 36f);

            closeBtnObj.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.35f, 0.95f);
            Button closeBtn = closeBtnObj.GetComponent<Button>();
            closeBtn.onClick.AddListener(Hide);

            GameObject cbTextObj = new GameObject("CloseText", typeof(RectTransform), typeof(Text));
            cbTextObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cbtRect = cbTextObj.GetComponent<RectTransform>();
            cbtRect.anchorMin = Vector2.zero;
            cbtRect.anchorMax = Vector2.one;
            cbtRect.sizeDelta = Vector2.zero;

            Text cbText = cbTextObj.GetComponent<Text>();
            cbText.font = UITheme.GetKenneyFont();
            cbText.fontSize = 13;
            cbText.fontStyle = FontStyle.Bold;
            cbText.alignment = TextAnchor.MiddleCenter;
            cbText.color = Color.white;
            cbText.text = "KAPAT";

            Hide();
        }

        private void CreateWorldCard(Transform parent, Vector2 pos, int index, WorldDefinition world, Action onClick)
        {
            string title = $"{world.icon} DÜNYA {index + 1}: {world.displayName}";
            CreateWorldCard(parent, pos, title, world.description, world.cardBgColor, world.accentColor, onClick);
        }

        private void CreateWorldCard(Transform parent, Vector2 pos, string title, string desc, Color bgColor, Color accentColor, Action onClick)
        {
            GameObject cardObj = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            cardObj.transform.SetParent(parent, false);

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(340f, 300f);

            cardObj.GetComponent<Image>().color = bgColor;
            Button btn = cardObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            // Title
            GameObject tObj = new GameObject("CardTitle", typeof(RectTransform), typeof(Text));
            tObj.transform.SetParent(cardObj.transform, false);
            RectTransform tr = tObj.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.anchoredPosition = new Vector2(0f, -16f);
            tr.sizeDelta = new Vector2(-20f, 35f);

            Text tText = tObj.GetComponent<Text>();
            tText.font = UITheme.GetKenneyFont();
            tText.fontSize = 15;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = accentColor;
            tText.text = title;

            // Desc
            GameObject dObj = new GameObject("CardDesc", typeof(RectTransform), typeof(Text));
            dObj.transform.SetParent(cardObj.transform, false);
            RectTransform dr = dObj.GetComponent<RectTransform>();
            dr.anchorMin = Vector2.zero;
            dr.anchorMax = Vector2.one;
            dr.offsetMin = new Vector2(20f, 60f);
            dr.offsetMax = new Vector2(-20f, -60f);

            Text dText = dObj.GetComponent<Text>();
            dText.font = UITheme.GetKenneyFont();
            dText.fontSize = 13;
            dText.lineSpacing = 1.3f;
            dText.alignment = TextAnchor.MiddleLeft;
            dText.color = new Color(0.9f, 0.95f, 1.0f, 0.95f);
            dText.text = desc;

            // Play Button inside card
            GameObject playBtnObj = new GameObject("PlayBtn", typeof(RectTransform), typeof(Image));
            playBtnObj.transform.SetParent(cardObj.transform, false);
            RectTransform pr = playBtnObj.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.5f, 0f);
            pr.anchorMax = new Vector2(0.5f, 0f);
            pr.pivot = new Vector2(0.5f, 0f);
            pr.anchoredPosition = new Vector2(0f, 16f);
            pr.sizeDelta = new Vector2(180f, 36f);

            playBtnObj.GetComponent<Image>().color = new Color(1.0f, 0.78f, 0.12f, 0.95f);

            GameObject ptObj = new GameObject("PlayText", typeof(RectTransform), typeof(Text));
            ptObj.transform.SetParent(playBtnObj.transform, false);
            RectTransform ptr = ptObj.GetComponent<RectTransform>();
            ptr.anchorMin = Vector2.zero;
            ptr.anchorMax = Vector2.one;
            ptr.sizeDelta = Vector2.zero;

            Text ptText = ptObj.GetComponent<Text>();
            ptText.font = UITheme.GetKenneyFont();
            ptText.fontSize = 13;
            ptText.fontStyle = FontStyle.Bold;
            ptText.alignment = TextAnchor.MiddleCenter;
            ptText.color = new Color(0.08f, 0.10f, 0.14f, 1.0f);
            ptText.text = "OYNAT";
        }
    }
}
