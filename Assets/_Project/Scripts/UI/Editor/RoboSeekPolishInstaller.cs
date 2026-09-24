#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Robot.UI.Production;
using Robot.Player.Movement;
public static class RoboSeekPolishInstaller
{
    [MenuItem("RoboSeek/Update Lobby Scene")]
    public static void UpdateLobbyScene()
    {
        BakeLobbyOnly();
        var camera = Camera.main;
        if (camera != null)
        {
            var target = new RenderTexture(1280, 720, 24);
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes("/private/tmp/RoboSeek_Lobby_preview.png", texture.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            Object.DestroyImmediate(target); Object.DestroyImmediate(texture);
            Debug.Log("[RoboSeek Lobby] Preview captured in UpdateLobbyScene.");
        }
        Debug.Log("Lobby scene updated successfully!");
    }
    public static void BakeAndRun() { Bake(); RoboSeekRevisionChecks.Run(); }
    public static void Inspect()
    {
        EditorSceneManager.OpenScene(RobotHuntUIInspection.ScenePath);
        var lines = new System.Collections.Generic.List<string>();
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var rs = root.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            lines.Add(root.name + " " + b);
        }
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if (r.name.ToLower().Contains("water") || r.bounds.size.x > 100 || r.bounds.size.z > 100) lines.Add("SURFACE " + r.name + " " + r.bounds);
        var player = Object.FindFirstObjectByType<RobotMovementController>();
        lines.Add("PLAYER " + player.transform.position);
        File.WriteAllLines("/private/tmp/roboseek-world-inspect.txt", lines);
    }
    public static void Bake()
    {
        if (Application.isPlaying) return;
        BakeLobbyOnly();
        EditorSceneManager.OpenScene(RobotHuntUIInspection.ScenePath);
        var root = Object.FindFirstObjectByType<UIRootController>();
        BuildSafety(); BakePose(root); root.RefineGameplayUI(); root.GetComponent<TargetHUD>().ApplyLayout(); root.state.result.ApplyLayout(); root.ApplyTypography();
        EditorSceneManager.SaveScene(root.gameObject.scene);
        AssetDatabase.SaveAssets();
        Inspect();
    }
    
