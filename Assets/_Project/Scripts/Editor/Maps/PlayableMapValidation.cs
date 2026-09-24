#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayableMapValidation
{
    private static double deadline;
    static PlayableMapValidation()
    {
        deadline = EditorApplication.timeSinceStartup + 900;
        EditorApplication.update += () =>
        {
            if (!SessionState.GetBool("PlayableMaps.Validation", false)) return;
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            if (EditorApplication.timeSinceStartup > deadline)
            {
                Debug.LogError("MAP VALIDATION: editor watchdog timeout");
                SessionState.SetBool("PlayableMaps.Validation", false);
                EditorApplication.Exit(1);
            }
        };
    }
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/RoboSeek_Lobby.unity");
        new GameObject("Playable Map Validation").AddComponent<MapPlayValidationProbe>();
        SessionState.SetBool("PlayableMaps.Validation", true);
        EditorApplication.isPaused = false;
        EditorApplication.EnterPlaymode();
    }
}
#endif
