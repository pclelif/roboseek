#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Robot.Core;
using Robot.Environment;
using Robot.Input;
using Robot.NPC;
using Robot.ObjectHunt;
using Robot.Player.CameraControl;
using Robot.Player.Movement;
using Robot.UI.Production;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MapPlayValidationProbe : MonoBehaviour
{
    private readonly StringBuilder report = new StringBuilder();
    private RobotMovementController movement;
    private PlayerInputReader input;
    private UIRootController ui;
    private MapLevelLayout layout;
    private float deadline;
    private int originalVsync, originalFrameRate;
    private float originalCapture;
    private void Start()
    {
        DontDestroyOnLoad(gameObject); deadline = Time.realtimeSinceStartup + 800;
        originalVsync=QualitySettings.vSyncCount;originalFrameRate=Application.targetFrameRate;originalCapture=Time.captureDeltaTime;
        QualitySettings.vSyncCount=0;Application.targetFrameRate=120;Time.captureDeltaTime=1f/60;
        StartCoroutine(SafeRun());
    }
    private void Update() { if(Time.realtimeSinceStartup>deadline) Finish(false,"timeout"); }
    private IEnumerator SafeRun()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            object next=null;bool step;
            try
            {
                step=stack.Peek().MoveNext();
                if(!step){stack.Pop();continue;}
                next=stack.Peek().Current;
            }
            catch(Exception e){Finish(false,e.ToString());yield break;}
            if(next is IEnumerator nested)stack.Push(nested);
            else yield return next;
        }
        Finish(true,"All four lobby transitions, map UI, targets and traversal checks passed.");
    }
    private void Require(bool value,string message)
    {
        if(!value)throw new Exception(message);
        report.AppendLine("PASS "+message);Debug.Log("MAP VALIDATION: "+message);
    }
    private IEnumerator Run()
    {
        foreach(var id in new[]{MapType.PolygonStarter,MapType.Adventure,MapType.Polygon,MapType.City}
            .Where(id=>string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("ROBOSEEK_TEST_MAP")) || id.ToString()==System.Environment.GetEnvironmentVariable("ROBOSEEK_TEST_MAP")))
        {
            yield return new WaitForSecondsRealtime(.5f);
            ui=FindFirstObjectByType<UIRootController>();
            Require(ui!=null && SceneManager.GetActiveScene().name=="RoboSeek_Lobby","Lobby available before "+id);
            MapManager.SetSelectedMap(id);
            var map=MapManager.SelectedMap;
            ui.startGame.onClick.Invoke();
            float end=Time.realtimeSinceStartup+35;
            do {yield return null;ui=FindFirstObjectByType<UIRootController>();}
            while((ui==null||!ui.state.IsGameplay)&&Time.realtimeSinceStartup<end);
            Require(ui!=null&&ui.state.IsGameplay,"Reached gameplay from lobby: "+map.displayName);
            movement=ui.state.inputGate.movement;input=movement.GetComponent<PlayerInputReader>();layout=FindFirstObjectByType<MapLevelLayout>();
            var hunt=ui.state.inputGate.hunt;
            Require(movement.IsGrounded,"Grounded path spawn: "+map.displayName);
            Require(movement.Traversal==map.Traversal,"Correct ability: "+map.Traversal);
            Require(Camera.main.GetComponent<CinemachineBrain>()!=null&&FindFirstObjectByType<ThirdPersonCameraController>().GetComponent<CinemachineCamera>().Follow==movement.transform,"Stable camera follows movement root");
            Require(hunt.ActiveTargets.Count==12,"12 spawned toys: "+map.displayName);
            Require(ui.state.intro!=null&&ui.state.result!=null&&ui.pause!=null&&ui.settings!=null,"Full City-style round/pause/settings/result UI");
            if(id!=MapType.City)
            {
                Require(layout!=null&&layout.toySpawns.Length==12,"12 authored terrain locations");
                Require(ui.GetComponent<PoliceRadarUI>()==null&&ui.GetComponent<TaxiSpeedUI>()==null&&ui.GetComponent<VehicleCountdownHUD>()==null,"City vehicle panels absent");
                Require(ui.state.gameplayHUD.transform.Find("MovementAbility")!=null,"Map ability HUD present");
                Require(ui.state.gameplayHUD.transform.Find("PlayerHealth")!=null,"Health HUD present");
                Require(Vector4.Distance(UIView.Accent,map.themeColor)<.001f,"Map accent matches selected world");
            }
            foreach(var npc in FindObjectsByType<NpcRobotController>(FindObjectsSortMode.None))npc.gameObject.SetActive(false);
            yield return CheckPause();
            if(id==MapType.PolygonStarter)
            {
                yield return CheckJetpack();
                foreach(var point in layout.toySpawns.Skip(4)) yield return FlyTo(point.position + Vector3.right * 1.5f);
            }
            if(id==MapType.Adventure)yield return CheckDoubleJump();
            if(id==MapType.Polygon)yield return CheckDash();
            if(id==MapType.Adventure||id==MapType.Polygon)
                foreach(var pad in FindObjectsByType<TraversalLaunchPad>(FindObjectsSortMode.None))yield return CheckPad(pad);
            movement.ReturnToSpawn();input.ClearBufferedInput();yield return null;
            Capture("/tmp/roboseek-play-"+map.displayName+".png");
            if(id!=MapType.City)
            {
                foreach(var definition in hunt.SelectedTargets.ToArray())
                {
                    var target=hunt.ActiveTargets.First(t=>t.Definition.objectId==definition.objectId);
                    Teleport(target.transform.position+Vector3.right*1.3f+Vector3.up*.2f);
                    yield return null;
                    Require(hunt.TryPickupNearest(),"Collect selected toy "+definition.objectId);
                    yield return new WaitForSecondsRealtime(.2f);
                }
                float resultDeadline=Time.realtimeSinceStartup+8;
                while(ui.state.Current!=UIScreen.Result&&Time.realtimeSinceStartup<resultDeadline)yield return null;
                Require(ui.state.Current==UIScreen.Result,"Round result shown: "+map.displayName);
                ui.state.RestartRound();
                float restartDeadline=Time.realtimeSinceStartup+30;
                while(!ui.state.IsGameplay&&Time.realtimeSinceStartup<restartDeadline)yield return null;
                Require(ui.state.IsGameplay&&hunt.ActiveTargets.Count==12,"Restart restores 12 targets and ability: "+map.displayName);
            }
            ui.state.ReturnToHub();yield return null;
            Require(SceneManager.GetActiveScene().name=="RoboSeek_Lobby","Return to lobby: "+map.displayName);
        }
    }
    private IEnumerator CheckPause()
    {
        input.ClearBufferedInput();var position=movement.transform.position;
        ui.state.Open(UIScreen.Pause);float fuel=movement.GetComponent<RobotJetpackController>()?.CurrentFuel??0;
        input.RequestDash();input.RequestJump();input.SetMobileJumpHeld(true);
        yield return new WaitForSecondsRealtime(.2f);
        Require(Vector3.Distance(position,movement.transform.position)<.01f&&!movement.enabled,"Pause blocks movement and traversal");
        Require(Mathf.Abs((movement.GetComponent<RobotJetpackController>()?.CurrentFuel??0)-fuel)<.001f,"Pause preserves fuel");
        ui.state.Open(UIScreen.Controls);yield return null;
        if(!MapManager.SelectedMap.enableCityMechanics)
            Require(!ui.controls.keys[7].transform.parent.parent.gameObject.activeSelf,"City turbo binding hidden");
        ui.state.Resume();yield return null;input.ClearBufferedInput();
        Require(movement.enabled&&ui.state.IsGameplay,"Resume restores gameplay");
    }
    private IEnumerator CheckJetpack()
    {
        movement.ReturnToSpawn();yield return null;var pack=movement.GetComponent<RobotJetpackController>();float startY=movement.transform.position.y;
        input.RequestJump();input.SetMobileJumpHeld(true);yield return new WaitForSeconds(1.2f);
        Require(movement.transform.position.y>startY+5&&pack.FuelNormalized<.85f,"Jetpack climbs and consumes fuel");
        yield return new WaitForSeconds(3.7f);
        Require(pack.CurrentFuel<=.01f&&!pack.IsFlying,"Jetpack cannot thrust on empty fuel");
        input.SetMobileJumpHeld(false);yield return new WaitForSeconds(.3f);
        Require(pack.IsGliding&&movement.VerticalVelocity>=-4.5f,"Released/exhausted jetpack glides");
        Teleport(layout.playerSpawn.position);
        yield return new WaitForSeconds(1f);
        Require(pack.CurrentFuel>.5f,"Landing recharges fuel");
        movement.ReturnToSpawn();yield return null;
    }
    private IEnumerator FlyTo(Vector3 destination)
    {
        var pack=movement.GetComponent<RobotJetpackController>();
        input.SetMobileRunHeld(true);input.RequestJump();float time=0;bool reachedAltitude=false;
        while(time<12)
        {
            Vector3 delta=destination-movement.transform.position;delta.y=0;
            var camera=FindFirstObjectByType<ThirdPersonCameraController>();
            input.SetMovementInput(delta.magnitude>.4f?new Vector2(Vector3.Dot(delta.normalized,camera.CameraRight),Vector3.Dot(delta.normalized,camera.CameraForward)):Vector2.zero);
            reachedAltitude |= movement.transform.position.y > destination.y + 1.1f;
            input.SetMobileJumpHeld(!reachedAltitude || delta.magnitude > 3);
            if(movement.IsGrounded&&Mathf.Abs(movement.transform.position.y-destination.y)<.3f&&delta.magnitude<2.4f)break;
            time+=Time.deltaTime;yield return null;
        }
        input.ClearBufferedInput();
        Require(movement.IsGrounded&&Mathf.Abs(movement.transform.position.y-destination.y)<.3f,"Flight route landing "+destination+" actual="+movement.transform.position);
        yield return new WaitForSeconds(2.1f);
        Require(pack.FuelNormalized>.95f,"Deck landing fully refuels");
    }
    private IEnumerator CheckDoubleJump()
    {
        movement.ReturnToSpawn();yield return null;float baseY=movement.transform.position.y;
        input.RequestJump();yield return new WaitForSeconds(.25f);float first=movement.VerticalVelocity;
        input.RequestJump();yield return null;
        Require(movement.VerticalVelocity>first+1&&movement.AirJumpsRemaining==0,"Second jump adds upward velocity");
        yield return new WaitForSeconds(.1f);float beforeThird=movement.VerticalVelocity;input.RequestJump();yield return null;
        Require(movement.VerticalVelocity<=beforeThird,"Third jump is rejected");
        yield return new WaitForSeconds(.25f);Require(movement.transform.position.y>baseY+2,"Double jump clears ordinary jump height");
        movement.ReturnToSpawn();yield return null;Vector3 start=movement.transform.position;input.SetMovementInput(Vector2.up);yield return new WaitForSeconds(.8f);float walk=FlatDistance(start,movement.transform.position);
        input.ClearBufferedInput();movement.ReturnToSpawn();yield return null;start=movement.transform.position;input.SetMovementInput(Vector2.up);input.SetMobileRunHeld(true);yield return new WaitForSeconds(.8f);float run=FlatDistance(start,movement.transform.position);
        input.ClearBufferedInput();Require(run>walk*1.5f,"Adventure sprint is faster than walking");movement.ReturnToSpawn();yield return null;
    }
    private IEnumerator CheckDash()
    {
        movement.ReturnToSpawn();yield return null;var start=movement.transform.position;input.RequestDash();yield return new WaitForSeconds(.25f);
        float distance=FlatDistance(start,movement.transform.position);Require(distance>1&&distance<3.6f,"Dash advances once without teleporting: "+distance);
        start=movement.transform.position;input.RequestDash();yield return new WaitForSeconds(.2f);Require(FlatDistance(start,movement.transform.position)<.1f,"Dash cooldown blocks repeated input");
        movement.ReturnToSpawn();yield return null;start=movement.transform.position;var forward=FindFirstObjectByType<ThirdPersonCameraController>().CameraForward;
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=start+forward*2+Vector3.up*1.5f;wall.transform.localScale=new Vector3(2,3,.5f);Physics.SyncTransforms();
        input.RequestDash();yield return new WaitForSeconds(.25f);Require(FlatDistance(start,movement.transform.position)<1.6f,"Dash respects solid wall collisions");Destroy(wall);movement.ReturnToSpawn();yield return null;
    }
    private IEnumerator CheckPad(TraversalLaunchPad pad)
    {
        input.ClearBufferedInput();Teleport(pad.transform.position+Vector3.up*.8f);
        Require(pad.TryLaunch(movement),"Launch pad activated: "+pad.name);
        float time=0;
        do{time+=Time.deltaTime;yield return null;}while(time<4&&(!movement.IsGrounded||time<.5f));
        Require(movement.IsGrounded&&Mathf.Abs(movement.transform.position.y-pad.landingPosition.y)<.4f&&FlatDistance(movement.transform.position,pad.landingPosition)<2.5f,"Pad lands on its lookout: "+pad.name+" actual="+movement.transform.position+" expected="+pad.landingPosition);
    }
    private void Teleport(Vector3 position)
    {
        var cc=movement.GetComponent<CharacterController>();cc.enabled=false;movement.transform.position=position;cc.enabled=true;Physics.SyncTransforms();
    }
    private static float FlatDistance(Vector3 a,Vector3 b){a.y=b.y;return Vector3.Distance(a,b);}
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
    private void Finish(bool success,string message)
    {
        StopAllCoroutines();input?.ClearBufferedInput();
        QualitySettings.vSyncCount=originalVsync;Application.targetFrameRate=originalFrameRate;Time.captureDeltaTime=originalCapture;
        report.AppendLine((success?"PASS ":"FAIL ")+message);File.WriteAllText("/tmp/roboseek-map-validation.txt",report.ToString());
        Debug.Log("MAP VALIDATION: "+(success?"PASS ":"FAIL ")+message);
        SessionState.SetBool("PlayableMaps.Validation",false);EditorApplication.Exit(success?0:1);
    }
}
#endif
