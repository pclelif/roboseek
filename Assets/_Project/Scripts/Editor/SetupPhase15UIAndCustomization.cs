using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Robot.UI.HUD;
using Robot.UI.Settings;
using Robot.Robots.Customization;
using Robot.Score;

namespace Robot.Editor
{
    public static class SetupPhase15UIAndCustomization
    {
        private const string DemoScenePath = "Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity";
        private const string Demo2ScenePath = "Assets/_Project/Scenes/Demo2.unity";

        [MenuItem("Tools/Robot Hunt/Setup Full UI & Customization (Phase 15-16)", false, 5)]
        public static void SetupFullUI()
        {
            SetupSceneUI(DemoScenePath, false);
            SetupSceneUI(Demo2ScenePath, true);
            Debug.Log("[Phase 15-16] Full UI, Customization, Settings, HUD & Mobile Controls successfully setup for both Singleplayer & Multiplayer!");
        }

        private static void SetupSceneUI(string scenePath, bool isMultiplayer)
        {
            if (!File.Exists(scenePath)) return;
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            // 1. Ensure SettingsManager
            if (UnityEngine.Object.FindFirstObjectByType<SettingsManager>() == null)
            {
                GameObject settingsObj = new GameObject("SettingsManager");
                settingsObj.AddComponent<SettingsManager>();
            }

            // 2. Ensure UI Canvas Root
            GameObject uiRoot = GameObject.Find("UI_System_Root");
            if (uiRoot == null) uiRoot = new GameObject("UI_System_Root");

            if (uiRoot.GetComponent<SettingsUI>() == null)
                uiRoot.AddComponent<SettingsUI>();

            if (uiRoot.GetComponent<PauseMenuUI>() == null)
                uiRoot.AddComponent<PauseMenuUI>();

            if (uiRoot.GetComponent<CombatHealthHUD>() == null)
                uiRoot.AddComponent<CombatHealthHUD>();

            if (uiRoot.GetComponent<MobileControlsHUD>() == null)
                uiRoot.AddComponent<MobileControlsHUD>();

            if (uiRoot.GetComponent<RobotCustomizationUI>() == null)
                uiRoot.AddComponent<RobotCustomizationUI>();

            EditorUtility.SetDirty(uiRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
