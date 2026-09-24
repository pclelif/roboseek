using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Robot.EditorTools
{
    public static class MissingScriptCleaner
    {
        [MenuItem("Tools/RoboSeek/Clean Missing Scripts (Active Scene)", false, 100)]
        public static void CleanActiveScene()
        {
            int totalRemoved = 0;
            int totalGameObjects = 0;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject root in rootObjects)
                {
                    Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (Transform t in transforms)
                    {
                        totalGameObjects++;
                        int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                        totalRemoved += count;
                    }
                }
            }

            Debug.Log($"[MissingScriptCleaner] Cleaned {totalRemoved} missing script components across {totalGameObjects} GameObjects in open scene(s).");
            EditorUtility.DisplayDialog("Clean Missing Scripts", $"Found and removed {totalRemoved} missing script component(s).", "OK");
        }
    }
}
