#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Robot.UI.Production;
using Robot.NPC;
using Robot.Robots.Customization;
using UnityEngine.AI;
using System.Collections.Generic;

public sealed class RoboSeekRevisionProbe : MonoBehaviour
{
    private int language, color;
    private readonly int[] tutorials = new int[4];
    private float deadline;
    private bool complete;
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        language = PlayerPrefs.GetInt("RoboSeek.Language", 0);
        color = PlayerPrefs.GetInt("RobotHunt.PlayerColor", 0);
        for (int i = 0; i < 4; i++)
        {
            string key = "RobotHunt.Tutorial.v1." + i;
            tutorials[i] = PlayerPrefs.GetInt(key, -1);
            PlayerPrefs.SetInt(key, 1);
        }
        deadline = Time.realtimeSinceStartup + 150;
        StartCoroutine(SafeRun());
    }
    private void Update() { if (!complete && Time.realtimeSinceStartup > deadline) Finish("FAIL: timeout", 1); }
    private IEnumerator SafeRun()
    {
        var routine = CheckFlow();
        while (true)
        {
            object next;
            try { if (!routine.MoveNext()) break; next = routine.Current; }
            catch (Exception ex) { Finish("FAIL: " + ex, 1); yield break; }
            yield return next;
        }
        Finish("PASS: lobby, language, settings, yellow timer/checks, real pickups, knockout/pause/recovery, world bounds, underwater, shared result pose, complete round and return", 0);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Debug.Log("REVISION PASS: " + message);
    }
    private IEnumerator CheckFlow()
    {
        yield return new WaitForSecondsRealtime(1);
        var root = FindFirstObjectByType<UIRootController>();
        Require(root.state.Current == UIScreen.Hub, "Dedicated lobby opens directly");
        Require(root.hub.CurrentMode == GameMode.Solo && root.startGame.interactable, "Solo is selected by default");
        Require(root.hubSolo.image.color == root.startGame.image.color && root.hubSolo.colors.selectedColor == Color.white, "Yellow stays identical on selected and unselected buttons");
        var deck = GameObject.Find("DeploymentTurntable");
        Require(deck.GetComponentsInChildren<Renderer>().Length == 1 && deck.transform.Find("StructuralBase").localScale.x > 1.32f, "Platform has one wider step");
        var rotation = GameObject.Find("RobotShowcasePlatform").transform.rotation;
        yield return new WaitForSecondsRealtime(.3f);
        Require(Quaternion.Angle(rotation, GameObject.Find("RobotShowcasePlatform").transform.rotation) > 1, "Showcase rotates with frozen game time");
        root.hubSettings.onClick.Invoke();
        Require(root.state.Current == UIScreen.Settings && root.state.screens[(int)UIScreen.Settings].activeInHierarchy, "Lobby settings opens");
        Require(root.settings.master.fillRect.GetComponent<Image>().color == UIView.AccentYellow && root.settings.master.fillRect.GetComponent<Image>().sprite == null, "Lobby settings slider uses neutral texture and shared yellow");
        Capture("/private/tmp/roboseek-revision-settings.png");
        root.settingsBack.onClick.Invoke();
        Require(root.state.Current == UIScreen.Hub, "Settings returns to lobby");
        root.hubControls.onClick.Invoke();
        Require(root.state.Current == UIScreen.Controls && root.controls.keys[0].text.Length > 0, "Lobby controls opens with bindings");
        root.controlsBack.onClick.Invoke();
        root.language.onClick.Invoke();
        Require(root.language.GetComponentInChildren<Text>().text == (UILocalization.IsTurkish ? "DİL: TÜRKÇE" : "LANGUAGE: ENGLISH"), "Language label shows current language");
        root.language.onClick.Invoke();
        root.nextColor.onClick.Invoke();
        Require(root.nextColor.image.color == root.hub.robot.ActiveThemeColor, "Color button follows robot");
        while (root.hub.robot.ActiveTheme != RobotColorCustomizer.ColorTheme.Sari) root.nextColor.onClick.Invoke();
        Capture("/private/tmp/roboseek-revision-lobby.png");
        yield return new WaitForSecondsRealtime(.5f);
        root.startGame.onClick.Invoke();
        yield return new WaitForSecondsRealtime(1);
        root = FindFirstObjectByType<UIRootController>();
        Require(root.state.Current != UIScreen.MainMenu && root.state.Current != UIScreen.Hub, "Start skips intermediate menus");
        Require(root.state.inputGate.movement.GetComponent<CharacterController>().isGrounded, "Robot grounded during intro");
        var playerColors = root.state.inputGate.movement.GetComponent<RobotColorCustomizer>();
        Require(playerColors.ActiveTheme == RobotColorCustomizer.ColorTheme.Sari, "Gameplay keeps lobby yellow selection");
        var npcs = FindObjectsByType<NpcRobotController>(FindObjectsSortMode.None);
        Require(npcs.Length == 9, "Nine enemy robots spawn");
        for (int c = 0; c < 10; c++)
        {
            var distinct = new HashSet<RobotColorCustomizer.ColorTheme>();
            foreach (var npc in npcs)
            {
                var colors = npc.GetComponent<RobotColorCustomizer>();
                Require(colors.ActiveTheme != playerColors.ActiveTheme && distinct.Add(colors.ActiveTheme), "Enemy color is unique and excludes " + playerColors.ActiveTheme);
            }
            playerColors.NextTheme();
            yield return null;
        }
        while (root.state.Current != UIScreen.Countdown) yield return null;
        Require(root.state.intro.countdown.color == Color.white, "Countdown is white");
        Capture("/private/tmp/roboseek-revision-countdown.png");
        while (root.state.intro.countdown.text != "GO!") yield return null;
        Require(root.state.intro.countdown.color == UIView.AccentYellow, "GO is yellow");
        while (root.state.Current != UIScreen.Gameplay) yield return null;
        var timer = root.transform.Find("GameplayHUD/RoundTimer/TimeRemaining").GetComponent<Text>();
        Require(timer.color == UIView.AccentYellow, "Timer digits and colon are yellow");
        Require(timer.font != null && timer.font.name.IndexOf("Kenney", System.StringComparison.OrdinalIgnoreCase) >= 0, "Timer uses Kenney font");
        Require(timer.text.StartsWith("10:") || timer.text.StartsWith("09:"), "Ten minute timer visible");
        Capture("/private/tmp/roboseek-revision-gameplay.png");
        root.state.Open(UIScreen.Pause);
        Require(root.resume.image.color == UIView.AccentYellow && root.pauseSettings.image.color == UIView.AccentYellow, "Pause buttons share lobby yellow");
        Capture("/private/tmp/roboseek-revision-pause.png");
        Require(root.resume.GetComponentInChildren<Text>().text == UILocalization.Translate("RETURN TO GAME"), "Pause return label");
        Require(root.restart.GetComponent<RectTransform>().anchoredPosition.y > root.pauseSettings.GetComponent<RectTransform>().anchoredPosition.y, "Pause order matches request");
        root.pauseSettings.onClick.Invoke();
        Require(root.settings.master.fillRect.GetComponent<Image>().color == UIView.AccentYellow, "Saved gameplay settings sliders use yellow");
        root.settingsBack.onClick.Invoke();
        Require(root.state.Current == UIScreen.Pause, "Pause settings returns to pause");
        root.resume.onClick.Invoke();
        Require(root.state.IsGameplay, "Resume restores gameplay");
        var movement = root.state.inputGate.movement;
        var origin = movement.transform.position;
        var agent = npcs[0].GetComponent<NavMeshAgent>();
        var enemyOrigin = npcs[0].transform.position;
        Require(agent.Warp(origin + Vector3.forward * 3), "Enemy moved near player for contextual hint");
        yield return new WaitForSecondsRealtime(.5f);
        Debug.Log("POLISH Q diagnostics: distance=" + Vector3.Distance(npcs[0].transform.position, movement.transform.position) + " state=" + root.state.Current + " target=" + root.state.prompt.source.Current + " health=" + movement.GetComponent<Robot.Combat.CombatHealth>().CurrentHealth);
        Require(root.state.tutorial.group.alpha > .9f && root.state.tutorial.key.text == "Q", "Q hint returns near enemy even after tutorial was learned");
        Capture("/private/tmp/roboseek-revision-q-hint.png");
        agent.Warp(enemyOrigin);
        var toy = FindFirstObjectByType<Robot.ObjectHunt.CollectibleTarget>();
        var controller = movement.GetComponent<CharacterController>();
        controller.enabled = false; movement.transform.position = toy.transform.position + Vector3.right; controller.enabled = true;
        yield return new WaitForSecondsRealtime(.5f);
        Require(root.state.prompt.group.alpha > .9f && root.state.tutorial.group.alpha == 0, "E pickup hint appears at toy without overlapping Q hint");
        Capture("/private/tmp/roboseek-revision-e-hint.png");
        Require(toy.TryCollect(movement.transform, null), "Toy pickup starts");
        yield return new WaitForSecondsRealtime(.9f);
        var targetHud = root.GetComponent<TargetHUD>();
        Require(root.state.inputGate.hunt.CollectedCount == 1, "Toy pickup is recorded");
        foreach (var check in targetHud.checks)
            if (check.gameObject.activeSelf) foreach (var stroke in check.GetComponentsInChildren<Image>()) Require(stroke.color == UIView.AccentYellow, "Collected HUD check is yellow");
        Capture("/private/tmp/roboseek-polish-collected.png");
        // Exercise actual knockout state, including pausing the recovery timer.
        controller.enabled = false; movement.transform.position = origin; controller.enabled = true;
        var health = movement.GetComponent<Robot.Combat.CombatHealth>();
        var healthSettings = new SerializedObject(health);
        float duration = health.KnockoutDuration;
        healthSettings.FindProperty("knockoutDuration").floatValue = 1.5f; healthSettings.ApplyModifiedPropertiesWithoutUndo();
        health.TakeDamage(health.MaxHealth, npcs[0].gameObject);
        yield return null;
        var notice = root.state.gameplayHUD.transform.Find("KnockoutNotice");
        Require(health.IsKnockedOut && notice.gameObject.activeInHierarchy, "Knockout notice appears only when hit knocks player out");
        Capture("/private/tmp/roboseek-polish-knockout.png");
        root.state.Open(UIScreen.Pause); float recovery = health.KnockoutTimeRemaining;
        yield return new WaitForSecondsRealtime(.2f);
        Require(Mathf.Abs(health.KnockoutTimeRemaining-recovery)<.01f, "Pause freezes recovery countdown");
        root.state.Resume(); yield return new WaitForSecondsRealtime(1.7f);
        Require(!health.IsKnockedOut && !notice.gameObject.activeSelf, "Recovery removes temporary notice");
        healthSettings.Update(); healthSettings.FindProperty("knockoutDuration").floatValue = duration; healthSettings.ApplyModifiedPropertiesWithoutUndo();
        var safety = movement.GetComponent<Robot.Player.Movement.WorldSafety>();
        Require(safety != null && GameObject.Find("WorldBoundary").GetComponentsInChildren<BoxCollider>().Length == 5, "World has four walls and a seabed");
        controller.enabled=false; movement.transform.position = new Vector3(safety.limits.max.x+10,-100,safety.limits.center.z); controller.enabled=true;
        yield return null; yield return null;
        Require(safety.limits.Contains(movement.transform.position), "Out of bounds and endless fall recovered");
        Require(Camera.main.backgroundColor.b > Camera.main.backgroundColor.r, "Underwater environment is blue");
        controller.enabled=false; movement.transform.position=origin; controller.enabled=true; movement.ResetGroundedMotion();
        yield return new WaitForSecondsRealtime(.8f);
        root.state.Open(UIScreen.Result);
        root.state.result.Show(new Robot.ObjectHunt.RoundResultData { completed=false, foundCount=1, elapsedTime=600 });
        yield return null;
        Require(root.hub.showcase.resultPose != null, "Both result screens use a fixed airborne pose");
        Require(root.state.result.time.text.Contains("<color=#"), "Result time value is yellow");
        Require(root.state.result.icons[0].transform.parent.Find("ResultMarker") != null, "Result targets have geometry status marks");
        Capture("/private/tmp/roboseek-polish-round-over.png");
        root.state.Resume();
        var remaining = new List<Robot.ObjectHunt.CollectibleTarget>(root.state.inputGate.hunt.ActiveTargets);
        foreach (var item in remaining)
        {
            if (item == null || !item.gameObject.activeSelf) continue;
            controller.enabled=false; movement.transform.position=item.transform.position + Vector3.right; controller.enabled=true;
            item.TryCollect(movement.transform, null); yield return new WaitForSecondsRealtime(.9f);
        }
        yield return new WaitForSecondsRealtime(1.6f);
        Require(root.state.inputGate.hunt.CollectedCount == 3 && root.state.Current == UIScreen.Result, "All three pickups reach actual complete result");
        Capture("/private/tmp/roboseek-polish-round-complete.png");
        root.state.Open(UIScreen.Pause);
        Require(root.state.prompt.group.alpha == 0 && root.state.tutorial.group.alpha == 0, "Contextual hints hide while paused");
        root.quit.onClick.Invoke();
        yield return new WaitForSecondsRealtime(1);
        root = FindFirstObjectByType<UIRootController>();
        Require(SceneManager.GetActiveScene().path == "Assets/_Project/Scenes/RoboSeek_Lobby.unity" && root.state.Current == UIScreen.Hub, "Quit returns directly to dedicated lobby");
    }
        private static void Capture(string path)
        {
            var root = FindFirstObjectByType<UIRootController>();
            var canvas = root.GetComponent<Canvas>(); var camera = Camera.main;
            var rt = new RenderTexture(1920, 1080, 24); rt.Create();
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            float scale = pipeline.renderScale; pipeline.renderScale = 1;
            var cameraData = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            bool postProcessing = cameraData != null && cameraData.renderPostProcessing;
            if (cameraData != null) cameraData.renderPostProcessing = false;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>(); bool scalerEnabled = scaler.enabled;
            scaler.enabled = false; float canvasScale = canvas.scaleFactor; canvas.scaleFactor = 1;
            var oldTarget = camera.targetTexture; camera.targetTexture = rt;
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldPlane = canvas.planeDistance;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + .01f;
            foreach (var label in root.GetComponentsInChildren<Text>(true))
            {
                label.font.RequestCharactersInTexture(label.text, label.fontSize, label.fontStyle);
                label.SetAllDirty();
            }
            Canvas.ForceUpdateCanvases();
            var request = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            RenderTexture.active = previous; canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane;
            camera.targetTexture = oldTarget; pipeline.renderScale = scale;
            if (cameraData != null) cameraData.renderPostProcessing = postProcessing;
            canvas.scaleFactor = canvasScale; scaler.enabled = scalerEnabled;
            Destroy(texture); rt.Release(); Destroy(rt);
        }

    private void Finish(string result, int code)
    {
        complete = true;
        SessionState.SetBool("RoboSeek.RevisionActive", false);
        PlayerPrefs.SetInt("RoboSeek.Language", language);
        PlayerPrefs.SetInt("RobotHunt.PlayerColor", color);
        for (int i = 0; i < 4; i++)
        {
            string key = "RobotHunt.Tutorial.v1." + i;
            if (tutorials[i] < 0) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetInt(key, tutorials[i]);
        }
        PlayerPrefs.Save();
        File.WriteAllText("/private/tmp/roboseek-revision-results.txt", result);
        Debug.Log(result);
        EditorApplication.Exit(code);
    }
}
#endif
