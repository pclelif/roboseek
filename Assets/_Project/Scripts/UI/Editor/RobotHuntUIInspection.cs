#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class RobotHuntUIInspection
{
    public const string ScenePath = "Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity";
    public static void Inspect()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var report = new StringBuilder();
        foreach (var root in scene.GetRootGameObjects())
        {
            report.AppendLine("ROOT " + root.name);
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) { report.AppendLine("MISSING SCRIPT under " + root.name); continue; }
                if (!(component is MonoBehaviour) && !(component is Canvas) && !(component is AudioSource)) continue;
                report.AppendLine(component.gameObject.name + " : " + component.GetType().FullName);
                if (component.GetType().Namespace != null && component.GetType().Namespace.StartsWith("Robot"))
                    report.AppendLine(EditorJsonUtility.ToJson(component));
            }
        }
        File.WriteAllText("Documentation/GameplaySceneInspection.txt", report.ToString());
        Debug.Log("ROBOT_HUNT_INSPECTION_COMPLETE");
    }
}
#endif
