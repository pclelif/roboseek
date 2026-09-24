#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Robot.UI.Production;
using Robot.ObjectHunt;
using System.Linq;
using System.Reflection;
[InitializeOnLoad]
public static class TaxiDiagnosticTemporary
{
    [System.Serializable] private class Setup { public SceneSetup[] scenes; }
    static double next = EditorApplication.timeSinceStartup + 5;
    static int stage;
    const string Key = "TaxiDiagnosticTemporary";
    const string Output = "/private/tmp/roboseek-taxi-play.txt";
    static TaxiDiagnosticTemporary()
    {
        EditorApplication.delayCall += Begin;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += s => {
            if (s == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key, false)) {
                SessionState.SetBool(Key, false);
                var setup = JsonUtility.FromJson<Setup>(SessionState.GetString(Key + "Setup", ""));
                if (setup.scenes.Length > 0 && setup.scenes.All(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup.scenes);
                else EditorSceneManager.OpenScene("Assets/_Project/Scenes/RoboSeek_Lobby.unity");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        };
    }
    static void Begin()
    {
        if (SessionState.GetBool(Key, false) || System.IO.File.Exists(Output)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { System.IO.File.WriteAllText(Output, "Skipped: already playing"); return; }
        var scenes = EditorSceneManager.GetSceneManagerSetup();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) { System.IO.File.WriteAllText(Output, "Skipped: unsaved scene"); return; }
        SessionState.SetString(Key + "Setup", JsonUtility.ToJson(new Setup { scenes = scenes }));
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Maps/RoboSeek_City.unity");
        EditorApplication.EnterPlaymode();
    }
    static object Field(object o, string name) => o == null ? null : o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(o);
    static void Log(string text) => System.IO.File.AppendAllText(Output, text + "\n");
    static void Tick()
    {
        if (SessionState.GetBool(Key, false) && EditorApplication.isPaused) EditorApplication.isPaused = false;
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 1;
        try {
            var root = Object.FindFirstObjectByType<UIRootController>();
            var loop = Object.FindFirstObjectByType<RoundGameLoop>();
            var taxi = root != null ? root.GetComponent<TaxiSpeedUI>() : null;
            if (stage == 0) { Log("root=" + root + " taxi=" + taxi + " loop=" + loop); if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>(); loop.StartGame(); stage++; return; }
            if (stage == 1) {
                if (Time.timeSinceLevelLoad > 35) { Log("Timeout phase=" + loop.CurrentPhase); EditorApplication.ExitPlaymode(); return; }
                if (loop.CurrentPhase != RoundPhase.Search) return;
                var player = root.state.inputGate.movement;
                var car = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("SM_Veh_Car_Taxi_") && !r.name.Contains("Wheel") && !r.name.Contains("Glass") && !r.name.Contains("Plates") && !r.name.Contains("Steering")).OrderBy(r => Vector3.Distance(r.transform.position, player.transform.position)).First();
                var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
                player.transform.position = new Vector3(car.bounds.max.x + 1f, car.bounds.min.y + .1f, car.bounds.center.z);
                cc.enabled = true; Physics.SyncTransforms();
                Log("car=" + car.name + " bounds=" + car.bounds + " player=" + player.transform.position);
                stage++; return;
            }
            if (stage == 2) {
                Log("gameplay=" + root.state.IsGameplay + " hunt=" + Field(taxi,"hunt") + " movement=" + Field(taxi,"movement") + " vehicle=" + Field(taxi,"taxiVehicle") + " transform=" + Field(taxi,"taxiTransform") + " used=" + Field(taxi,"usedThisRound"));
                var panel = Field(taxi,"promptPanel") as UnityEngine.UI.Image;
                Log("prompt=" + panel + " active=" + (panel != null && panel.gameObject.activeInHierarchy));
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(UnityEngine.InputSystem.Key.T)); stage++; return;
            }
            Log("after T used=" + Field(taxi,"usedThisRound") + " boostUntil=" + Field(root.state.inputGate.movement,"boostUntil") + " time=" + Time.time);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            EditorApplication.ExitPlaymode();
        } catch (System.Exception e) { Log(e.ToString()); EditorApplication.ExitPlaymode(); }
    }
}
#endif
