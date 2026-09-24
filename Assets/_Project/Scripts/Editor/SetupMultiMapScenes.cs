#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Robot.Core;

namespace Robot.Editor
{
    [InitializeOnLoad]
    public static class SetupMultiMapScenes
    {
        static SetupMultiMapScenes()
        {
            EditorApplication.delayCall += RegisterMultiMapScenesInBuildSettings;
        }

        [MenuItem("Tools/Robot Hunt/Setup Multi-Map Build Settings", false, 10)]
        public static void RegisterMultiMapScenesInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            string[] mapScenePaths = new[]
            {
                "Assets/_Project/Scenes/RoboSeek_Lobby.unity",
                "Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity",
                "Assets/ThirdParty/SyntyStudios/PolygonAdventure/Scenes/Demo.unity",
                "Assets/ThirdParty/SyntyStudios/PolygonPrototype/Scenes/Demo.unity",
                "Assets/ThirdParty/SyntyStudios/PolygonStarter/Scenes/Demo.unity",
                "Assets/_Project/Scenes/Maps/RoboSeek_City.unity",
                "Assets/_Project/Scenes/Maps/RoboSeek_Adventure.unity",
                "Assets/_Project/Scenes/Maps/RoboSeek_Polygon.unity",
                "Assets/_Project/Scenes/Maps/RoboSeek_PolygonStarter.unity"
            };

            bool changed = false;
            foreach (string path in mapScenePaths)
            {
                if (!scenes.Exists(s => s.path == path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                    changed = true;
                }
            }

            if (changed)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("[SetupMultiMapScenes] Successfully registered multi-map scenes into EditorBuildSettings.");
            }
        }
    }
}
#endif
