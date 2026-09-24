#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Robot.UI.Production;
using Robot.Player.Movement;
using Robot.ObjectHunt;
using Robot.Combat;

public sealed class CriticalFixesProbe : MonoBehaviour
{
    private float deadline;
    private void Start() { DontDestroyOnLoad(gameObject); deadline = Time.realtimeSinceStartup + 120; StartCoroutine(Safe()); }
    private void Update() { if (Time.realtimeSinceStartup > deadline) { Debug.LogError("CRITICAL TEST TIMEOUT"); EditorApplication.Exit(1); } }
    private IEnumerator Safe()
    {
        var run = Run();
        while (true)
        {
            object next;
            try { if (!run.MoveNext()) break; next = run.Current; }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
            yield return next;
        }
        Debug.Log("CRITICAL TESTS ALL PASSED");
        EditorApplication.Exit(0);
    }
    private static void Capture(string path)
    {
        typeof(RoboSeekRevisionProbe).GetMethod("Capture", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[] { path });
    }
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); Debug.Log("CRITICAL PASS: " + label); }
    private IEnumerator Run()
    {
        Application.runInBackground = true;
        UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
        var testKeyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        yield return new WaitForSecondsRealtime(1);
        var root = FindFirstObjectByType<UIRootController>();
        root.hub.SelectMultiplayer();
        Check(!root.startGame.interactable && root.startGame.image.color.r == root.startGame.image.color.g, "Multiplayer disables and greys Start Game");
        root.hub.StartGame();
        Check(root.state.Current == UIScreen.Hub, "Direct multiplayer start is blocked");
        Check(root.transform.Find("LobbyScreen/RightControlDeck").GetComponent<RectTransform>().anchoredPosition.y == 35, "Lobby card positioned comfortably");
        Capture("/tmp/robot-critical-lobby.png");
        yield return new WaitForSecondsRealtime(.3f);
        root.hub.SelectSolo();
        root.hub.StartGame();
        yield return new WaitForSecondsRealtime(14);
        root = FindFirstObjectByType<UIRootController>();
        var movement = root.state.inputGate.movement;
        var cc = movement.GetComponent<CharacterController>();
        var hunt = FindFirstObjectByType<ObjectHuntRoundManager>();
        Check(hunt.IsRoundActive, "Gameplay search started");
        var spawn = movement.transform.position;
        Check(root.state.gameplayHUD.transform.Find("PlayerHealth/HealthTrack/HealthFill") != null, "Kenney health bar attached to gameplay HUD");
        var health = movement.GetComponent<CombatHealth>();
        health.TakeDamage(20, null);
        yield return null;
        var fill = root.state.gameplayHUD.transform.Find("PlayerHealth/HealthTrack/HealthFill").GetComponent<UnityEngine.UI.Image>();
        Check(fill.sprite != null && fill.fillAmount < 1, "Health bar has sprite and responds to damage");
        health.Heal(health.MaxHealth);
        yield return null;
        Check(fill.fillAmount == 1, "Health bar responds to healing");
        foreach (var item in hunt.ActiveTargets)
            Check(item.GetComponentsInChildren<Collider>().Any(c => c.enabled && !c.isTrigger), "Solid toy: " + item.name);
        movement.SetControlEnabled(false);
        var ambo = FindObjectsByType<Collider>(FindObjectsSortMode.None).First(c => c.enabled && !c.isTrigger && c.name.Contains("Ambo"));
        cc.enabled = false;
        movement.transform.position = ambo.bounds.center + Vector3.right * (ambo.bounds.extents.x + 1f);
        cc.enabled = true;
        health.TakeDamage(25,null);
        yield return new WaitForSecondsRealtime(.3f);
        Check(root.state.gameplayHUD.transform.Find("AmbulancePrompt").gameObject.activeInHierarchy, "Real Ambo vehicle shows healing prompt");
        // Batch mode has no focused OS keyboard. Inject a device state and evaluate
        // the normal HUD input handler in the same input frame.
        testKeyboard.MakeCurrent();
        UnityEngine.InputSystem.InputSystem.EnableDevice(testKeyboard);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(testKeyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.H));
        UnityEngine.InputSystem.InputSystem.Update();
        var vitals = root.GetComponent<PlayerVitalsUI>();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(PlayerVitalsUI).GetMethod("Update",flags).Invoke(vitals,null);
        Check(vitals.IsHealing && health.CurrentHealth < health.MaxHealth, "H begins gradual healing at the ambulance");
        UnityEngine.InputSystem.LowLevel.InputState.Change(testKeyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        Capture("/tmp/robot-critical-health.png");
        yield return new WaitForSecondsRealtime(.3f);
        var wrong = hunt.ActiveTargets.First(t => !hunt.SelectedTargets.Any(s => s.objectId == t.Definition.objectId));
        cc.enabled = false; movement.transform.position = wrong.transform.position + Vector3.right * 2; cc.enabled = true;
        Physics.SyncTransforms();
        int count = hunt.CollectedCount;
        Check(!hunt.TryPickupNearest(), "Wrong item rejected");
        Check(wrong.GetComponent<WrongItemFeedback>() != null && hunt.CollectedCount == count, "Wrong feedback runs without collection");
        yield return new WaitForSecondsRealtime(.5f);
        Check(wrong.GetComponentsInChildren<MeshRenderer>().All(r => r.enabled), "Rejection restores renderers");
        // Isolated, high-displacement collision checks exercise real CharacterController sweeps.
        movement.SetControlEnabled(false);
        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.transform.position = new Vector3(10000,10001,10003);
        obstacle.transform.localScale = new Vector3(6,4,.1f);
        cc.enabled = false; movement.transform.position = new Vector3(10000,10000,10000); cc.enabled = true;
        var safety = movement.GetComponent<WorldSafety>(); if (safety != null) safety.enabled = false;
        Physics.SyncTransforms(); cc.Move(Vector3.forward * 10);
        Check(movement.transform.position.z < 10003, "Thin solid obstacle blocks a 10 metre movement sweep");
        Destroy(obstacle);
        var npc = FindFirstObjectByType<Robot.NPC.NpcRobotController>();
        Check(npc.GetComponent<CharacterController>() != null && !npc.GetComponent<UnityEngine.AI.NavMeshAgent>().updatePosition, "NPC collision controller owns NavMesh movement");
        npc.enabled = false;
        npc.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
        var npcBody = npc.GetComponent<CharacterController>();
        npcBody.enabled = false; npc.transform.position = new Vector3(10000,10000,10003); npcBody.enabled = true;
        cc.enabled = false; movement.transform.position = new Vector3(10000,10000,10000); cc.enabled = true;
        Physics.SyncTransforms(); cc.Move(Vector3.forward * 10);
        Check(movement.transform.position.z < 10003, "Player cannot sweep through another robot");
        cc.enabled = false; movement.transform.position = new Vector3(10000,10000,10000); cc.enabled = true;
        Physics.SyncTransforms(); npcBody.Move(Vector3.back * 10);
        Check(npc.transform.position.z > 10000, "NPC cannot sweep through the player");
        var toyBounds = wrong.GetComponentsInChildren<Collider>().First(c => c.enabled && !c.isTrigger).bounds;
        wrong.transform.position += new Vector3(10000,10000,10003) - new Vector3(toyBounds.center.x,toyBounds.min.y,toyBounds.center.z);
        npcBody.enabled = false; npc.transform.position = new Vector3(10000,10000,10000); npcBody.enabled = true;
        cc.enabled = false;
        Physics.SyncTransforms(); npcBody.Move(Vector3.forward * 10);
        Check(npc.transform.position.z < 10003, "NPC cannot sweep through a solid toy");
        cc.enabled = true;
        root.state.loop.RestartRound();
        Check(Vector2.Distance(new Vector2(movement.transform.position.x,movement.transform.position.z),new Vector2(spawn.x,spawn.z)) < .1f, "Restart returns to original spawn after teleport");
        yield return null;
    }
}
#endif
