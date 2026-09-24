using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI.Production
{
    // Shared layout primitives with Kenney UI Pack theme support.
    public static class UIView
    {
        public static readonly Color Ink = Robot.UI.UITheme.TextWhite;
        public static readonly Color Paper = Robot.UI.UITheme.GlassBackground;
        // Gameplay UI follows the selected map: City remains yellow, while the
        // three alternate maps receive their green, red, and blue HUD variants.
        // The lobby has its own serialized styling and is not rebuilt here.
        public static Color Accent => Robot.Core.MapManager.SelectedMap != null
            ? Robot.Core.MapManager.SelectedMap.themeColor
            : Robot.UI.UITheme.AccentYellow;
        public static Color AccentYellow => Accent;

        public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position = default)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var rect = Rect(name, parent, Vector2.zero);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image Panel(string name, Transform parent, Vector2 size, Color color, Vector2 position = default)
        {
            var image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
            image.color = color;
            var panelSprite = Robot.UI.UITheme.GetPanelGlass() ?? Robot.UI.UITheme.GetPanelRectangle();
            if (panelSprite != null)
            {
                image.sprite = panelSprite;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        public static Font FontFor(string name, string text)
        {
            var kenney = Robot.UI.UITheme.GetKenneyFont();
            if (kenney != null && Supports(kenney, text)) return kenney;
            return Resources.Load<Font>("RobotHuntUI/Inter-SemiBold") ?? kenney;
        }

        public static void ApplyFont(Text label)
        {
            if (label == null) return;
            label.font = FontFor(label.name, label.text);
            label.fontStyle = FontStyle.Normal;
            if (label.GetComponent<UIFontBinder>() == null) label.gameObject.AddComponent<UIFontBinder>();
        }

        static bool Supports(Font font, string text)
        {
            if (font == null || string.IsNullOrEmpty(text)) return true;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<')
                {
                    int end = text.IndexOf('>', i + 1);
                    if (end > i) { i = end; continue; }
                }
                if (char.IsWhiteSpace(c) || char.IsControl(c) || c <= 127) continue;
                if (!font.HasCharacter(c)) return false;
            }
            return true;
        }

        public static Text Label(string name, Transform parent, string text, int size, Vector2 dimensions, Vector2 position = default, Color? color = null)
        {
            var label = Rect(name, parent, dimensions, position).gameObject.AddComponent<Text>();
            label.fontSize = size; label.text = text; label.color = color ?? Ink;
            ApplyFont(label);
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }

        public static Button Button(string text, Transform parent, Vector2 pos, Vector2 size)
        {
            var image = Rect(text, parent, size, pos).gameObject.AddComponent<Image>();
            var btnSprite = Robot.UI.UITheme.GetButtonRectangle();
            if (btnSprite != null)
            {
                image.sprite = btnSprite;
                image.type = Image.Type.Sliced;
            }
            image.color = AccentYellow;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Label("Label", image.transform, text, 17, size - new Vector2(16, 4), Vector2.zero, Robot.UI.UITheme.TextWhite);
            StyleButton(button, AccentYellow, true);
            return button;
        }

        // Keep selection/hover from shifting one yellow button into a different hue.
        public static void StyleButton(Button button, Color surface, bool darkText)
        {
            if (button == null) return;
            if (button.image != null)
            {
                var btnSprite = Robot.UI.UITheme.GetButtonRectangle();
                if (btnSprite != null)
                {
                    button.image.sprite = btnSprite;
                    button.image.type = Image.Type.Sliced;
                }
                button.image.color = surface;
            }
            var colors = button.colors;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(.82f, .82f, .82f);
            colors.disabledColor = new Color(.65f, .65f, .65f, .55f);
            button.colors = colors;
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.color = darkText ? new Color(.07f, .07f, .07f) : Color.white;
        }

        public static void StyleSlider(Slider slider)
        {
            var track = slider.transform.Find("Track")?.GetComponent<Image>();
            if (track != null) { track.sprite = null; track.color = new Color(.16f,.16f,.16f); }
            var fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
            if (fill != null) { fill.sprite = null; fill.color = AccentYellow; }
            if (slider.targetGraphic != null) slider.targetGraphic.color = AccentYellow;
            var colors = slider.colors;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(.82f,.82f,.82f);
            slider.colors = colors;
        }

        public static Slider Slider(string name, Transform parent, float y, float min, float max)
        {
            Label("Label", parent, name, 16, new Vector2(280, 30), new Vector2(-115, y), Ink);
            var root = Rect(name, parent, new Vector2(260, 30), new Vector2(155, y));
            var slider = root.gameObject.AddComponent<Slider>();
            var track = Panel("Track", root, new Vector2(260, 10), new Color(0.12f, 0.18f, 0.25f, 0.9f));
            var trackSprite = Robot.UI.UITheme.GetBarRound();
            if (trackSprite != null)
            {
                track.sprite = trackSprite;
                track.type = Image.Type.Sliced;
            }
            track.raycastTarget = true;

            var fillArea = Stretch("FillArea", root); fillArea.offsetMin = new Vector2(4, 2); fillArea.offsetMax = new Vector2(-4, -2);
            var fill = Panel("Fill", fillArea, Vector2.zero, Accent); fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            var fillSprite = Robot.UI.UITheme.GetBarRound();
            if (fillSprite != null)
            {
                fill.sprite = fillSprite;
                fill.type = Image.Type.Sliced;
            }

            var handleArea = Stretch("HandleArea", root); handleArea.offsetMin = new Vector2(8, 0); handleArea.offsetMax = new Vector2(-8, 0);
            var handle = Panel("Handle", handleArea, new Vector2(22, 30), AccentYellow);
            var handleSprite = Robot.UI.UITheme.GetButtonSquare();
            if (handleSprite != null)
            {
                handle.sprite = handleSprite;
                handle.type = Image.Type.Sliced;
            }

            slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.minValue = min; slider.maxValue = max;
            StyleSlider(slider);
            return slider;
        }

        public static GameObject Screen(string name, Transform parent, float dim)
        {
            var root = Stretch(name, parent);
            var bg = root.gameObject.AddComponent<Image>(); bg.color = new Color(0.04f, 0.06f, 0.10f, dim);
            var bgSprite = Robot.UI.UITheme.GetPanelGlass();
            if (bgSprite != null)
            {
                bg.sprite = bgSprite;
                bg.type = Image.Type.Sliced;
            }
            return root.gameObject;
        }

        public static void Visible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1 : 0;
            group.interactable = group.blocksRaycasts = visible;
        }

        public static Text Keycap(Transform parent, string key, Vector2 pos, Vector2 size)
        {
            var shadow = Panel("Keycap", parent, size, new Color(.08f, .12f, .18f), pos);
            shadow.raycastTarget = false;
            var face = Panel("Face", shadow.transform, size - new Vector2(2, 5), AccentYellow, new Vector2(0, 2)); face.raycastTarget = false;
            var keySprite = Robot.UI.UITheme.GetButtonSquare();
            if (keySprite != null)
            {
                shadow.sprite = keySprite;
                shadow.type = Image.Type.Sliced;
                face.sprite = keySprite;
                face.type = Image.Type.Sliced;
            }
            return Label("Key", face.transform, key, 16, size - new Vector2(4, 4), Vector2.zero, new Color(.08f, .08f, .08f, 1f));
        }
    }

    public sealed class UIFontBinder : MonoBehaviour
    {
        Text label;
        string last;
        void Awake() => label = GetComponent<Text>();
        void OnEnable() => Sync();
        void LateUpdate() => Sync();
        void Sync()
        {
            if (label == null) label = GetComponent<Text>();
            if (label == null || label.text == last) return;
            last = label.text;
            label.font = UIView.FontFor(label.name, label.text);
            label.fontStyle = FontStyle.Normal;
        }
    }
}
