#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Robot.Player.Movement;
using Robot.Robots.Customization;

public static class RoboSeekCriticalFixes
{
    [MenuItem("Tools/RoboSeek/Apply Critical Physics Fixes")]
    public static void Apply()
    {
        var report = new List<string> { "# Scene collision audit", "Each mesh below was checked for an enabled, non-trigger collider on itself or its parent." };
        foreach (var path in new[] { "Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity", "Assets/_Project/Scenes/RoboSeek_Lobby.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path);
            bool gameplay = path.EndsWith("Demo.unity");
            if (gameplay)
            {
                foreach (var mesh in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (mesh.sharedMesh == null || mesh.GetComponentInParent<Canvas>() != null || mesh.GetComponentInParent<RobotColorCustomizer>() != null) continue;
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(mesh);
                    string asset = source != null ? AssetDatabase.GetAssetPath(source) : "scene-authored";
                    // Water, sky and UI decorations are deliberately not walking surfaces.
                    string name = mesh.name.ToLowerInvariant();
                    if (name.Contains("water") || name.Contains("sky") || name.Contains("cloud") || name.Contains("effect")) continue;
                    WorldSafety.AddMissingSolidColliders(mesh.gameObject);
                    var solid = false;
                    foreach (var col in mesh.GetComponentsInParent<Collider>()) if (col.enabled && !col.isTrigger) solid = true;
                    report.Add((solid ? "PASS " : "FAIL ") + Hierarchy(mesh.transform) + " | " + asset);
                }
            }
            foreach (var robot in Object.FindObjectsByType<RobotColorCustomizer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (gameplay && !robot.CompareTag("Player")) continue;
                var serialized = new SerializedObject(robot);
                serialized.FindProperty("activeTheme").enumValueIndex = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                // Persist dedicated materials, so Scene view is yellow before any script runs.
                foreach (var renderer in robot.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var source = materials[i];
                        if (source == null) continue;
                        string role = source.name.Contains("Emissive") ? "Emissive" : source.name.Contains("Base") && !source.name.Contains("Offset") ? "Base" : "Offset";
                        string materialPath = "Assets/_Project/Materials/RobotPreview_" + role + ".mat";
                        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material == null) { material = new Material(source); AssetDatabase.CreateAsset(material, materialPath); }
                        material.shader = Shader.Find("Universal Render Pipeline/Lit");
                        Color color = role == "Offset" ? new Color(.82f,.70f,.40f) : role == "Base" ? new Color(.14f,.14f,.14f) : new Color(.04f,.04f,.05f);
                        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                        material.DisableKeyword("_EMISSION");
                        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
                        if (role == "Emissive")
                        {
                            material.SetTexture("_BaseMap", null);
                            material.SetTexture("_EmissionMap", null);
                            material.SetFloat("_Metallic",0);
                            material.SetFloat("_Smoothness",.35f);
                        }
                        EditorUtility.SetDirty(material);
                        materials[i] = material;
                    }
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                }
            }
            foreach (var controls in Object.FindObjectsByType<Robot.UI.Production.ControlsPanelController>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            { controls.EnsureHealingRow(); EditorUtility.SetDirty(controls); }
            var deck = GameObject.Find("RightControlDeck")?.GetComponent<RectTransform>();
            if (deck != null) deck.anchoredPosition = new Vector2(535,75);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Documentation");
        File.WriteAllLines("Documentation/CollisionAudit.md",report);
        Debug.Log("CRITICAL_FIXES_APPLIED entries=" + report.Count);
    }
    public static void ApplyFinalTest() { Apply(); FinalTest(); }
    public static void FinalTest()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/RoboSeek_Lobby.unity");
        var yellow = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/RobotPreview_Offset.mat");
        var eyes = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/RobotPreview_Emissive.mat");
        if (yellow.GetColor("_BaseColor") != new Color(.82f,.70f,.40f) || eyes.GetColor("_EmissionColor") != Color.black || eyes.IsKeywordEnabled("_EMISSION"))
            throw new System.Exception("Editor robot material mismatch yellow="+yellow.GetColor("_BaseColor")+" eyes="+eyes.GetColor("_EmissionColor")+" keyword="+eyes.IsKeywordEnabled("_EMISSION"));
        new GameObject("Final Polish Probe").AddComponent<FinalPolishProbe>();
        EditorApplication.isPaused = false;
        EditorApplication.EnterPlaymode();
    }
    public static void ApplyAndTest() { Apply(); Test(); }
    public static void Test()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/RoboSeek_Lobby.unity");
        new GameObject("Critical Fixes Probe").AddComponent<CriticalFixesProbe>();
        EditorApplication.isPaused = false;
        EditorApplication.EnterPlaymode();
    }
    private static string Hierarchy(Transform t) => t.parent == null ? t.name : Hierarchy(t.parent) + "/" + t.name;
}
#endif
