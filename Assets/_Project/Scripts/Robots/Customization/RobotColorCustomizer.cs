using System;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.Robots.Customization
{
    public class RobotColorCustomizer : MonoBehaviour
    {
        public enum ColorTheme
        {
            Siyah = 0,       // Siyah (DEFAULT INITIAL THEME)
            Kirmizi = 1,     // Kırmızı
            Turuncu = 2,     // Turuncu
            Sari = 3,        // Sarı
            Yesil = 4,       // Yeşil
            Mavi = 5,        // Mavi
            Mor = 6,         // Mor
            Pembe = 7,       // Pembe
            Kahverengi = 8,  // Kahverengi
            Beyaz = 9        // Beyaz
        }

        public enum EyeColorMode
        {
            SleekBlack = 0,      // Sleek Glossy Black Eyes
            MatchTheme = 1       // Match Robot Body Theme Color
        }

        [Serializable]
        public struct ColorPreset
        {
            public ColorTheme theme;
            public string name;
            public Color bodyColor;
            public Color jointColor;
        }

        [Header("Configuration")]
        [SerializeField] private ColorTheme activeTheme = ColorTheme.Siyah; // Starts Siyah as requested!
        [SerializeField] private EyeColorMode eyeMode = EyeColorMode.SleekBlack;
        [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();

        [Header("Color Presets (Matched to User Scene View Cars: Siyah -> Kırmızı -> Turuncu -> Sarı -> Yeşil -> Mavi -> Mor -> Pembe -> Kahverengi -> Beyaz)")]
        [SerializeField]
        private ColorPreset[] presets = new ColorPreset[]
        {
            new ColorPreset 
            { 
                theme = ColorTheme.Siyah, 
                name = "Siyah", 
                bodyColor = new Color(0.31f, 0.31f, 0.31f), 
                jointColor = new Color(0.14f, 0.14f, 0.14f)
            },
            new ColorPreset 
            { 
                theme = ColorTheme.Kirmizi, 
                name = "Kırmızı", 
                bodyColor = new Color(0.62f, 0.24f, 0.24f), 
                jointColor = new Color(0.14f, 0.14f, 0.14f)
            },
            new ColorPreset 
            { 
                theme = ColorTheme.Turuncu, 
                name = "Turuncu", 
                bodyColor = new Color(0.75f, 0.45f, 0.28f), 
                jointColor = new Color(0.14f, 0.14f, 0.14f)
            },
            new ColorPreset 
            { 
                theme = ColorTheme.Sari, 
                name = "Sarı", 
                bodyColor = new Color(0.78f, 0.68f, 0.38f), 
                jointColor = new Color(0.14f, 0.14f, 0.14f)
            },
            new ColorPreset 
            { 
                theme = ColorTheme.Yesil, 
                name = "Yeşil", 
                bodyColor = new Color(0.38f, 0.55f, 0.45f), 
                jointColor = new Color(0.14f, 0.14f, 0.14f)
            },
            new ColorPreset 
            { 
                theme = ColorTheme.Mavi, 
                name = "Mavi", 
                bodyColor = new Color(0.35f, 0.45f, 0.65f), 
                jointColor = new Color(0.14f, 0.14f, 0.14f)
            },
            new ColorPreset 
            { 
                theme = ColorTheme.Mor, 
                name = "Mor", 
                bodyColor = new Color(0.50f, 0.35f, 0.55f), 
                jointColor = new Color(0.14f, 0.14f, 0.14f)
            },
            new ColorPreset 
            { 
                theme = ColorTheme.Pembe, 
                name = "Pembe", 
                bodyColor = new Color(0.72f, 0.42f, 0.52f), 
                jointColor = new Color(0.14f, 0.14f, 0.14f)
            },
            new ColorPreset 
            { 
                theme = ColorTheme.Kahverengi, 
                name = "Kahverengi", 
                bodyColor = new Color(0.48f, 0.32f, 0.22f), 
                jointColor = new Color(0.14f, 0.14f, 0.14f)
            },
            new ColorPreset 
            { 
                theme = ColorTheme.Beyaz, 
                name = "Beyaz", 
                bodyColor = new Color(0.80f, 0.80f, 0.80f), 
                jointColor = new Color(0.16f, 0.16f, 0.16f)
            }
        };

        private static readonly int BaseColorHash = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorHash = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorHash = Shader.PropertyToID("_EmissionColor");
        private static readonly int SmoothnessHash = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicHash = Shader.PropertyToID("_Metallic");

        public ColorTheme ActiveTheme => activeTheme;
        public string ActiveThemeName => GetPreset(activeTheme).name;
        public Color ActiveThemeColor => GetPreset(activeTheme).bodyColor;
        public event Action<ColorTheme> ThemeChanged;

        private void Awake()
        {
            if (targetRenderers.Count == 0)
            {
                GetComponentsInChildren(true, targetRenderers);
            }
        }

        private void Start()
        {
            EnsureUrpMaterials();
            ApplyTheme(activeTheme);
        }

        private void EnsureUrpMaterials()
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null) urpLitShader = Shader.Find("Standard");

            if (urpLitShader == null) return;

            foreach (Renderer ren in targetRenderers)
            {
                if (ren == null) continue;

                Material[] mats = ren.materials; // Access instance materials
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].shader != urpLitShader)
                    {
                        mats[i].shader = urpLitShader;
                    }
                }
            }
        }

        public void ApplyTheme(ColorTheme theme)
        {
            bool changed = activeTheme != theme;
            activeTheme = theme;
            ColorPreset preset = GetPreset(theme);

            if (targetRenderers == null || targetRenderers.Count == 0)
            {
                targetRenderers = new List<Renderer>();
                GetComponentsInChildren(true, targetRenderers);
            }

            Color eyesColor = eyeMode == EyeColorMode.SleekBlack ? new Color(0.04f, 0.04f, 0.05f) : preset.bodyColor;

            foreach (Renderer ren in targetRenderers)
            {
                if (ren == null) continue;

                Material[] mats = ren.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null) continue;

                    string matName = mat.name;

                    if (matName.Contains("M_AtlasOffset") || matName.Contains("Offset")) // Main Robot Armor & Body
                    {
                        mat.SetColor(BaseColorHash, preset.bodyColor);
                        mat.SetColor(ColorHash, preset.bodyColor);
                        if (mat.HasProperty(EmissionColorHash)) mat.SetColor(EmissionColorHash, Color.black);
                        if (mat.HasProperty(SmoothnessHash)) mat.SetFloat(SmoothnessHash, 0.35f);
                        if (mat.HasProperty(MetallicHash)) mat.SetFloat(MetallicHash, 0.15f);
                    }
                    else if (matName.Contains("M_AtlasBase") || matName.Contains("Base")) // Joint Connections
                    {
                        mat.SetColor(BaseColorHash, preset.jointColor);
                        mat.SetColor(ColorHash, preset.jointColor);
                        if (mat.HasProperty(EmissionColorHash)) mat.SetColor(EmissionColorHash, Color.black);
                        if (mat.HasProperty(SmoothnessHash)) mat.SetFloat(SmoothnessHash, 0.5f);
                        if (mat.HasProperty(MetallicHash)) mat.SetFloat(MetallicHash, 0.7f);
                    }
                    else if (matName.Contains("M_AtlasEmissive") || matName.Contains("Emissive")) // Eyes (Sleek Black)
                    {
                        mat.SetColor(BaseColorHash, eyesColor);
                        mat.SetColor(ColorHash, eyesColor);
                        if (mat.HasProperty(EmissionColorHash)) mat.SetColor(EmissionColorHash, Color.black);
                        if (mat.HasProperty(SmoothnessHash)) mat.SetFloat(SmoothnessHash, 0.9f);
                    }
                    else
                    {
                        mat.SetColor(BaseColorHash, preset.bodyColor);
                        mat.SetColor(ColorHash, preset.bodyColor);
                    }
                }
            }
            if (changed) ThemeChanged?.Invoke(activeTheme);
        }

        public void NextTheme()
        {
            int totalThemes = Enum.GetValues(typeof(ColorTheme)).Length;
            int nextIndex = ((int)activeTheme + 1) % totalThemes;
            ApplyTheme((ColorTheme)nextIndex);
        }

        private ColorPreset GetPreset(ColorTheme theme)
        {
            foreach (var p in presets)
            {
                if (p.theme == theme) return p;
            }
            return presets[0];
        }
    }
}