    public static void BakeLobbyOnly()
    {
        if (!Application.isPlaying)
        {
            EditorSceneManager.OpenScene(RoboSeekLobbyInstaller.LobbyScenePath);
        }
        var root = Object.FindFirstObjectByType<UIRootController>();
        if (root == null) return;
        var theme = root.GetComponent<LobbyRuntimeTheme>() ?? root.gameObject.AddComponent<LobbyRuntimeTheme>();
        theme.Apply();

        // --- Editor Bake: apply saved color + Jump_Air pose so scene view matches runtime ---
        if (!Application.isPlaying)
        {
            var platform = GameObject.Find("RobotShowcasePlatform");
            var customizer = platform?.GetComponentInChildren<Robot.Robots.Customization.RobotColorCustomizer>();
            if (customizer != null)
            {
                // Sarı (yellow) — editor preview color
                customizer.ApplyTheme(Robot.Robots.Customization.RobotColorCustomizer.ColorTheme.Sari);

                // Sample Jump_Air so scene view shows the hover stance without pressing Play
                var editorAnimator = customizer.GetComponentInChildren<Animator>();
                if (editorAnimator != null && editorAnimator.runtimeAnimatorController != null)
                {
                    var clips = editorAnimator.runtimeAnimatorController.animationClips;
                    var hoverClip = System.Array.Find(clips, c => c != null && c.name.Contains("Jump_Air"));
                    if (hoverClip != null)
                        hoverClip.SampleAnimation(editorAnimator.gameObject, hoverClip.length * 0.5f);
                }
                EditorUtility.SetDirty(platform);
            }
        }

        // Save soft warm pastel yellow background texture asset (PNG)
        string folder = "Assets/UI/Resources/RobotHuntUI";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        string texPath = folder + "/PolishBackdrop.png";

        Texture2D backdropTex = new Texture2D(512, 256, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        Color centerYellow = new Color(0.99f, 0.90f, 0.54f);
        Color edgeGold     = new Color(0.95f, 0.74f, 0.32f);

        var pixels = new Color[512 * 256];
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 512; x++)
            {
                float u = x / 511f;
                float v = y / 255f;
                float dx = (u - 0.45f) * 1.4f;
                float dy = (v - 0.55f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float centerGlow = Mathf.Clamp01(1f - (dist / 0.72f));
                centerGlow = Mathf.SmoothStep(0f, 1f, centerGlow);
                Color c = Color.Lerp(edgeGold, centerYellow, centerGlow);
                float verticalLight = Mathf.Lerp(0.92f, 1.05f, v);
                pixels[y * 512 + x] = c * verticalLight;
            }
        }
        backdropTex.SetPixels(pixels);
        backdropTex.Apply();
        byte[] pngBytes = backdropTex.EncodeToPNG();
        File.WriteAllBytes(texPath, pngBytes);
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
        Object.DestroyImmediate(backdropTex);

        var savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        string matPath = folder + "/PolishRearWallCore.mat";
        var savedMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
        if (savedMat == null)
        {
            savedMat = new Material(unlitShader) { name = "PolishRearWallCore" };
            AssetDatabase.CreateAsset(savedMat, matPath);
        }
        savedMat.shader = unlitShader;
        if (savedMat.HasProperty("_BaseMap")) savedMat.SetTexture("_BaseMap", savedTex);
        if (savedMat.HasProperty("_MainTex")) savedMat.SetTexture("_MainTex", savedTex);
        if (savedMat.HasProperty("_BaseColor")) savedMat.SetColor("_BaseColor", Color.white);
        if (savedMat.HasProperty("_Color")) savedMat.SetColor("_Color", Color.white);
        savedMat.mainTexture = savedTex;
        EditorUtility.SetDirty(savedMat);

        var wallGO = GameObject.Find("RearWallCore");
        if (wallGO != null)
        {
            wallGO.transform.position = new Vector3(-0.90f, 1.85f, 2.7f);
            wallGO.transform.localScale = new Vector3(16f, 9f, .2f);
            wallGO.GetComponent<Renderer>().sharedMaterial = savedMat;
        }

        Decorate(root.gameObject.scene); root.hub.Refresh(); root.ApplyTypography();
        if (!Application.isPlaying)
        {
            EditorSceneManager.SaveScene(root.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
    }
    private static void BakePose(UIRootController root)
    {
        const string folder = "Assets/UI/Resources/RobotHuntUI/ResultPose";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/UI/Resources/RobotHuntUI", "ResultPose");
        var original = root.hub.showcase.robot.gameObject;
        var copy = Object.Instantiate(original); copy.name = "PoseSample";
        var animator = copy.GetComponentInChildren<Animator>();
        var clip = animator.runtimeAnimatorController.animationClips.First(c => c.name.Contains("Jump_Air"));
        clip.SampleAnimation(animator.gameObject, clip.length * .5f);
        var pose = new GameObject("ResultHoverPose");
        int index = 0;
        foreach (var renderer in copy.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skin)
            {
                mesh = new Mesh(); skin.BakeMesh(mesh);
                string path = folder + "/Part" + index++ + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing != null) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
                else AssetDatabase.CreateAsset(mesh, path);
            }
            else mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) continue;
            var part = new GameObject(renderer.name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(pose.transform);
            part.transform.localPosition = copy.transform.InverseTransformPoint(renderer.transform.position);
            part.transform.localRotation = Quaternion.Inverse(copy.transform.rotation) * renderer.transform.rotation;
            var a = renderer.transform.lossyScale; var b = copy.transform.lossyScale;
            part.transform.localScale = new Vector3(a.x/b.x,a.y/b.y,a.z/b.z);
            part.GetComponent<MeshFilter>().sharedMesh = mesh; part.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
        }
        root.hub.showcase.resultPose = PrefabUtility.SaveAsPrefabAsset(pose, folder + "/ResultHoverPose.prefab");
        Object.DestroyImmediate(pose); Object.DestroyImmediate(copy);
    }
    private static void BuildSafety()
    {
        var ocean = GameObject.Find("SM_Env_Ocean_Tile_01");
        var renderers = ocean.GetComponentsInChildren<Renderer>();
        Bounds oceanBounds = renderers[0].bounds; foreach (var r in renderers) oceanBounds.Encapsulate(r.bounds);
        var player = Object.FindFirstObjectByType<RobotMovementController>();
        var safety = player.GetComponent<WorldSafety>() ?? player.gameObject.AddComponent<WorldSafety>();
        safety.waterHeight = oceanBounds.center.y;
        safety.limits = new Bounds(new Vector3(oceanBounds.center.x,40,oceanBounds.center.z), new Vector3(oceanBounds.size.x-4,104,oceanBounds.size.z-4));
        if (GameObject.Find("WorldBoundary") == null)
        {
            var boundary = new GameObject("WorldBoundary"); var b = safety.limits;
            Wall(boundary.transform,"West",new Vector3(b.min.x,b.center.y,b.center.z),new Vector3(2,b.size.y,b.size.z));
            Wall(boundary.transform,"East",new Vector3(b.max.x,b.center.y,b.center.z),new Vector3(2,b.size.y,b.size.z));
            Wall(boundary.transform,"South",new Vector3(b.center.x,b.center.y,b.min.z),new Vector3(b.size.x,b.size.y,2));
            Wall(boundary.transform,"North",new Vector3(b.center.x,b.center.y,b.max.z),new Vector3(b.size.x,b.size.y,2));
            Wall(boundary.transform,"Seabed",new Vector3(b.center.x,b.min.y,b.center.z),new Vector3(b.size.x,2,b.size.z));
        }
        int repaired=0;
        foreach (var mesh in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            string n = mesh.name;
            bool solid = n.StartsWith("SM_Bld_") || n.StartsWith("SM_Prop_") || n.StartsWith("SM_Veh_") || n.StartsWith("SM_Env_WaterEdge") || n.StartsWith("SM_Env_Road");
            if (!solid || mesh.GetComponent<Collider>() != null || mesh.GetComponentInParent<Collider>() != null || mesh.GetComponentInParent<Rigidbody>() != null || mesh.sharedMesh == null) continue;
            mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh; repaired++;
        }
        Debug.Log("POLISH: repaired static mesh colliders: " + repaired + "; bounds " + safety.limits);
    }
    private static void Wall(Transform parent,string name,Vector3 position,Vector3 size)
    {
        var wall = new GameObject(name,typeof(BoxCollider)); wall.transform.SetParent(parent); wall.transform.position=position; wall.GetComponent<BoxCollider>().size=size;
    }
    private static void Decorate(UnityEngine.SceneManagement.Scene scene)
    {
        if (!scene.IsValid()) scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "LobbyToyCollection" || go.name.Contains("Bookcase") || go.name.Contains("Bookshelf"))
                Object.DestroyImmediate(go);
        }

        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t != null && t.gameObject != null && (t.name == "Layered_Back_Wall" || t.name.StartsWith("FloorPanel_") || t.name.StartsWith("TechnicalLabel_") || t.name == "BackdropFill") && t.gameObject.scene == scene)
            {
                if (!toDestroy.Contains(t.gameObject)) toDestroy.Add(t.gameObject);
            }
        }
        foreach (var go in toDestroy)
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        // Expand floor on the bottom-left so no floor edges or gaps are visible in camera angle
        var floor = GameObject.Find("IndustrialFloor");
        if (floor != null)
        {
            floor.transform.position = new Vector3(-1.10f, -0.51f, 2.0f);
            floor.transform.localScale = new Vector3(14.0f, 0.18f, 10.0f);
        }

        var collection = new GameObject("LobbyToyCollection");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(collection, scene);

        var floatingToys = new[]
        {
            // Reverted to loved layout (from 2 prompts ago) with unscaled time floating effect
            // Left cluster:
            new { name = "Prop_TeddyBear_01", pos = new Vector3(-2.70f, 1.15f, 0.35f), rot = new Vector3(15f, 150f, -12f), size = 0.25f, phase = 0.0f },
            new { name = "Prop_Ball_01",      pos = new Vector3(-2.32f, 1.32f, 0.25f), rot = new Vector3(-10f, 135f, 15f), size = 0.25f, phase = 0.5f },
            new { name = "Prop_ToyCar_Blue",   pos = new Vector3(-2.85f, 0.72f, 0.30f), rot = new Vector3(8f, 175f, -10f), size = 0.25f, phase = 1.0f },
            new { name = "Prop_TeddyBear_02", pos = new Vector3(-2.68f, 0.44f, 0.20f), rot = new Vector3(-12f, 190f, 10f), size = 0.25f, phase = 1.5f },
            new { name = "Prop_Ball_02",      pos = new Vector3(-2.80f, 0.05f, 0.25f), rot = new Vector3(0f, 110f, -18f), size = 0.25f, phase = 2.0f },
            new { name = "Prop_ToyCar_Yellow", pos = new Vector3(-2.28f, 0.04f, 0.15f), rot = new Vector3(14f, 160f, 8f), size = 0.25f, phase = 2.5f },

            // Right cluster (micro-shifted slightly left):
            new { name = "Prop_TeddyBear_03", pos = new Vector3(-1.07f, 1.30f, 0.30f), rot = new Vector3(-8f, 170f, 6f), size = 0.26f, phase = 3.0f },
            new { name = "Prop_Ball_03",      pos = new Vector3(-0.72f, 1.10f, 0.35f), rot = new Vector3(0f, 200f, -12f), size = 0.25f, phase = 3.5f },
            new { name = "Prop_ToyCar_Red",    pos = new Vector3(-0.97f, 0.70f, 0.25f), rot = new Vector3(-15f, 185f, -8f), size = 0.25f, phase = 4.0f },
            new { name = "Prop_TeddyBear_04", pos = new Vector3(-0.77f, 0.42f, 0.20f), rot = new Vector3(10f, 165f, -14f), size = 0.24f, phase = 4.5f },
            new { name = "Prop_Ball_04",      pos = new Vector3(-1.07f, 0.10f, 0.20f), rot = new Vector3(0f, 90f, 15f), size = 0.25f, phase = 5.0f },
            new { name = "Prop_ToyCar_Green",  pos = new Vector3(-0.77f, 0.08f, 0.15f), rot = new Vector3(8f, 195f, 12f), size = 0.24f, phase = 5.5f }
        };

        foreach (var spec in floatingToys)
        {
            string prefabPath = "Assets/ThirdParty/Selected/toy/" + spec.name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            var toy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, collection.transform);
            foreach (var c in toy.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            var renderers = toy.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            toy.transform.localScale *= spec.size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            toy.transform.rotation = Quaternion.Euler(spec.rot);

            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            toy.transform.position += spec.pos - bounds.center;

            var floatAnim = toy.GetComponent<LobbyFloatingToy>() ?? toy.AddComponent<LobbyFloatingToy>();
            floatAnim.SetAnchor(toy.transform.position, spec.rot);
            floatAnim.phaseOffset = spec.phase;
            floatAnim.bobAmplitude = 0.05f;
            floatAnim.bobSpeed = 1.4f;
            floatAnim.tiltAmplitude = 3.0f;
            floatAnim.tiltSpeed = 1.0f;
        }
    }
}
#endif
