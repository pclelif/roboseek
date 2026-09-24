#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
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

public static class RobotHuntUIInstaller
{
    [MenuItem("Robot Hunt/UI/Install Production UI in Gameplay Scene")]
    public static void Install()
    {
        var scene = EditorSceneManager.OpenScene(RobotHuntUIInspection.ScenePath);
        if (scene.path != RobotHuntUIInspection.ScenePath) throw new InvalidOperationException("Wrong scene.");
        InstallInCurrentScene();
    }

    public static void InstallInCurrentScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var oldCanvas = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Canvas_UI_Root");
        if (oldCanvas != null)
        {
            UnityEngine.Object.DestroyImmediate(oldCanvas);
        }
        var hunt = UnityEngine.Object.FindFirstObjectByType<ObjectHuntRoundManager>();
        var loop = UnityEngine.Object.FindFirstObjectByType<RoundGameLoop>();
        var movement = UnityEngine.Object.FindFirstObjectByType<RobotMovementController>();
        var camera = UnityEngine.Object.FindFirstObjectByType<ThirdPersonCameraController>();
        if (hunt == null || loop == null || movement == null || camera == null) throw new InvalidOperationException("Missing existing gameplay integration component.");
        var input = movement.GetComponent<PlayerInputReader>();
        foreach (var old in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (old is ObjectHuntHUD || old is Robot.UI.HUD.RobotShowcaseUI || old is Robot.UI.GameModeSelectionUI || old is Robot.UI.HUD.PauseMenuUI || old is Robot.UI.HUD.ObjectHuntUIAdapter || old is Robot.UI.MainMenu.MainMenuUI || old is Robot.UI.Lobby.MultiplayerLobbyUI)
            {
                old.enabled = false; EditorUtility.SetDirty(old);
            }
        }
        var loopSO = new SerializedObject(loop); loopSO.FindProperty("autoStart").boolValue = false;
        loopSO.FindProperty("useUnscaledPresentation").boolValue = true; loopSO.ApplyModifiedPropertiesWithoutUndo();
        var rootGO = new GameObject("Canvas_UI_Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = rootGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = rootGO.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var root = rootGO.AddComponent<UIRootController>(); root.localization = rootGO.AddComponent<UILocalization>(); var state = rootGO.AddComponent<UIStateManager>(); root.state = state; state.loop = loop;
        var gate = rootGO.AddComponent<GameplayInputGate>(); gate.movement = movement; gate.combatInput = movement.GetComponent<PlayerCombatInput>(); gate.input = input; gate.hunt = hunt; state.inputGate = gate;
        var cursor = rootGO.AddComponent<CursorStateController>(); cursor.gameplayCamera = camera; state.cursor = cursor;
        var visuals = rootGO.AddComponent<TargetVisualLibrary>();
        var robotVisualGO = new GameObject("RobotVisuals"); robotVisualGO.transform.SetParent(rootGO.transform, false); var robotVisuals = robotVisualGO.AddComponent<TargetVisualLibrary>();
        var source = rootGO.AddComponent<InteractionStateAdapter>(); source.hunt = hunt; source.state = state;
        var screens = new GameObject[10]; state.screens = screens;
        BuildMainMenu(root, movement.GetComponent<RobotColorCustomizer>(), robotVisuals);
        BuildHub(root, movement.GetComponent<RobotColorCustomizer>(), robotVisuals);
        BuildIntro(root, loop, visuals);
        BuildCountdown(root, loop);
        screens[(int)UIScreen.Gameplay] = UIView.Stretch("GameplayHUD", rootGO.transform).gameObject;
        state.gameplayHUD = screens[(int)UIScreen.Gameplay].AddComponent<CanvasGroup>();
        BuildHUD(root, hunt, visuals, source, screens[(int)UIScreen.Gameplay].transform);
        BuildPause(root);
        SettingsScreenBuilder.Build(root, input, camera);
        BuildResult(root, hunt, visuals);
        foreach (var screen in screens) if (screen != null) screen.SetActive(screen == screens[(int)UIScreen.MainMenu]);
        UIView.Visible(state.gameplayHUD, false);
        var es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        // Keep all authored gameplay objects and prefab overrides, only save the confirmed gameplay scene.
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Validate();
        Debug.Log("ROBOT_HUNT_PRODUCTION_UI_INSTALLED");
    }
    private static Transform Modal(UIRootController root, UIScreen screen, string name, Vector2 size, float dim = .2f, Vector2 position = default)
    {
        var layer = UIView.Screen(name, root.transform, dim); root.state.screens[(int)screen] = layer;
        return UIView.Panel("Panel", layer.transform, size, Robot.UI.UITheme.GlassDark, position).transform;
    }
    private static RawImage Icon(Transform parent, Vector2 size, Vector2 position)
    {
        var image = UIView.Rect("Visual", parent, size, position).gameObject.AddComponent<RawImage>(); image.raycastTarget = false; return image;
    }
    private static void BuildHUD(UIRootController root, ObjectHuntRoundManager hunt, TargetVisualLibrary visuals, InteractionStateAdapter source, Transform parent)
    {
        var target = UIView.Panel("TargetPanel", parent, new Vector2(224, 96), new Color(.08f, .12f, .14f, .65f));
        target.rectTransform.anchorMin = target.rectTransform.anchorMax = target.rectTransform.pivot = Vector2.one;
        target.rectTransform.anchoredPosition = new Vector2(-40, -40); target.raycastTarget = false;
        UIView.Label("Heading", target.transform, "TARGETS", 13, new Vector2(200, 20), new Vector2(0, 34), Color.white);
        var hud = root.gameObject.AddComponent<TargetHUD>(); hud.hunt = hunt; hud.visuals = visuals;
        hud.icons = new RawImage[3]; hud.names = new Text[3]; hud.states = new Text[3]; hud.checks = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            var slot = UIView.Rect("TargetSlot" + (i + 1), target.transform, new Vector2(68, 68), new Vector2((i - 1) * 72, -9));
            hud.icons[i] = Icon(slot, new Vector2(45, 43), new Vector2(0, 15));
            hud.names[i] = UIView.Label("AccessibleName", slot, "", 9, new Vector2(68, 22), new Vector2(0, -11), Color.white);
            hud.states[i] = UIView.Label("FoundState", slot, "0", 15, new Vector2(40, 20), new Vector2(0, -27), Color.white);
            // Geometry checkmark avoids missing glyphs in platform fonts.
            var check = UIView.Rect("CompletionCheckmark", slot, new Vector2(18, 18), new Vector2(0, -27));
            var a = UIView.Panel("ShortStroke", check, new Vector2(7, 3), UIView.Accent, new Vector2(-4, -1)); a.transform.localRotation = Quaternion.Euler(0, 0, -45); a.raycastTarget = false;
            var b = UIView.Panel("LongStroke", check, new Vector2(13, 3), UIView.Accent, new Vector2(2, 1)); b.transform.localRotation = Quaternion.Euler(0, 0, 45); b.raycastTarget = false;
            hud.checks[i] = check; check.gameObject.SetActive(false);
        }
        var promptRect = UIView.Rect("InteractionPrompt", parent, new Vector2(290, 42));
        promptRect.anchorMin = promptRect.anchorMax = new Vector2(.5f, 0); promptRect.anchoredPosition = new Vector2(0, 145);
        var prompt = root.gameObject.AddComponent<InteractionPromptUI>(); prompt.group = promptRect.gameObject.AddComponent<CanvasGroup>(); prompt.group.alpha = 0; prompt.group.blocksRaycasts = false; prompt.group.interactable = false; prompt.source = source; root.state.prompt = prompt;
        UIView.Keycap(promptRect, "E", new Vector2(-118, 0), new Vector2(36, 38));
        UIView.Label("Action", promptRect, "SCAN & RETRIEVE", 19, new Vector2(238, 40), new Vector2(23, 0), Color.white).gameObject.AddComponent<Shadow>();
        var feedbackRect = UIView.Rect("FeedbackMessage", parent, new Vector2(440, 65));
        feedbackRect.anchorMin = feedbackRect.anchorMax = new Vector2(.5f, 1); feedbackRect.anchoredPosition = new Vector2(0, -150);
        var feedback = root.gameObject.AddComponent<FeedbackMessageUI>(); feedback.group = feedbackRect.gameObject.AddComponent<CanvasGroup>(); feedback.group.alpha = 0; feedback.group.blocksRaycasts = false; feedback.group.interactable = false;
        UIView.Label("Heading", feedbackRect, "TARGET RETRIEVED", 16, new Vector2(440, 28), new Vector2(0, 16), UIView.AccentYellow).gameObject.AddComponent<Shadow>();
        feedback.targetName = UIView.Label("TargetName", feedbackRect, "", 21, new Vector2(440, 32), new Vector2(0, -15), Color.white); feedback.targetName.gameObject.AddComponent<Shadow>(); hud.feedback = feedback;
        var hintRect = UIView.Rect("TutorialHint", parent, new Vector2(300, 40)); hintRect.anchorMin = hintRect.anchorMax = new Vector2(.5f, 0); hintRect.anchoredPosition = new Vector2(0, 100);
        var tutorial = root.gameObject.AddComponent<TutorialHintUI>(); root.state.tutorial = tutorial;
        tutorial.group = hintRect.gameObject.AddComponent<CanvasGroup>(); tutorial.group.blocksRaycasts = false; tutorial.group.interactable = false; tutorial.group.alpha = 0;
        tutorial.key = UIView.Keycap(hintRect, "W A S D", new Vector2(-100, 0), new Vector2(100, 38)); tutorial.message = UIView.Label("Hint", hintRect, "MOVE", 18, new Vector2(195, 40), new Vector2(53, 0), Color.white); tutorial.message.gameObject.AddComponent<Shadow>();
        tutorial.input = root.state.inputGate.input; tutorial.movement = root.state.inputGate.movement; tutorial.cameraController = root.state.cursor.gameplayCamera;
        tutorial.attack = tutorial.movement.GetComponent<CombatAttack>(); tutorial.hunt = hunt; tutorial.interaction = source;
    }
    private static void BuildMainMenu(UIRootController root, RobotColorCustomizer robot, TargetVisualLibrary visuals)
    {
        var panel = Modal(root, UIScreen.MainMenu, "MainMenu", new Vector2(460, 680), .2f, new Vector2(-420, 0));
        var showcase = root.gameObject.AddComponent<RobotShowcaseController>(); showcase.robot = robot; showcase.visuals = visuals;
        showcase.menuImage = Icon(panel, new Vector2(260, 260), new Vector2(0, 160));
        UIView.Label("Title", panel, "RoboSeek", 44, new Vector2(420, 56), new Vector2(0, 260), Robot.UI.UITheme.AccentYellow);
        UIView.Label("Subtitle", panel, "Where is my toy?", 18, new Vector2(420, 30), new Vector2(0, 218), Robot.UI.UITheme.PrimaryBlue);
        root.menu = root.gameObject.AddComponent<MainMenuController>(); root.menu.state = root.state;
        root.menuPlay = UIView.Button("PLAY", panel, new Vector2(0, 10), new Vector2(380, 58));
        root.menuSettings = UIView.Button("SETTINGS", panel, new Vector2(0, -60), new Vector2(380, 50));
        root.menuControls = UIView.Button("CONTROLS", panel, new Vector2(0, -125), new Vector2(380, 50));
        root.menuColor = UIView.Button("ROBOT COLOR", panel, new Vector2(0, -190), new Vector2(380, 50));
        root.menuQuit = UIView.Button("QUIT", panel, new Vector2(0, -255), new Vector2(380, 50));
    }
    private static Color UIColor(Color baseColor, float alpha)
    {
        baseColor.a = alpha;
        return baseColor;
    }
    private static void BuildIntro(UIRootController root, RoundGameLoop loop, TargetVisualLibrary visuals)
    {
        var layer = UIView.Screen("RoundIntro", root.transform, .3f); root.state.screens[(int)UIScreen.Intro] = layer;
        var intro = root.gameObject.AddComponent<RoundIntroController>(); root.state.intro = intro; intro.loop = loop; intro.visuals = visuals;
        var reveal = UIView.Rect("TargetReveal", layer.transform, new Vector2(840, 400)); intro.reveal = reveal.gameObject;
        UIView.Label("Heading", reveal, "FIND THESE", 36, new Vector2(800, 60), new Vector2(0, 180), Color.white);
        UIView.Label("Instruction", reveal, "FIND ALL 3 TARGETS", 19, new Vector2(800, 40), new Vector2(0, -170), Color.white);
        intro.cards = new CanvasGroup[3]; intro.icons = new RawImage[3]; intro.names = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            var card = UIView.Panel("TargetCard" + (i + 1), reveal, new Vector2(220, 240), UIView.Paper, new Vector2((i - 1) * 244, 0));
            intro.cards[i] = card.gameObject.AddComponent<CanvasGroup>(); intro.icons[i] = Icon(card.transform, new Vector2(170, 170), new Vector2(0, 24));
            intro.names[i] = UIView.Label("TargetName", card.transform, "", 18, new Vector2(210, 48), new Vector2(0, -84));
        }
    }
    private static void BuildCountdown(UIRootController root, RoundGameLoop loop)
    {
        var layer = UIView.Screen("Countdown", root.transform, .18f); root.state.screens[(int)UIScreen.Countdown] = layer;
        var intro = root.state.intro;
        intro.countdown = UIView.Label("Countdown", layer.transform, "", 120, new Vector2(600, 220), default, Color.white);
    }
    private static void BuildHub(UIRootController root, RobotColorCustomizer robot, TargetVisualLibrary visuals)
    {
        var panel = Modal(root, UIScreen.Hub, "RobotHub", new Vector2(480, 700), .2f, new Vector2(440, 0));
        var showcase = root.GetComponent<RobotShowcaseController>(); if (showcase == null) showcase = root.gameObject.AddComponent<RobotShowcaseController>(); showcase.robot = robot; showcase.visuals = visuals;
        showcase.hubImage = Icon(panel, new Vector2(240, 240), new Vector2(0, 245));

        UIView.Label("Title", panel, "RoboSeek", 40, new Vector2(440, 50), new Vector2(0, 315), Robot.UI.UITheme.AccentYellow);
        UIView.Label("Subtitle", panel, "Where is my toy?", 16, new Vector2(440, 26), new Vector2(0, 280), Robot.UI.UITheme.PrimaryBlue);

        root.hub = root.GetComponent<RobotHubController>(); if (root.hub == null) root.hub = root.gameObject.AddComponent<RobotHubController>(); root.hub.state = root.state; root.hub.robot = robot; root.hub.showcase = showcase;

        UIView.Label("ModeHeader", panel, "GAME MODE", 13, new Vector2(420, 22), new Vector2(0, 175), Robot.UI.UITheme.TextMuted);
        root.hubSolo = UIView.Button("SOLO", panel, new Vector2(-105, 140), new Vector2(200, 48));
        root.hubMultiplayer = UIView.Button("MULTIPLAYER", panel, new Vector2(105, 140), new Vector2(200, 48));
        root.hub.soloButton = root.hubSolo;
        root.hub.multiplayerButton = root.hubMultiplayer;

        root.hub.modeDescription = UIView.Label("ModeDescription", panel, "SOLO: Search for hidden toys alone!", 14, new Vector2(420, 36), new Vector2(0, 92), Color.white);

        root.startGame = UIView.Button("START GAME", panel, new Vector2(0, 20), new Vector2(420, 62));
        root.hub.startGameButton = root.startGame;

        root.nextColor = UIView.Button("ROBOT COLOR", panel, new Vector2(0, -60), new Vector2(420, 50));
        root.hub.colorName = UIView.Label("Color", panel, "", 14, new Vector2(380, 24), new Vector2(0, -100), Robot.UI.UITheme.AccentYellow);

        root.hubSettings = UIView.Button("SETTINGS", panel, new Vector2(-105, -155), new Vector2(200, 46));
        root.hubControls = UIView.Button("CONTROLS", panel, new Vector2(105, -155), new Vector2(200, 46));
        root.language = UIView.Button("LANGUAGE: TURKISH", panel, new Vector2(0, -215), new Vector2(420, 46));
        root.hubBack = UIView.Button("BACK", panel, new Vector2(0, -275), new Vector2(420, 46));
    }
    private static void BuildPause(UIRootController root)
    {
        var panel = Modal(root, UIScreen.Pause, "PauseMenu", new Vector2(490, 560));
        UIView.Label("Title", panel, "PAUSED", 36, new Vector2(440, 60), new Vector2(0, 208));
        root.resume = UIView.Button("RESUME", panel, new Vector2(0, 113), new Vector2(340, 52));
        root.pauseSettings = UIView.Button("SETTINGS", panel, new Vector2(0, 47), new Vector2(340, 52));
        root.pauseControls = UIView.Button("CONTROLS", panel, new Vector2(0, -19), new Vector2(340, 52));
        root.restart = UIView.Button("RESTART ROUND", panel, new Vector2(0, -85), new Vector2(340, 52));
        root.quit = UIView.Button("QUIT TO LOBBY", panel, new Vector2(0, -151), new Vector2(340, 52));
        var confirmPanel = Modal(root, UIScreen.Confirm, "ConfirmDialog", new Vector2(590, 300));
        var confirm = root.gameObject.AddComponent<ConfirmDialogController>(); root.confirm = confirm; confirm.state = root.state;
        confirm.title = UIView.Label("Title", confirmPanel, "", 30, new Vector2(550, 60), new Vector2(0, 86));
        confirm.message = UIView.Label("Message", confirmPanel, "", 20, new Vector2(530, 66), new Vector2(0, 5));
        root.cancel = UIView.Button("CANCEL", confirmPanel, new Vector2(-130, -87), new Vector2(230, 50));
        root.confirmAction = UIView.Button("CONFIRM", confirmPanel, new Vector2(130, -87), new Vector2(230, 50)); confirm.confirmLabel = root.confirmAction.GetComponentInChildren<Text>();
        root.pause = root.gameObject.AddComponent<PauseMenuController>(); root.pause.state = root.state; root.pause.confirm = confirm;
    }
    private static void BuildResult(UIRootController root, ObjectHuntRoundManager hunt, TargetVisualLibrary visuals)
    {
        var panel = Modal(root, UIScreen.Result, "ResultScreen", new Vector2(880, 720), .35f);
        var result = root.gameObject.AddComponent<ResultScreenController>(); root.state.result = result; result.hunt = hunt; result.visuals = visuals;
        result.title = UIView.Label("Title", panel, "ROUND COMPLETE", 36, new Vector2(810, 58), new Vector2(0, 286));
        result.count = UIView.Label("Count", panel, "3 / 3", 32, new Vector2(350, 50), new Vector2(170, 206));
        UIView.Label("FoundLabel", panel, "TARGETS FOUND", 17, new Vector2(350, 30), new Vector2(170, 162));
        root.hub.showcase.resultImage = Icon(panel, new Vector2(240, 270), new Vector2(-220, 67));
        result.icons = new RawImage[3]; result.names = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            var card = UIView.Rect("CollectedTarget" + (i + 1), panel, new Vector2(136, 145), new Vector2(12 + i * 140, 38));
            result.icons[i] = Icon(card, new Vector2(120, 115), new Vector2(0, 17));
            result.names[i] = UIView.Label("Name", card, "", 12, new Vector2(132, 35), new Vector2(0, -59));
        }
        result.time = UIView.Label("Time", panel, "TIME", 24, new Vector2(300, 85), new Vector2(-165, -129));
        result.score = UIView.Label("Score", panel, "SCORE", 24, new Vector2(300, 85), new Vector2(165, -129));
        root.nextRound = UIView.Button("NEXT ROUND", panel, new Vector2(0, -230), new Vector2(330, 48)); result.next = root.nextRound;
        root.returnToHub = UIView.Button("RETURN TO LOBBY", panel, new Vector2(0, -295), new Vector2(330, 48));
    }
    public static void ApplyTypography()
    {
        var scene = EditorSceneManager.OpenScene(RobotHuntUIInspection.ScenePath);
        var root = UnityEngine.Object.FindFirstObjectByType<UIRootController>();
        if (root == null) throw new Exception("Missing production UI root.");
        foreach (var label in root.GetComponentsInChildren<Text>(true))
        {
            UIView.ApplyFont(label);
            EditorUtility.SetDirty(label);
        }
        root.GetComponent<Canvas>().pixelPerfect = true;
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Validate();
    }
    [MenuItem("Robot Hunt/UI/Validate Production UI")]
    public static void Validate()
    {
        var root = UnityEngine.Object.FindFirstObjectByType<UIRootController>();
        if (root == null || root.gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()) throw new Exception("Production root missing/wrong scene.");
        if (root.state.screens.Length != 10 || root.state.screens.Any(x => x == null)) throw new Exception("Missing screen reference.");
        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null) throw new Exception("Missing UI script.");
            var serialized = new SerializedObject(component); var property = serialized.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                    throw new Exception("Broken reference: " + component.GetType().Name + "." + property.propertyPath);
            }
        }
        if (UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length != 1) throw new Exception("Expected one EventSystem.");
        Debug.Log("ROBOT_HUNT_UI_VALIDATION_PASSED");
    }
}
#endif
