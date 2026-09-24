#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class RobotHuntUIPlayTests
{
    private const string Key = "RobotHunt.UI.PlayTests";
    static RobotHuntUIPlayTests()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
            {
                SessionState.SetBool(Key, false);
                new GameObject("Production UI Integration Tests").AddComponent<Robot.UI.Production.RobotHuntUIPlayProbe>();
            }
        };
    }
    public static void Begin()
    {
        EditorSceneManager.OpenScene(RobotHuntUIInspection.ScenePath);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
}
#endif
