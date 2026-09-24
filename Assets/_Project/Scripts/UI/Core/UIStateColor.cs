using System;
using UnityEngine;

namespace Robot.UI.Core
{
    /// <summary>
    /// UI State types defining semantic meaning and color schemes across the Robot Hunt UI framework.
    /// </summary>
    public enum UIStateType
    {
        Grey,    // Neutral / Default
        Blue,    // Active / Gameplay / Info
        Green,   // Success / Completed / Healthy
        Yellow,  // Caution / Attention / Warning
        Red      // Danger / Error / Critical / Close
    }

    /// <summary>
    /// Centralized color palette and styling constants for the Sci-Fi Robot Hunt Design System.
    /// </summary>
    public static class UIStateColor
    {
        // Semantic State Colors
        public static readonly Color Grey = new Color(0.42f, 0.48f, 0.55f, 1f);
        public static readonly Color Blue = new Color(0.18f, 0.62f, 0.95f, 1f);
        public static readonly Color Green = new Color(0.25f, 0.82f, 0.45f, 1f);
        public static readonly Color Yellow = new Color(0.94f, 0.72f, 0.10f, 1f);
        public static readonly Color Red = new Color(0.92f, 0.26f, 0.26f, 1f);

        // Highlight & Glow Variations
        public static readonly Color BlueGlow = new Color(0.40f, 0.78f, 1.0f, 0.9f);
        public static readonly Color GreenGlow = new Color(0.45f, 0.95f, 0.60f, 0.9f);
        public static readonly Color YellowGlow = new Color(0.95f, 0.75f, 0.35f, 0.9f);
        public static readonly Color RedGlow = new Color(1.0f, 0.40f, 0.40f, 0.9f);

        // Glassmorphism & Panel Backgrounds
        public static readonly Color GlassDark = new Color(0.05f, 0.08f, 0.12f, 0.92f);
        public static readonly Color GlassMedium = new Color(0.08f, 0.13f, 0.19f, 0.85f);
        public static readonly Color GlassLight = new Color(0.12f, 0.18f, 0.26f, 0.75f);
        public static readonly Color GlassModalOverlay = new Color(0.02f, 0.04f, 0.07f, 0.75f);

        // Text Colors
        public static readonly Color TextPrimary = new Color(0.96f, 0.98f, 1.0f, 1f);
        public static readonly Color TextSecondary = new Color(0.68f, 0.76f, 0.86f, 1f);
        public static readonly Color TextMuted = new Color(0.44f, 0.52f, 0.62f, 1f);
        public static readonly Color TextDisabled = new Color(0.35f, 0.40f, 0.48f, 0.6f);

        // Border & Accent Colors
        public static readonly Color BorderSubtle = new Color(0.24f, 0.36f, 0.50f, 0.6f);
        public static readonly Color BorderBright = new Color(0.40f, 0.65f, 0.90f, 0.9f);

        /// <summary>
        /// Retrieves the primary color corresponding to a given UI state.
        /// </summary>
        public static Color GetColor(UIStateType state)
        {
            switch (state)
            {
                case UIStateType.Blue: return Blue;
                case UIStateType.Green: return Green;
                case UIStateType.Yellow: return Yellow;
                case UIStateType.Red: return Red;
                case UIStateType.Grey:
                default: return Grey;
            }
        }

        /// <summary>
        /// Retrieves the glow/accent color corresponding to a given UI state.
        /// </summary>
        public static Color GetGlowColor(UIStateType state)
        {
            switch (state)
            {
                case UIStateType.Blue: return BlueGlow;
                case UIStateType.Green: return GreenGlow;
                case UIStateType.Yellow: return YellowGlow;
                case UIStateType.Red: return RedGlow;
                case UIStateType.Grey:
                default: return TextSecondary;
            }
        }

        /// <summary>
        /// Converts a Color into hexadecimal string for rich text tagging.
        /// </summary>
        public static string ToHex(Color color)
        {
            Color32 c = color;
            return $"{c.r:X2}{c.g:X2}{c.b:X2}";
        }
    }
}
