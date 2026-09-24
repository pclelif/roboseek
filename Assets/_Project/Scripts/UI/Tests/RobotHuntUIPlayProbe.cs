#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Robot.ObjectHunt;
using Robot.UI.Settings;
namespace Robot.UI.Production
{
    public sealed class RobotHuntUIPlayProbe : MonoBehaviour
    {
        private readonly List<string> results = new List<string>();
        private readonly Dictionary<string, int> tutorials = new Dictionary<string, int>();
        private float master, music, sfx, sensitivity;
        private SettingsManager settings;
        private bool finished;
        private float deadline;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editorInput;
        private void Start()
        {
            background = InputSystem.settings.backgroundBehavior;
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.EnableDevice(Keyboard.current);
            deadline = Time.realtimeSinceStartup + 120;
            for (int i = 0; i < 4; i++) { string key = "RobotHunt.Tutorial.v1." + i; tutorials[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : -1; }
            Application.logMessageReceived += Log;
            StartCoroutine(RunSafely());
        }
        private void Update() { if (!finished && Time.realtimeSinceStartup > deadline) Finish("FAIL: test timeout"); }
        private void Log(string condition, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Assert || type == LogType.Error) results.Add("RUNTIME ERROR: " + condition + "\n" + trace);
        }
        private IEnumerator RunSafely()
        {
            var tests = Run();
            while (true)
            {
                object next = null; bool more = false;
                try { more = tests.MoveNext(); if (more) next = tests.Current; }
                catch (Exception ex) { Finish("FAIL: " + ex); yield break; }
                if (!more) break;
                yield return next;
            }
            Finish("PASS: production UI integration flow");
        }
        private void Check(bool value, string message)
        {
            if (!value) throw new Exception(message);
            results.Add("PASS: " + message);
        }
        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(2);
            var root = FindFirstObjectByType<UIRootController>(); var state = root.state; var gate = state.inputGate; var hunt = gate.hunt;
            settings = SettingsManager.Instance; master = settings.MasterVolume; music = settings.MusicVolume; sfx = settings.SFXVolume; sensitivity = settings.Sensitivity;
            Check(state.Current == UIScreen.MainMenu && Time.timeScale == 0, "Scene starts in Main Menu with frozen world");
            Check(!gate.movement.enabled && !gate.combatInput.enabled && !hunt.InteractionEnabled, "Hub blocks movement, attack and retrieval");
            Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Menu cursor visible/unlocked");
            Check(FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Exactly one EventSystem");
            Check(GameObject.Find("Object Hunt HUD") == null, "Retained legacy HUD does not create a second Canvas");
            Capture("/tmp/robot-hunt-hub.png"); yield return new WaitForSecondsRealtime(.5f);
            root.menuPlay.onClick.Invoke(); Check(state.Current == UIScreen.Hub, "Play enters Robot Hub");
            root.hubSettings.onClick.Invoke(); Check(state.Current == UIScreen.Settings, "Hub opens shared settings");
            root.settings.master.value = .63f; root.settings.music.value = .42f; root.settings.sfx.value = .57f; root.settings.sensitivity.value = 1.7f;
            settings.LoadSettings();
            Check(Mathf.Approximately(settings.MasterVolume, .63f) && Mathf.Approximately(settings.MusicVolume, .42f) && Mathf.Approximately(settings.SFXVolume, .57f) && Mathf.Approximately(settings.Sensitivity, 1.7f), "Audio and sensitivity persist through preference reload");
            Check(Mathf.Approximately(AudioListener.volume, .63f), "Master volume applied to AudioListener");
            root.categories[2].onClick.Invoke(); root.settingsControls.onClick.Invoke();
            Check(state.Current == UIScreen.Controls, "Settings opens shared Controls");
            Escape(); yield return null; yield return null; Release();
            Check(state.Current == UIScreen.Settings, "Escape from Controls returns to Settings (actual " + state.Current + ")");
            Escape(); yield return null; yield return null; Release();
            Check(state.Current == UIScreen.Hub, "Escape from Settings returns to Hub");
            root.hubSolo.onClick.Invoke();
            root.startGame.onClick.Invoke();
            Check(state.Current == UIScreen.Intro && !gate.movement.enabled && !hunt.IsRoundActive, "Start Game enters intro without activating gameplay");
            yield return new WaitForSecondsRealtime(2.0f);
            Check(hunt.SelectedTargets.Count == 3, "Intro reads exactly three real selected targets");
            Check(!hunt.TryPickupNearest(), "Retrieval rejected during intro");
            Capture("/tmp/robot-hunt-intro.png"); yield return new WaitForSecondsRealtime(.4f);
            while (state.Current != UIScreen.Countdown) yield return null;
            Check(state.Current == UIScreen.Countdown, "Countdown uses its own visible state");
            while (!state.IsGameplay) yield return null;
            Check(Time.timeScale == 1 && gate.movement.enabled && gate.combatInput.enabled && hunt.InteractionEnabled, "GO enables existing gameplay systems");
            Check(state.cursor.WantsLockedCursor, "Gameplay requests locked/hidden cursor (OS lock requires focused Game View)");
            yield return null;
            var prompt = state.prompt;
            Check(source(root).Current == null && prompt.group.alpha == 0, "No interaction prompt without valid nearby target");
            var initial = gate.movement.transform.position;
            gate.input.SetMobileMove(Vector2.up); yield return new WaitForSecondsRealtime(.3f); gate.input.SetMobileMove(Vector2.zero);
            Check(Vector3.Distance(initial, gate.movement.transform.position) > .05f, "Existing movement works after intro");
            Check(PlayerPrefs.GetInt("RobotHunt.Tutorial.v1.0", 0) == 1, "Actual movement completes persistent movement hint");
            var orbit = state.cursor.gameplayCamera.GetComponent<Unity.Cinemachine.CinemachineOrbitalFollow>();
            var cameraInputField = typeof(Robot.Player.CameraControl.ThirdPersonCameraController).GetField("inputReader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Check(cameraInputField.GetValue(state.cursor.gameplayCamera) == gate.input, "Camera reads the existing local player input");
            float yaw = orbit.HorizontalAxis.Value;
            gate.input.SetMobileLook(new Vector2(20, 0)); yield return null; yield return null; gate.input.SetMobileLook(Vector2.zero);
            Check(!Mathf.Approximately(yaw, orbit.HorizontalAxis.Value), "Existing camera responds to look input and saved sensitivity");
            Check(PlayerPrefs.GetInt("RobotHunt.Tutorial.v1.1", 0) == 1, "Actual camera movement completes persistent camera hint");
            Escape(); yield return null; yield return null; Release();
            Check(state.Current == UIScreen.Pause && Time.timeScale == 0 && !gate.movement.enabled && !gate.combatInput.enabled, "Escape pauses time and player input");
            yaw = orbit.HorizontalAxis.Value;
            gate.input.SetMobileLook(new Vector2(20, 0));
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Q)); yield return null; yield return null; Release();
            Check(Mathf.Approximately(yaw, orbit.HorizontalAxis.Value) && !gate.combatInput.GetComponent<Robot.Combat.CombatAttack>().IsAttacking, "Pause blocks actual camera and Q attack input");
            float remaining = hunt.TimeRemaining; yield return new WaitForSecondsRealtime(.2f);
            Check(Mathf.Approximately(remaining, hunt.TimeRemaining), "Round timer does not tick while paused");
            root.pauseSettings.onClick.Invoke();
            Check(state.Current == UIScreen.Settings && Time.timeScale == 0 && Cursor.visible, "Same Settings instance preserves pause and cursor");
            Escape(); yield return null; yield return null; Release(); Check(state.Current == UIScreen.Pause, "Escape closes Settings before Pause");
            root.pauseControls.onClick.Invoke(); Escape(); yield return null; yield return null; Release(); Check(state.Current == UIScreen.Pause, "Escape closes Controls before Pause");
            root.restart.onClick.Invoke(); Check(state.Current == UIScreen.Confirm, "Restart requires confirmation");
            root.cancel.onClick.Invoke(); Check(state.Current == UIScreen.Pause, "Cancel restart preserves paused round");
            root.resume.onClick.Invoke(); yield return null;
            Check(state.IsGameplay && Time.timeScale == 1, "Resume restores gameplay");
            var target = hunt.ActiveTargets[0]; Teleport(gate, target.transform.position + Vector3.right);
            yield return new WaitForSecondsRealtime(.25f);
            Check(source(root).Current == target && prompt.group.alpha > .95f, "Valid existing interaction fades prompt in");
            Capture("/tmp/robot-hunt-gameplay.png"); yield return new WaitForSecondsRealtime(.2f);
            Check(hunt.TryPickupNearest(), "Existing pickup accepts target"); Check(prompt.group.alpha == 0, "Successful interaction hides prompt immediately");
            yield return new WaitForSecondsRealtime(1);
            Check(hunt.CollectedCount == 1, "Existing pickup updates manager found-state");
            Check(root.GetComponent<TargetHUD>().checks[0].gameObject.activeSelf, "HUD marks retrieved target with checkmark");
            Check(gate.movement.ControlEnabled, "Pickup restores movement when animation completes");
            Check(PlayerPrefs.GetInt("RobotHunt.Tutorial.v1.2", 0) == 1, "Completed retrieval persists interaction lesson");
            target = hunt.ActiveTargets[1]; Teleport(gate, target.transform.position + Vector3.right); yield return null;
            Check(hunt.TryPickupNearest(), "Begin in-flight pickup before restart"); yield return new WaitForSecondsRealtime(.1f);
            state.Open(UIScreen.Pause); root.restart.onClick.Invoke(); root.confirmAction.onClick.Invoke();
            Check(state.Current == UIScreen.Intro && hunt.CollectedCount == 0, "Confirmed restart clears progress and enters intro");
            while (!state.IsGameplay) yield return null;
            Check(root.GetComponent<TargetHUD>().states[0].text == "0", "New round resets HUD found-state");
            Check(gate.movement.ControlEnabled, "Restart cancels in-flight pickup without retaining its movement lock");
            Check(FindObjectsByType<CollectibleTarget>(FindObjectsSortMode.None).Length == 3, "Preview visuals never add gameplay collectible objects");
            for (int i = 0; i < 3; i++)
            {
                target = hunt.ActiveTargets[i]; Teleport(gate, target.transform.position + Vector3.right); yield return null;
                Check(hunt.TryPickupNearest(), "Retrieve round target " + i); yield return new WaitForSecondsRealtime(.9f);
            }
            yield return new WaitForSecondsRealtime(1.6f);
            Check(state.Current == UIScreen.Result && hunt.CollectedCount == 3, "All targets transition to Result without scene load");
            Check(state.result.count.text == "3 / 3" && state.result.score.text.Contains(state.loop.LastResult.roundScore.ToString()), "Result uses actual target count and existing round score");
            Check(!gate.movement.enabled && Cursor.visible && Cursor.lockState == CursorLockMode.None, "Result gates input and unlocks cursor");
            Capture("/tmp/robot-hunt-result.png"); yield return new WaitForSecondsRealtime(.3f);
            root.nextRound.onClick.Invoke(); Check(state.Current == UIScreen.Intro, "Next Round goes through intro again");
            while (!state.IsGameplay) yield return null;
            Check(gate.movement.ControlEnabled, "Movement restored after result and next round");
            // Exercise existing timeout/failure path without waiting ten minutes.
            var timer = typeof(ObjectHuntRoundManager).GetField("timeRemaining", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            timer.SetValue(hunt, .01f); yield return new WaitForSecondsRealtime(.1f);
            Check(state.Current == UIScreen.Result && state.result.title.text == "ROUND OVER", "Existing timer failure enters result with gameplay gated");
            root.returnToHub.onClick.Invoke(); Check(state.Current == UIScreen.Hub, "Result Return to Lobby opens Hub");
            root.startGame.onClick.Invoke(); while (!state.IsGameplay) yield return null;
            state.Open(UIScreen.Pause); root.quit.onClick.Invoke(); Check(state.Current == UIScreen.Confirm, "Quit requires confirmation");
            root.confirmAction.onClick.Invoke();
            Check(state.Current == UIScreen.Hub && hunt.ActiveTargets.Count == 0 && !hunt.IsRoundActive, "Confirmed quit returns to Hub and ends round");
            Check(Mathf.Approximately(settings.Sensitivity, 1.7f), "Settings preserved between rounds and Hub");
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
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
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
        private static InteractionStateAdapter source(UIRootController root) => root.GetComponent<InteractionStateAdapter>();
        private static void Teleport(GameplayInputGate gate, Vector3 position)
        {
            var cc = gate.movement.GetComponent<CharacterController>(); cc.enabled = false; gate.movement.transform.position = position; cc.enabled = true;
        }
        private static void Escape() { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Escape)); }
        private static void Release() { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); }
        private void Finish(string outcome)
        {
            if (finished) return; finished = true; results.Add(outcome);
            Application.logMessageReceived -= Log;
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            if (settings != null) { settings.SetMasterVolume(master); settings.SetMusicVolume(music); settings.SetSFXVolume(sfx); settings.SetSensitivity(sensitivity); }
            foreach (var entry in tutorials) { if (entry.Value < 0) PlayerPrefs.DeleteKey(entry.Key); else PlayerPrefs.SetInt(entry.Key, entry.Value); } PlayerPrefs.Save();
            File.WriteAllLines("Documentation/RobotHunt-UI-Playmode.txt", results);
            bool failed = results.Exists(s => s.StartsWith("FAIL") || s.StartsWith("RUNTIME ERROR"));
            Debug.Log("ROBOT_HUNT_PLAY_TESTS_" + (failed ? "FAILED" : "PASSED"));
            Time.timeScale = 1;
            EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
#endif
