#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Robot.ObjectHunt;
using Robot.Player.Movement;
using Robot.Player.CameraControl;
using Robot.Input;
using Robot.Combat;
using Robot.Robots.Customization;
using Robot.UI.Production;
using Robot.UI;

[InitializeOnLoad]
public static class RoboSeekLobbyInstaller
{
    public const string LobbyScenePath = "Assets/_Project/Scenes/RoboSeek_Lobby.unity";

    static RoboSeekLobbyInstaller()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(LobbyScenePath))
            {
                BuildLobbyScene();
            }
        };
    }

    [MenuItem("Robot Hunt/UI/Build Dedicated RoboSeek Lobby Scene", false, 1)]
    [MenuItem("Tools/RoboSeek/Build Dedicated RoboSeek Lobby Scene", false, 1)]
    public static void BuildLobbyScene()
    {
        string scenesDir = "Assets/_Project/Scenes";
        if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // The dedicated lobby ships with a single, intentional language baseline.
        // Players can still change it from the lobby control afterwards.

        // 1. Presentation camera. The UI remains a screen-space overlay; this only frames the stage.
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.20f, 0.18f, 0.16f, 1f);
        cam.fieldOfView = 39f;
        cam.transform.position = new Vector3(0.30f, 1.35f, -3.85f);
        cam.transform.rotation = Quaternion.Euler(8.0f, -14.0f, 0f);
        camObj.tag = "MainCamera";

        // 2. Soft, readable three-point lighting for the hero character.
        GameObject lightObj = new GameObject("Main Key Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.82f, 0.92f, 1.0f);
        light.intensity = 1.15f;
        lightObj.transform.rotation = Quaternion.Euler(38f, -34f, 0f);

        GameObject rimLightObj = new GameObject("Rim Light (Cyan)");
        Light rimLight = rimLightObj.AddComponent<Light>();
        rimLight.type = LightType.Directional;
        rimLight.color = new Color(0.30f, 0.62f, 0.82f);
        rimLight.intensity = 0.75f;
        rimLightObj.transform.rotation = Quaternion.Euler(-15f, 145f, 0f);

        GameObject amberLightObj = new GameObject("Accent Light (Amber)");
        Light amberLight = amberLightObj.AddComponent<Light>();
        amberLight.type = LightType.Directional;
        amberLight.color = new Color(1.0f, 0.58f, 0.25f);
        amberLight.intensity = 0.18f;
        amberLightObj.transform.rotation = Quaternion.Euler(28f, -140f, 0f);

        // 3. Dedicated turntable and the existing robot prefab.
        GameObject turntableObj = new GameObject("RobotShowcasePlatform");
        turntableObj.transform.position = new Vector3(-0.90f, 0f, 0f);
        BuildDeploymentEnvironment(turntableObj.transform);

        // Robot Model (Facing Camera at Quaternion.Euler(0, 160, 0))
        GameObject robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Robot/FreeLowPolyRobot/Meshes_and_Animations/RandomModularRobots_Prefab.prefab")
                             ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Robot/FreeLowPolyRobot/Meshes_and_Animations/Modular_Parts.prefab");
        RobotColorCustomizer customizer = null;
        if (robotPrefab != null)
        {
            GameObject robotInst = (GameObject)PrefabUtility.InstantiatePrefab(robotPrefab, turntableObj.transform);
            robotInst.transform.localPosition = Vector3.zero;
            robotInst.transform.localRotation = Quaternion.Euler(0f, 160f, 0f); // Hero front 3/4 pose facing player
            customizer = robotInst.GetComponent<RobotColorCustomizer>();
            if (customizer == null) customizer = robotInst.AddComponent<RobotColorCustomizer>();
        }

        // A lobby hero should present a dependable front silhouette.  The platform
        // remains physical, but no longer rotates the character into a side profile.
        turntableObj.AddComponent<LobbyAmbientMotion>().rotationSpeed = 0f;

        // 4. EventSystem
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<InputSystemUIInputModule>();

        // 5. Canvas & Production UI Root
        GameObject rootGO = new GameObject("Canvas_UI_Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = rootGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = rootGO.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;

        var root = rootGO.AddComponent<UIRootController>();
        root.localization = rootGO.AddComponent<UILocalization>();
        var state = rootGO.AddComponent<UIStateManager>();
        root.state = state;

        var visuals = rootGO.AddComponent<TargetVisualLibrary>();
        var robotVisualGO = new GameObject("RobotVisuals"); robotVisualGO.transform.SetParent(rootGO.transform, false);
        var robotVisuals = robotVisualGO.AddComponent<TargetVisualLibrary>();

        var screens = new GameObject[10]; state.screens = screens;

        // Build RoboSeek Header & Floating Right Control Deck Panel
        BuildRoboSeekHubUI(root, customizer, robotVisuals);
        SettingsScreenBuilder.Build(root, null, null);

        foreach (var screen in screens) if (screen != null) screen.SetActive(screen == screens[(int)UIScreen.Hub]);

        EditorSceneManager.SaveScene(scene, LobbyScenePath);
        Debug.Log("<color=green><b>[RoboSeek Lobby]</b> Dedicated RoboSeek Lobby Scene successfully created at: " + LobbyScenePath + "</color>");
    }

    [MenuItem("Tools/RoboSeek/Capture Lobby Preview", false, 2)]
    public static void CaptureLobbyPreview()
    {
        RoboSeekPolishInstaller.BakeLobbyOnly();
        var camera = Camera.main;
        if (camera == null) throw new InvalidOperationException("Lobby camera was not found.");
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
        File.WriteAllBytes("/private/tmp/RoboSeek_Lobby_preview.png", texture.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture);
        Debug.Log("[RoboSeek Lobby] Preview captured.");
    }

    private static void BuildRoboSeekHubUI(UIRootController root, RobotColorCustomizer robot, TargetVisualLibrary visuals)
    {
        // Screen Layer
        var layer = UIView.Screen("LobbyScreen", root.transform, 0.15f);
        root.state.screens[(int)UIScreen.Hub] = layer;

        // Floating right control deck. It stays clear of the showcase and uses a single
        // quiet surface so the robot, rather than the UI chrome, owns the composition.
        var panel = UIView.Panel("RightControlDeck", layer.transform, new Vector2(500, 590), new Color(.035f, .055f, .085f, .91f), new Vector2(500, -6));

        // Compact identity lock-up; no oversized mirrored background typography.
        var brandBanner = UIView.Panel("BrandHeaderBanner", layer.transform, new Vector2(440, 106), new Color(.025f, .043f, .070f, .72f), new Vector2(-470, 416));
        UIView.Label("Title", brandBanner.transform, "ROBOSEEK", 40, new Vector2(400, 48), new Vector2(0, 17), UITheme.TextWhite);
        UIView.Label("Subtitle", brandBanner.transform, "WHERE IS MY TOY?", 14, new Vector2(400, 24), new Vector2(0, -23), new Color(.34f, .72f, .86f, 1f));

        // Showcase Controller
        var showcase = root.GetComponent<RobotShowcaseController>();
        if (showcase == null) showcase = root.gameObject.AddComponent<RobotShowcaseController>();
        showcase.robot = robot; showcase.visuals = visuals;

        // Hub Controller
        root.hub = root.GetComponent<RobotHubController>();
        if (root.hub == null) root.hub = root.gameObject.AddComponent<RobotHubController>();
        root.hub.state = root.state; root.hub.robot = robot; root.hub.showcase = showcase;

        UIView.Label("DeckLabel", panel.transform, "LOBBY  //  UNIT 01", 11, new Vector2(430, 20), new Vector2(0, 250), new Color(.38f, .66f, .77f, .9f));
        UIView.Label("ModeHeader", panel.transform, "GAME MODE", 14, new Vector2(430, 24), new Vector2(0, 210), UITheme.TextMuted);

        // Single-Choice Toggle Game Mode Buttons
        root.hubSolo = UIView.Button("SOLO", panel.transform, new Vector2(-110, 164), new Vector2(210, 46));
        root.hubMultiplayer = UIView.Button("MULTIPLAYER", panel.transform, new Vector2(110, 164), new Vector2(210, 46));
        root.hub.soloButton = root.hubSolo;
        root.hub.multiplayerButton = root.hubMultiplayer;

        // Game Mode Description Text Box
        var descBox = UIView.Panel("ModeDescBox", panel.transform, new Vector2(430, 42), new Color(.025f, .043f, .067f, .72f), new Vector2(0, 108));
        root.hub.modeDescription = UIView.Label("ModeDescription", descBox.transform, "SELECT A GAME MODE TO BEGIN.", 12, new Vector2(408, 38), Vector2.zero, UITheme.TextMuted);

        // The one primary action deliberately has more height and contrast.
        root.startGame = UIView.Button("START GAME", panel.transform, new Vector2(0, 36), new Vector2(430, 60));
        root.hub.startGameButton = root.startGame;

        // Colour is an actual selectable component; its label always reflects the
        // existing RobotColorCustomizer selection.
        UIView.Label("ColorHeader", panel.transform, "ROBOT COLOR", 14, new Vector2(430, 22), new Vector2(0, -36), UITheme.TextMuted);
        root.nextColor = UIView.Button("DEFAULT", panel.transform, new Vector2(0, -76), new Vector2(430, 44));
        root.hub.colorName = root.nextColor.GetComponentInChildren<Text>();

        // Secondary Row: SETTINGS and CONTROLS
        root.hubSettings = UIView.Button("SETTINGS", panel.transform, new Vector2(-110, -136), new Vector2(210, 42));
        root.hubControls = UIView.Button("CONTROLS", panel.transform, new Vector2(110, -136), new Vector2(210, 42));
        root.language = UIView.Button("LANGUAGE: ENGLISH", panel.transform, new Vector2(0, -192), new Vector2(430, 40));

        // Navigation: BACK / QUIT
        root.hubBack = UIView.Button("QUIT", panel.transform, new Vector2(0, -244), new Vector2(430, 40));
    }

    private static void BuildDeploymentEnvironment(Transform turntable)
    {
        var room = new GameObject("DeploymentStation_Environment").transform;
        var materials = CreateEnvironmentMaterials();

        // Soft, clean background wall using PolishRearWallCore material
        var rearWallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UI/Resources/RobotHuntUI/PolishRearWallCore.mat") ?? materials.wall;
        var wallObj = CreateBox("RearWallCore", room, new Vector3(-0.90f, 1.85f, 2.7f), new Vector3(14f, 7f, .20f), rearWallMat);

        // Dark, modular floor keeps the scene grounded
        CreateBox("IndustrialFloor", room, new Vector3(-.15f, -.29f, 1.6f), new Vector3(8.8f, .18f, 5.7f), materials.floor);

        // Deployment turntable: clean, low-profile silhouette
        var platform = new GameObject("DeploymentTurntable").transform;
        platform.SetParent(turntable, false);
        CreateCylinder("StructuralBase", platform, new Vector3(0f, -.15f, 0f), new Vector3(1.32f, .14f, 1.32f), materials.metal);
        CreateCylinder("MechanicalRing", platform, new Vector3(0f, -.015f, 0f), new Vector3(1.18f, .075f, 1.18f), materials.panel);
        CreateCylinder("TopDeck", platform, new Vector3(0f, .055f, 0f), new Vector3(1.02f, .055f, 1.02f), materials.floorPanel);
        CreateCylinder("CyanDeploymentRing", platform, new Vector3(0f, .069f, 0f), new Vector3(1.105f, .012f, 1.105f), materials.cyan);
        CreateCylinder("DeckInset", platform, new Vector3(0f, .081f, 0f), new Vector3(.82f, .010f, .82f), materials.recess);

        // Local fixtures softly define silhouette, with lights kept outside of the UI panel area.
        AddPointLight("Robot Key Fixture", room, new Vector3(-2.5f, 2.5f, -1.0f), new Color(.50f, .75f, 1f), 2.0f, 5f);
        AddPointLight("Rear Cyan Fixture", room, new Vector3(-1.2f, 1.6f, 1.8f), new Color(.12f, .72f, 1f), 1.3f, 3.5f);
    }

    private static (Material wall, Material recess, Material metal, Material floor, Material floorPanel, Material panel, Material cyan, Material amber, Material label) CreateEnvironmentMaterials()
    {
        const string directory = "Assets/_Project/Materials/Lobby";
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        return (
            GetMaterial(directory, "M_Wall", new Color(.027f, .052f, .083f), .08f, 0f),
            GetMaterial(directory, "M_Recess", new Color(.012f, .023f, .042f), .02f, 0f),
            GetMaterial(directory, "M_Metal", new Color(.075f, .115f, .155f), .72f, .32f),
            GetMaterial(directory, "M_Floor", new Color(.025f, .043f, .067f), .35f, .12f),
            GetMaterial(directory, "M_FloorPanel", new Color(.052f, .083f, .112f), .54f, .19f),
            GetMaterial(directory, "M_Panel", new Color(.042f, .072f, .102f), .62f, .26f),
            GetMaterial(directory, "M_CyanAccent", new Color(.02f, .42f, .68f), .30f, .85f),
            GetMaterial(directory, "M_AmberAccent", new Color(.85f, .26f, .04f), .18f, .0f),
            GetMaterial(directory, "M_TechnicalLabel", new Color(.30f, .70f, .84f), .15f, .0f));
    }

    private static Material GetMaterial(string directory, string name, Color color, float metallic, float emission)
    {
        string path = directory + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        // This project uses URP. Standard materials render pink/missing in that pipeline,
        // so upgrade pre-existing generated materials too when this builder is rerun.
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .52f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .52f);
        if (emission > 0f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion rotation = default)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name; obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
        obj.transform.localRotation = rotation == default ? Quaternion.identity : rotation;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }

    private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion rotation = default)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name; obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
        obj.transform.localRotation = rotation == default ? Quaternion.identity : rotation;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }

    private static void AddPointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
    {
        var obj = new GameObject(name); obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
        var light = obj.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range;
    }

    private static void AddWorldLabel(Transform parent, string text, Vector3 position, float size, Material material)
    {
        var obj = new GameObject("TechnicalLabel_" + text); obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
        obj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var mesh = obj.AddComponent<TextMesh>(); mesh.text = text; mesh.fontSize = 64; mesh.characterSize = size;
        mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center; mesh.color = material.color;
    }
}
#endif
