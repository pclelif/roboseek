using System;
using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI
{
    public static class UITheme
    {
        public static readonly Color PrimaryBlue = new Color(0.22f, 0.58f, 0.85f, 1f);
        public static readonly Color AccentYellow = new Color(0.95f, 0.77f, 0.25f, 1f);
        public static readonly Color GlassBackground = new Color(0.08f, 0.12f, 0.18f, 0.88f);
        public static readonly Color GlassDark = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        public static readonly Color TextWhite = new Color(0.94f, 0.96f, 0.98f, 1f);
        public static readonly Color TextMuted = new Color(0.65f, 0.72f, 0.82f, 1f);
        public static readonly Color SuccessGreen = new Color(0.20f, 0.75f, 0.45f, 1f);
        public static readonly Color DangerRed = new Color(0.85f, 0.25f, 0.25f, 1f);

        private const string KenneyBasePath = "Assets/ThirdParty/kenney_ui-pack-space-expansion/PNG/";

        public static Sprite LoadSprite(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(KenneyBasePath + path);
#else
            return null;
#endif
        }

        public static Sprite GetPanelGlass() => LoadSprite("Extra/Default/panel_glass.png");
        public static Sprite GetPanelRectangle() => LoadSprite("Extra/Default/panel_rectangle.png");
        public static Sprite GetPanelNotches() => LoadSprite("Extra/Default/panel_glass_notches.png");
        public static Sprite GetButtonRectangle() => LoadSprite("Extra/Default/button_rectangle_depth.png");
        public static Sprite GetButtonSquare() => LoadSprite("Extra/Default/button_square_depth.png");
        public static Sprite GetBarRound() => LoadSprite("Blue/Default/bar_round_large.png");
        public static Sprite GetBarShadow() => LoadSprite("Extra/Default/bar_shadow_round_large.png");

        public static Font GetKenneyFont()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/ThirdParty/kenney_ui-pack-space-expansion/Font/Kenney Future.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        public static GameObject CreatePanel(Transform parent, string name, Vector2 size, Color bgColor, Sprite sprite = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Image img = go.GetComponent<Image>();
            img.color = bgColor;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return go;
        }

        public static Button CreateButton(Transform parent, string labelText, Vector2 size, Action onClick, Color? btnColor = null, Sprite sprite = null)
        {
            GameObject btnObj = new GameObject($"Btn_{labelText}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            img.color = btnColor ?? PrimaryBlue;
            if (sprite != null || (sprite = GetButtonRectangle()) != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = img.color;
            cb.highlightedColor = img.color * 1.18f;
            cb.pressedColor = img.color * 0.85f;
            cb.selectedColor = cb.highlightedColor;
            btn.colors = cb;

            if (onClick != null) btn.onClick.AddListener(() => onClick());

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            Text txt = txtObj.GetComponent<Text>();
            txt.text = labelText;
            txt.font = GetKenneyFont() ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 18;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = TextWhite;

            return btn;
        }

        public static Text CreateText(Transform parent, string content, int fontSize, TextAnchor alignment, Color? color = null)
        {
            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(parent, false);
            Text txt = txtObj.GetComponent<Text>();
            txt.text = content;
            txt.font = GetKenneyFont() ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color ?? TextWhite;
            return txt;
        }
    }
}
