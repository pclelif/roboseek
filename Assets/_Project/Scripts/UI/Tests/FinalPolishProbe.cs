#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Robot.UI.Production;
using Robot.Player.Movement;
using Robot.Combat;
using Robot.NPC;
using Robot.ObjectHunt;
using UnityEngine.AI;

public sealed class FinalPolishProbe : MonoBehaviour
{
    private float deadline;
    private readonly List<string> results = new List<string>();
    private void Start() { DontDestroyOnLoad(gameObject); deadline=Time.realtimeSinceStartup+150; StartCoroutine(Safe()); }
    private void Update() { if(Time.realtimeSinceStartup>deadline) Finish("TIMEOUT",1); }
    private void Finish(string message,int code)
    {
        results.Add(message); File.WriteAllLines("Documentation/FinalPolishValidation.txt",results);
        Debug.Log("FINAL POLISH: "+message); EditorApplication.Exit(code);
    }
    private IEnumerator Safe()
    {
        var iterator=Run();
        while(true)
        {
            object next;
            try { if(!iterator.MoveNext())break; next=iterator.Current; }
            catch(Exception e) { Debug.LogException(e); Finish(e.ToString(),1); yield break; }
            yield return next;
        }
        Finish("ALL CHECKS PASSED",0);
    }
    private void Check(bool test,string name) { if(!test)throw new Exception(name); results.Add("PASS "+name); Debug.Log("FINAL PASS "+name); }
    private static void Capture(string path) => typeof(RoboSeekRevisionProbe).GetMethod("Capture",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{path});
    private static void Teleport(CharacterController cc,Vector3 position)
    {
        cc.enabled=false; cc.transform.position=position; cc.enabled=true;
        cc.GetComponent<SolidRobotBody>()?.RefreshHull(); Physics.SyncTransforms();
    }
    private IEnumerator Run()
    {
        Application.runInBackground = true;
        UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        yield return new WaitForSecondsRealtime(1);
        var root=FindFirstObjectByType<UIRootController>();
        Check(root.transform.Find("LobbyScreen/RightControlDeck").GetComponent<RectTransform>().anchoredPosition.y==35,"Lobby deck positioned comfortably");
        root.controls.Refresh();
        Check(root.controls.keys.Length==7 && root.controls.keys[5].text=="H" && root.controls.keys[6].text=="ESC","Heal before Pause in Controls");
        Capture("/tmp/robot-final-lobby.png");
        root.hub.SelectSolo(); root.hub.StartGame();
        yield return new WaitForSecondsRealtime(14);
        root=FindFirstObjectByType<UIRootController>();
        var player=root.state.inputGate.movement;
        var cc=player.GetComponent<CharacterController>();
        var health=player.GetComponent<CombatHealth>();
        var vitals=root.GetComponent<PlayerVitalsUI>();
        var timer=root.state.gameplayHUD.transform.Find("RoundTimer").GetComponent<RectTransform>();
        var panel=root.state.gameplayHUD.transform.Find("PlayerHealth").GetComponent<RectTransform>();
        Check(panel.sizeDelta.x==timer.sizeDelta.x && panel.anchoredPosition.x==timer.anchoredPosition.x && panel.anchoredPosition.y==timer.anchoredPosition.y-timer.sizeDelta.y-12,"Health aligned beneath Time at equal width");
        Check(panel.Find("HealthValue").GetComponent<Text>().text=="HEALTH","Health heading contains no numbers");
        var spawn=player.transform.position;
        var rotation=player.transform.rotation;
        var cameraPosition=Camera.main.transform.position;
        var cameraRotation=Camera.main.transform.rotation;
        Capture("/tmp/robot-final-hud.png");
        player.SetControlEnabled(false);
        var ambo=FindObjectsByType<Collider>(FindObjectsSortMode.None).First(c=>c.enabled&&!c.isTrigger&&c.name.Contains("Ambo"));
        Teleport(cc,ambo.bounds.center+Vector3.right*(ambo.bounds.extents.x+1));
        health.TakeDamage(120,null);
        yield return new WaitForSecondsRealtime(.3f);
        var prompt=root.state.gameplayHUD.transform.Find("AmbulancePrompt");
        Check(prompt.gameObject.activeInHierarchy && prompt.Find("Keycap/Face/Key").GetComponent<Text>().text=="H","Ambulance uses shared yellow keycap");
        Check(!prompt.Find("HealHint").GetComponent<Text>().text.Contains("[H]"),"No plain H brackets");
        keyboard.MakeCurrent();
        UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.H));
        UnityEngine.InputSystem.InputSystem.Update();
        typeof(PlayerVitalsUI).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(vitals,null);
        Check(vitals.IsHealing && health.CurrentHealth<health.MaxHealth,"H begins gradual healing without instant full health");
        UnityEngine.InputSystem.LowLevel.InputState.Change(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
        var track=panel.Find("HealthTrack").GetComponent<RectTransform>();
        var previous=track.anchoredPosition;
        float before=health.CurrentHealth;
        yield return new WaitForSecondsRealtime(.3f);
        Check(health.CurrentHealth>before && health.CurrentHealth<health.MaxHealth,"Health increases progressively");
        Check(track.anchoredPosition!=previous && track.Find("HealthFill").GetComponent<Image>().sprite.name=="HealthBarGreen","Green healing fill shakes");
        Teleport(cc,spawn);
        yield return null;
        Check(!vitals.IsHealing,"Leaving ambulance stops healing");
        bool[,] matrix=new bool[32,32];
        for(int i=0;i<32;i++)for(int j=0;j<32;j++)matrix[i,j]=Physics.GetIgnoreLayerCollision(i,j);
        var npc=FindFirstObjectByType<NpcRobotController>();
        var npcCC=npc.GetComponent<CharacterController>();
        var npcHealth=npc.GetComponent<CombatHealth>();
        npc.enabled=false; npc.GetComponent<NavMeshAgent>().enabled=false;
        var safety=player.GetComponent<WorldSafety>(); safety.enabled=false;
        Teleport(npcCC,new Vector3(10000,10000,10000));
        Teleport(cc,new Vector3(10000,10000,9998));
        player.transform.rotation=Quaternion.identity;
        player.GetComponent<CombatAttack>().TryAttack(npc.transform);
        yield return new WaitForSeconds(.5f);
        Check(npc.GetComponent<SolidRobotBody>().Hull.enabled&&!npc.GetComponent<SolidRobotBody>().Hull.isTrigger,"Attack preserves solid hull");
        npcHealth.TakeDamage(npcHealth.MaxHealth,player.gameObject);
        yield return new WaitForSeconds(1.5f);
        var hull=npc.GetComponent<SolidRobotBody>(); hull.RefreshHull();
        Check(npcHealth.IsKnockedOut && npcCC.enabled && npcCC.detectCollisions && hull.Hull.enabled&&!hull.Hull.isTrigger,"Knockout retains controller and solid body");
        foreach(var renderer in npc.GetComponentsInChildren<Renderer>())
            if(renderer.enabled) Check(hull.BodyBounds.Contains(renderer.bounds.min)&&hull.BodyBounds.Contains(renderer.bounds.max),"Fallen visual contained: "+renderer.name);
        foreach(var direction in new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left})
        {
            foreach(float elevation in new[]{0f,1.2f})
            {
                Vector3 origin=hull.BodyBounds.center+direction*5;
                origin.y=npc.transform.position.y+elevation;
                Teleport(cc,origin);
                SolidRobotBody.Move(cc,-direction*10);
                Check(Vector3.Dot(player.transform.position-hull.BodyBounds.center,direction)>0,"Fallen robot blocks approach / jump: "+direction+" height="+elevation);
            }
        }
        bool unchanged=true;
        for(int i=0;i<32;i++)for(int j=0;j<32;j++)unchanged &= matrix[i,j]==Physics.GetIgnoreLayerCollision(i,j);
        Check(unchanged,"All 1024 layer collision pairs unchanged after attack and knockout");
        npcHealth.ResetForRound();
        yield return null;
        Check(!npcHealth.IsKnockedOut && hull.Hull.enabled&&!hull.Hull.isTrigger,"Recovery keeps solid hull");
        health.TakeDamage(health.MaxHealth,null);
        Check(health.IsKnockedOut,"Player knockout prepared");
        root.state.loop.RestartRound();
        Check(health.CurrentHealth==health.MaxHealth&&!health.IsKnockedOut&&health.KnockoutTimeRemaining==0,"Restart resets health and knockout timer");
        yield return new WaitForSecondsRealtime(8);
        Check(Vector3.Distance(player.transform.position,spawn)<.2f && Quaternion.Angle(player.transform.rotation,rotation)<1,"GO restores spawn and robot rotation");
        Check(Vector3.Distance(Camera.main.transform.position,cameraPosition)<.3f && Quaternion.Angle(Camera.main.transform.rotation,cameraRotation)<2,"GO restores camera without orbit or damping history");
        Check(root.state.IsGameplay,"Restart returns to gameplay");
        Capture("/tmp/robot-final-restart.png");
    }
}
#endif
