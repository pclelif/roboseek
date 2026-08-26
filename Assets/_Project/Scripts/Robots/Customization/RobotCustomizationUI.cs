using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Robot.UI;

namespace Robot.Robots.Customization
{
    [DisallowMultipleComponent]
    public sealed class RobotCustomizationUI : MonoBehaviour
    {
        [SerializeField] private RobotColorPalette palette;
        [SerializeField] private Transform previewRobotRoot;

        private GameObject modalRoot;
        private int selectedIndex = 0;
        private readonly List<Button> colorButtons = new List<Button>();
        private Text selectedColorName;

        public event Action Closed;

        private void Awake()
        {
            if (palette == null && RobotColorService.Instance != null) palette = RobotColorService.Instance.Palette;
            BuildUI();
            Hide();
        }

        public void Show()
        {
            if (modalRoot != null) modalRoot.SetActive(true);
            if (previewRobotRoot != null) previewRobotRoot.gameObject.SetActive(true);

            selectedIndex = RobotColorService.Instance != null
                ? RobotColorService.Instance.LoadSinglePlayerSelection()
                : PlayerPrefs.GetInt(RobotColorService.PlayerPreferenceKey, 0);

            ApplyPreviewColor(selectedIndex);
        }

        public void Hide()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            if (previewRobotRoot != null) previewRobotRoot.gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("CustomizationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                transform.SetParent(canvasObj.transform, false);
            }

            modalRoot = UITheme.CreatePanel(canvas.transform, "CustomizationPanel", new Vector2(620f, 680f), UITheme.GlassDark, UITheme.GetPanelGlass());
            RectTransform modalRect = modalRoot.GetComponent<RectTransform>();
            modalRect.anchoredPosition = new Vector2(0f, 0f);

            Text title = UITheme.CreateText(modalRoot.transform, "ROBOT ÖZELLEŞTİRME", 28, TextAnchor.MiddleCenter, UITheme.AccentYellow);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 280f);
            titleRect.sizeDelta = new Vector2(500f, 50f);

            selectedColorName = UITheme.CreateText(modalRoot.transform, "SEÇİLEN RENK: Mavi", 18, TextAnchor.MiddleCenter, UITheme.TextWhite);
            RectTransform nameRect = selectedColorName.GetComponent<RectTransform>();
            nameRect.anchoredPosition = new Vector2(0f, 220f);
            nameRect.sizeDelta = new Vector2(500f, 30f);

            // Color buttons grid
            GameObject grid = new GameObject("ColorGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(modalRoot.transform, false);
            RectTransform gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchoredPosition = new Vector2(0f, 40f);
            gridRect.sizeDelta = new Vector2(520f, 240f);

            GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(92f, 92f);
            layout.spacing = new Vector2(12f, 12f);
            layout.childAlignment = TextAnchor.MiddleCenter;

            if (palette != null)
            {
                for (int i = 0; i < palette.Count; i++)
                {
                    int index = i;
                    var entry = palette.Colors[i];
                    Button btn = UITheme.CreateButton(grid.transform, string.Empty, new Vector2(92f, 92f), () =>
                    {
                        SelectColor(index);
                    }, entry.bodyColor, UITheme.GetButtonSquare());
                    colorButtons.Add(btn);
                }
            }

            Button saveBtn = UITheme.CreateButton(modalRoot.transform, "KAYDET (SAVE)", new Vector2(240f, 52f), () =>
            {
                Save();
                Hide();
            }, UITheme.SuccessGreen);
            RectTransform saveRect = saveBtn.GetComponent<RectTransform>();
            saveRect.anchoredPosition = new Vector2(-130f, -260f);

            Button backBtn = UITheme.CreateButton(modalRoot.transform, "GERİ (BACK)", new Vector2(240f, 52f), Hide, UITheme.PrimaryBlue);
            RectTransform backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchoredPosition = new Vector2(130f, -260f);
        }

        private void SelectColor(int index)
        {
            selectedIndex = index;
            ApplyPreviewColor(index);
        }

        private void ApplyPreviewColor(int index)
        {
            if (palette != null && palette.TryGet(index, out var entry))
            {
                if (selectedColorName != null) selectedColorName.text = $"SEÇİLEN RENK: {entry.displayName.ToUpper()}";

                // Update 3D preview model renderers
                if (previewRobotRoot != null)
                {
                    foreach (var rend in previewRobotRoot.GetComponentsInChildren<Renderer>())
                    {
                        if (rend != null && rend.sharedMaterial != null)
                        {
                            rend.material.color = entry.bodyColor;
                        }
                    }
                }
            }
        }

        private void Save()
        {
            if (RobotColorService.Instance != null)
            {
                RobotColorService.Instance.SaveSinglePlayerSelection(selectedIndex);
            }
            else
            {
                PlayerPrefs.SetInt(RobotColorService.PlayerPreferenceKey, selectedIndex);
                PlayerPrefs.Save();
            }
            Debug.Log($"[RobotCustomizationUI] Color {selectedIndex} saved successfully.");
        }
    }
}
