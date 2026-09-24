using System;
using System.Collections.Generic;
using UnityEngine;
namespace Robot.UI.Production
{
    public sealed class GraphicsSettingsController : MonoBehaviour
    {
        private const string Prefix = "RobotHunt.Graphics.";
        public bool SupportsDisplay => Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer;
        public bool SupportsVSync => SupportsDisplay || Application.isEditor;
        public readonly List<Vector2Int> Resolutions = new List<Vector2Int>();
        public readonly List<FullScreenMode> Modes = new List<FullScreenMode>();
        public int ResolutionIndex { get; private set; }
        public int ModeIndex { get; private set; }
        public string[] QualityNames => QualitySettings.names;
        public event Action Changed;
        public void Initialize()
        {
            if (SupportsDisplay)
            {
                Modes.Add(FullScreenMode.Windowed); Modes.Add(FullScreenMode.FullScreenWindow);
                if (Application.platform == RuntimePlatform.WindowsPlayer) Modes.Add(FullScreenMode.ExclusiveFullScreen);
                foreach (var resolution in Screen.resolutions)
                {
                    var size = new Vector2Int(resolution.width, resolution.height);
                    if (!Resolutions.Contains(size)) Resolutions.Add(size);
                }
                var current = new Vector2Int(Screen.width, Screen.height);
                if (!Resolutions.Contains(current)) Resolutions.Add(current);
                var saved = new Vector2Int(PlayerPrefs.GetInt(Prefix + "Width", current.x), PlayerPrefs.GetInt(Prefix + "Height", current.y));
                ResolutionIndex = Mathf.Max(0, Resolutions.IndexOf(Resolutions.Contains(saved) ? saved : current));
                var mode = (FullScreenMode)PlayerPrefs.GetInt(Prefix + "Mode", (int)Screen.fullScreenMode);
                ModeIndex = Mathf.Max(0, Modes.IndexOf(mode));
                if (PlayerPrefs.HasKey(Prefix + "Mode") || PlayerPrefs.HasKey(Prefix + "Width")) ApplyDisplay();
            }
            if (PlayerPrefs.HasKey(Prefix + "Quality") && QualityNames.Length > 0)
                QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality"), 0, QualityNames.Length - 1));
            if (SupportsVSync && PlayerPrefs.HasKey(Prefix + "VSync")) QualitySettings.vSyncCount = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "VSync"), 0, 1);
        }
        public void NextDisplayMode() { if (!SupportsDisplay) return; ModeIndex = (ModeIndex + 1) % Modes.Count; ApplyDisplay(); }
        public void NextResolution() { if (!SupportsDisplay || Resolutions.Count == 0) return; ResolutionIndex = (ResolutionIndex + 1) % Resolutions.Count; ApplyDisplay(); }
        private void ApplyDisplay()
        {
            var size = Resolutions[ResolutionIndex]; var mode = Modes[ModeIndex];
            Screen.SetResolution(size.x, size.y, mode);
            PlayerPrefs.SetInt(Prefix + "Width", size.x); PlayerPrefs.SetInt(Prefix + "Height", size.y); PlayerPrefs.SetInt(Prefix + "Mode", (int)mode); Save();
        }
        public void NextQuality()
        {
            if (QualityNames.Length < 2) return;
            int quality = (QualitySettings.GetQualityLevel() + 1) % QualityNames.Length;
            int vsync = QualitySettings.vSyncCount; QualitySettings.SetQualityLevel(quality); QualitySettings.vSyncCount = vsync;
            PlayerPrefs.SetInt(Prefix + "Quality", quality); Save();
        }
        public void ToggleVSync()
        {
            if (!SupportsVSync) return;
            QualitySettings.vSyncCount = QualitySettings.vSyncCount == 0 ? 1 : 0;
            PlayerPrefs.SetInt(Prefix + "VSync", QualitySettings.vSyncCount); Save();
        }
        private void Save() { PlayerPrefs.Save(); Changed?.Invoke(); }
        public string ModeLabel => Modes.Count == 0 ? "" : Modes[ModeIndex] == FullScreenMode.Windowed ? "WINDOWED" : Modes[ModeIndex] == FullScreenMode.FullScreenWindow ? "BORDERLESS" : "FULLSCREEN";
    }
}
