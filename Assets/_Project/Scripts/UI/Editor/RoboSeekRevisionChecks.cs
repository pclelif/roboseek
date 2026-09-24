#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class RoboSeekRevisionChecks
{
    static RoboSeekRevisionChecks()
    {
        EditorApplication.update += () =>
        {
            if (SessionState.GetBool("RoboSeek.RevisionActive", false) && EditorApplication.isPaused)
                EditorApplication.isPaused = false;
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(RoboSeekLobbyInstaller.LobbyScenePath);
        new GameObject("Revision checks").AddComponent<RoboSeekRevisionProbe>();
        Debug.Log("REVISION: Entering play mode");
        SessionState.SetBool("RoboSeek.RevisionActive", true);
        EditorApplication.isPaused = false;
        EditorApplication.EnterPlaymode();
    }
}
#endif
