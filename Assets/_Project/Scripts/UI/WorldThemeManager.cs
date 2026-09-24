using System;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.UI
{
    [Serializable]
    public struct WorldDefinition
    {
        public string sceneName;
        public string displayName;
        public string description;
        public string icon;
        public Color accentColor;
        public Color cardBgColor;

        public WorldDefinition(string sceneName, string displayName, string description, string icon, Color accentColor, Color cardBgColor)
        {
            this.sceneName = sceneName;
            this.displayName = displayName;
            this.description = description;
            this.icon = icon;
            this.accentColor = accentColor;
            this.cardBgColor = cardBgColor;
        }
    }

    public static class WorldThemeManager
    {
        public static readonly List<WorldDefinition> AvailableWorlds = BuildWorlds();

        private static List<WorldDefinition> BuildWorlds()
        {
            var result = new List<WorldDefinition>();
            foreach (var map in Robot.Core.MapManager.AllMaps)
            {
                string description = map.Traversal == Robot.Core.MapTraversal.Jetpack ? "Dikey platformlar • SPACE: Jetpack • Havada süzülme" :
                    map.Traversal == Robot.Core.MapTraversal.DoubleJump ? "Köy patikaları • SHIFT: Sprint • SPACE: Çift zıplama" :
                    map.Traversal == Robot.Core.MapTraversal.Dash ? "Kale sokakları • F: Dash • Surlara zıplama pedleri" :
                    "Şehir sokakları • Araçlar • Oyuncak avı";
                result.Add(new WorldDefinition(map.sceneName, map.displayName, description, "", map.themeColor,
                    Color.Lerp(new Color(.06f, .08f, .12f), map.themeColor, .18f)));
            }
            return result;
        }

        private static int selectedIndex = 0;

        public static event Action<WorldDefinition> OnWorldChanged;

        public static int SelectedIndex => selectedIndex;

        public static WorldDefinition CurrentWorld => AvailableWorlds[selectedIndex];

        public static Color CurrentAccent => CurrentWorld.accentColor;

        public static string SelectedSceneName => CurrentWorld.sceneName;

        public static void SelectWorld(int index)
        {
            if (index < 0 || index >= AvailableWorlds.Count) return;
            selectedIndex = index;
            Debug.Log($"[WorldThemeManager] Selected World: {CurrentWorld.displayName} ({CurrentWorld.sceneName}) | Accent Color: {CurrentAccent}");
            OnWorldChanged?.Invoke(CurrentWorld);
        }

        public static void SelectWorldByScene(string sceneName)
        {
            for (int i = 0; i < AvailableWorlds.Count; i++)
            {
                if (string.Equals(AvailableWorlds[i].sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    SelectWorld(i);
                    return;
                }
            }
        }
    }
}
