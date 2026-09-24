#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robot.Core;
using Robot.Environment;
using Robot.Input;
using Robot.NPC;
using Robot.ObjectHunt;
using Robot.Player.CameraControl;
using Robot.Player.Movement;
using Robot.UI.Production;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PlayableMapBuilder
{
    private const string Generated = "Assets/_Project/Data/PlayableMaps";
    private static MapLevelLayout layout;
    private static Material accent, stone, dark;
    private static readonly List<Vector3> reserved = new List<Vector3>();

    [MenuItem("Robot Hunt/Maps/Build Three Playable Maps")]
    public static void BuildAll()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Generated);
        foreach (var id in new[] { MapType.PolygonStarter, MapType.Adventure, MapType.Polygon }) Build(id);
        AssetDatabase.SaveAssets();
        Debug.Log("PLAYABLE MAPS: BUILD PASS");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    private static void Build(MapType id)
    {
        var map = MapManager.GetMapDefinition(id);
        MapManager.SetSelectedMap(id);
        var scene = EditorSceneManager.OpenScene(map.scenePath);
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Map Gameplay" || root.name == "RobotPlayer" || root.name == "RobotThirdPersonCamera" || root.name == "Canvas_UI_Root" || root.name == "Shared Gameplay System")
                Object.DestroyImmediate(root);
        // Keep environment art, remove demo camera behaviours and legacy canvases.
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(camera.gameObject);
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(canvas.gameObject);
        foreach (var surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None)) Object.DestroyImmediate(surface);
        NavMesh.RemoveAllNavMeshData();
        Physics.SyncTransforms();
        layout = new GameObject("Map Gameplay").AddComponent<MapLevelLayout>(); layout.mapId = id;
        layout.playableBounds = id == MapType.PolygonStarter ? new Bounds(new Vector3(3,15,5),new Vector3(110,80,115)) :
            id == MapType.Adventure ? new Bounds(new Vector3(0,15,15),new Vector3(115,80,140)) : new Bounds(new Vector3(-15,20,12),new Vector3(155,100,160));
        layout.fallResetHeight = -8;
        accent = MaterialAsset(id + " Accent", map.themeColor, true);
        stone = MaterialAsset(id + " Structure", id == MapType.Adventure ? new Color(.34f,.25f,.13f) : id == MapType.Polygon ? new Color(.30f,.33f,.37f) : new Color(.16f,.23f,.31f));
        dark = MaterialAsset(id + " Trim", new Color(.065f,.085f,.11f));
        reserved.Clear();
        Vector3 seed = id == MapType.Adventure ? new Vector3(7.46f,0,12) : id == MapType.Polygon ? new Vector3(-.4f,0,39.4f) : new Vector3(-9,0,0);
        Vector3 spawn = GroundNear(seed, 8, id != MapType.PolygonStarter);
        layout.playerSpawn = Marker("Player Spawn - Walkable Path", spawn + Vector3.up * .04f);
        layout.playerSpawn.rotation = Quaternion.Euler(0,id == MapType.Polygon ? 180 : 0,0);
        reserved.Add(spawn);
        BuildSpawnSign(map, spawn);
        var points = new List<Vector3>();
        if (id == MapType.PolygonStarter) BuildArena(points);
        else if (id == MapType.Adventure) BuildAdventure(points);
        else BuildCastle(points);
        BakeNavigation(scene, id);
        ValidateWalkingTargets(points, spawn, id);
        layout.toySpawns = points.Select((p,i)=>Marker("Toy Location " + (i+1).ToString("00"),p)).ToArray();
        layout.npcSpawns = SelectNpcPoints(spawn).Select((p,i)=>Marker("NPC Spawn " + (i+1).ToString("00"),p + Vector3.up*.03f)).ToArray();
        InstallGameplay(map, spawn);
        RobotHuntUIInstaller.InstallInCurrentScene();
        var rootUI = Object.FindFirstObjectByType<UIRootController>();
        // The same layout as City, with the selected map's accent.
        rootUI.RefineGameplayUI();
        rootUI.GetComponent<TargetHUD>().ApplyLayout();
        rootUI.ApplyTypography();
        var pauseButton = UIView.Button("ESC  PAUSE",rootUI.state.gameplayHUD.transform,Vector2.zero,new Vector2(150,42));
        var rect=pauseButton.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-40,40);
        var relay=pauseButton.gameObject.AddComponent<MapPauseButton>();relay.state=rootUI.state;
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        ValidateLocations();
        CaptureOverview(map);
        Debug.Log($"PLAYABLE MAP: {map.displayName}; spawn={spawn}; targets={points.Count}; NPCs={layout.npcSpawns.Length}");
    }

    private static void BuildArena(List<Vector3> points)
    {
        foreach(var p in new[]{new Vector3(-5,0,5),new Vector3(8,0,10),new Vector3(1,0,19),new Vector3(12,0,-8)}) AddGround(points,p,6);
        // Each deck is a generous landing/refuelling stop. No jump exceeds 6m vertically.
        var decks=new[]{new Vector3(-12,4,6),new Vector3(-15,7,16),new Vector3(-7,10,25),new Vector3(4,13,28),new Vector3(16,16,23),new Vector3(24,19,12),new Vector3(18,15,1),new Vector3(8,10,-7)};
        for(int i=0;i<decks.Length;i++)
        {
            var p=decks[i]; Deck("Flight Deck "+(i+1),p,new Vector2(5.5f,5.5f),true);
            points.Add(p); reserved.Add(p);
            Sign("LAND TO REFUEL",p+new Vector3(0,.2f,2.5f),.07f);
        }
    }
    private static void BuildAdventure(List<Vector3> points)
    {
        foreach(var p in new[]{new Vector3(-1.5f,0,-15),new Vector3(6,0,-22),new Vector3(15,0,-22),new Vector3(25,0,-8),new Vector3(7.5f,0,23),new Vector3(15,0,34.5f),new Vector3(7.5f,0,40),new Vector3(15,0,47.5f),new Vector3(-12,0,-16)}) AddGround(points,p,6);
        // Village lookout decks are integrated beside buildings, reachable by double jump/pads.
        AddLaunchLookout(points,new Vector3(17,0,20),new Vector3(19,4.3f,23),"Village Rooftop",true);
        AddLaunchLookout(points,new Vector3(-1,0,33),new Vector3(-4,4.7f,34),"Market Rooftop",true);
        AddLaunchLookout(points,new Vector3(13,0,44),new Vector3(18,5.2f,43),"Forest Lookout",true);
    }
    private static void BuildCastle(List<Vector3> points)
    {
        foreach(var p in new[]{new Vector3(-9,0,57),new Vector3(11.5f,0,48.6f),new Vector3(20,0,42),new Vector3(-15.5f,0,48.6f),new Vector3(-6,0,39),new Vector3(6,0,57),new Vector3(-18,0,58),new Vector3(16,0,63),new Vector3(-10,0,67)}) AddGround(points,p,9);
        AddLaunchLookout(points,new Vector3(-7,0,36),new Vector3(-7,8,31),"Gate Battlement",false);
        AddLaunchLookout(points,new Vector3(-21,0,39),new Vector3(-25,8,35),"West Battlement",false);
        AddLaunchLookout(points,new Vector3(19,0,37),new Vector3(24,8,32),"Siege Battlement",false);
    }
    private static void AddGround(List<Vector3> points,Vector3 seed,float radius)
    {
        var p=GroundNear(seed,radius,false);points.Add(p);reserved.Add(p);
    }
    private static void AddLaunchLookout(List<Vector3> points,Vector3 padSeed,Vector3 deck,string name,bool mushroom)
    {
        // Raise the deck above any roof already occupying its footprint.
        if(Physics.Raycast(deck+Vector3.up*25,Vector3.down,out var hit,40,~0,QueryTriggerInteraction.Ignore))deck.y=Mathf.Max(deck.y,hit.point.y+.35f);
        Deck(name,deck,new Vector2(4.6f,4.6f),false);
        var pad=GroundNear(padSeed,10,false,deck.y+4,1.15f);
        points.Add(deck);reserved.Add(deck);
        var padRoot=new GameObject(name+" Launch Pad");padRoot.transform.SetParent(layout.transform);padRoot.transform.position=pad;
        Primitive("Pedestal",PrimitiveType.Cylinder,pad+Vector3.up*.12f,new Vector3(1.8f,.12f,1.8f),stone,padRoot.transform,false);
        Primitive(mushroom?"Mushroom Cap":"Launch Surface",mushroom?PrimitiveType.Sphere:PrimitiveType.Cylinder,pad+Vector3.up*.26f,new Vector3(1.9f,mushroom ? .28f : .05f,1.9f),accent,padRoot.transform,false);
        var walkSurface = padRoot.AddComponent<BoxCollider>();walkSurface.center=Vector3.up*.15f;walkSurface.size=new Vector3(1.8f,.3f,1.8f);
        var trigger=padRoot.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up*.6f;trigger.size=new Vector3(.9f,1.2f,.9f);
        var launch=padRoot.AddComponent<TraversalLaunchPad>();launch.landingPosition=deck+Vector3.right*1.35f;
        float apex=Mathf.Max(2,deck.y-pad.y+2.5f);float vy=Mathf.Sqrt(40*apex);
        float landingTime=(vy+Mathf.Sqrt(Mathf.Max(0,vy*vy-40*(deck.y-pad.y))))/20;
        var horizontal=launch.landingPosition-pad;horizontal.y=0;launch.launchVelocity=horizontal/Mathf.Min(1.1f,landingTime)+Vector3.up*vy;
        Sign(mushroom?"BOUNCE":"LAUNCH",pad+new Vector3(0,.6f,-1.6f),.09f);
    }
    private static void Deck(string name,Vector3 top,Vector2 size,bool pylon)
    {
        var go=new GameObject(name);go.transform.SetParent(layout.transform);
        Primitive("Deck",PrimitiveType.Cube,top-Vector3.up*.22f,new Vector3(size.x,.44f,size.y),stone,go.transform);
        Primitive("Inset",PrimitiveType.Cube,top-Vector3.up*.018f,new Vector3(size.x-.45f,.04f,size.y-.45f),dark,go.transform,false);
        foreach(int side in new[]{-1,1})
        {
            Primitive("Route Light",PrimitiveType.Cube,top+new Vector3(side*(size.x*.5f-.1f),.025f,0),new Vector3(.10f,.035f,size.y),accent,go.transform,false);
            Primitive("Route Light",PrimitiveType.Cube,top+new Vector3(0,.025f,side*(size.y*.5f-.1f)),new Vector3(size.x,.035f,.10f),accent,go.transform,false);
        }
        if(pylon && Physics.Raycast(top+Vector3.down*.6f,Vector3.down,out var ground,50,~0,QueryTriggerInteraction.Ignore))
        {
            float h=top.y-ground.point.y-.44f;
            if(h>0)Primitive("Support",PrimitiveType.Cube,new Vector3(top.x,ground.point.y+h*.5f,top.z),new Vector3(.6f,h,.6f),dark,go.transform);
        }
        Physics.SyncTransforms();
    }
    private static void BuildSpawnSign(MapDefinition map,Vector3 spawn)
    {
        string ability=map.Traversal==MapTraversal.Jetpack?"SPACE : FLY":map.Traversal==MapTraversal.DoubleJump?"SHIFT : SPRINT\nSPACE x2 : DOUBLE JUMP":"F : DASH";
        Sign(map.displayName+"\n"+ability,spawn+new Vector3(-3.5f,1.7f,5f),.035f);
    }
    private static void Sign(string text,Vector3 position,float size)
    {
        var go=new GameObject("Route Sign");go.transform.SetParent(layout.transform);go.transform.position=position;
        var mesh=go.AddComponent<TextMesh>();mesh.text=text;mesh.characterSize=size;mesh.fontSize=64;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.color=accent.color;
        go.transform.rotation=Quaternion.identity;
    }
    private static GameObject Primitive(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material material,Transform parent,bool solid=true)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent);go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    private static Material MaterialAsset(string name,Color color,bool glow=false)
    {
        string path=Generated+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.color=color;mat.SetFloat("_Smoothness",.2f);if(glow){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.7f);}EditorUtility.SetDirty(mat);return mat;
    }
    private static Transform Marker(string name,Vector3 position)
    {
        var t=new GameObject(name).transform;t.SetParent(layout.transform);t.position=position;return t;
    }
    private static bool GroundName(string name,bool pathOnly)
    {
        name=name.ToLowerInvariant();if(name.Contains("water")||name.Contains("stream")||name.Contains("tree")||name.Contains("cloud"))return false;
        return name.Contains("road")||name.Contains("path")||(!pathOnly&&(name.Contains("ground")||name.Contains("floor")||name.Contains("hill")||name.Contains("dirt")||name.Contains("terrain")));
    }
    public static Vector3 GroundNear(Vector3 seed,float radius,bool pathOnly,float clearTop = -1,float clearanceRadius = .42f)
    {
        Physics.SyncTransforms();
        for(float r=0;r<=radius;r+=.6f)for(int i=0;i<(r==0?1:32);i++)
        {
            float angle=i*Mathf.PI*2/32;var p=seed+new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r);
            var hits=Physics.RaycastAll(new Vector3(p.x,70,p.z),Vector3.down,100,~0,QueryTriggerInteraction.Ignore).OrderByDescending(h=>h.point.y);
            foreach(var hit in hits)
            {
                if(!GroundName(hit.collider.name,pathOnly))continue;
                // Never pick a lower overlapping terrain mesh underneath the visible road.
                if(hit.normal.y<.86f||hit.point.y>12)break;
                p=hit.point;
                if(reserved.Any(q=>Vector3.Distance(p,q)<3.2f))break;
                if(Physics.CheckCapsule(p+Vector3.up*(clearanceRadius+.13f),new Vector3(p.x,Mathf.Max(p.y+1.65f,clearTop),p.z),clearanceRadius,~0,QueryTriggerInteraction.Ignore))break;
                return p;
            }
        }
        throw new Exception($"No clear walkable ground near {seed} (pathOnly={pathOnly})");
    }
    private static void BakeNavigation(Scene scene,MapType id)
    {
        var surface=layout.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Volume;surface.center=layout.playableBounds.center;surface.size=layout.playableBounds.size;
        surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.overrideVoxelSize=true;surface.voxelSize=.15f;
        surface.BuildNavMesh();
        string path=Generated+"/"+id+" Navigation.asset";
        if(AssetDatabase.LoadAssetAtPath<NavMeshData>(path)!=null)AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(surface.navMeshData,path);
    }
    private static void ValidateWalkingTargets(List<Vector3> points,Vector3 spawn,MapType id)
    {
        if(!NavMesh.SamplePosition(spawn,out var origin,2,NavMesh.AllAreas))throw new Exception("Spawn has no baked walking surface: "+spawn);
        int count=id==MapType.PolygonStarter?4:9;
        for(int i=0;i<count;i++)
        {
            var path=new NavMeshPath();
            if(!NavMesh.SamplePosition(points[i],out var goal,1,NavMesh.AllAreas)||!NavMesh.CalculatePath(origin.position,goal.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
            {
                var mesh=NavMesh.CalculateTriangulation();
                bool replaced=false;
                foreach(var candidate in mesh.vertices.OrderBy(v=>Vector3.Distance(v,points[i])))
                {
                    if(Vector3.Distance(candidate,points[i])>10)break;
                    if(points.Where((p,index)=>index!=i).Any(p=>Vector3.Distance(p,candidate)<3.2f))continue;
                    if(!Physics.Raycast(candidate+Vector3.up*.5f,Vector3.down,out var ground,1,~0,QueryTriggerInteraction.Ignore)||!GroundName(ground.collider.name,false)||ground.normal.y<.86f)continue;
                    if(Physics.CheckCapsule(ground.point+Vector3.up*.55f,ground.point+Vector3.up*1.65f,.42f,~0,QueryTriggerInteraction.Ignore))continue;
                    if(!NavMesh.CalculatePath(origin.position,candidate,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                    points[i]=ground.point;replaced=true;break;
                }
                if(!replaced)throw new Exception($"Ground toy {i+1} is not connected to spawn: {points[i]}");
            }
        }
    }
    private static List<Vector3> SelectNpcPoints(Vector3 spawn)
    {
        var result=new List<Vector3>();var triangulation=NavMesh.CalculateTriangulation();
        for(int i=0;i+2<triangulation.indices.Length;i+=3)
        {
            var p=(triangulation.vertices[triangulation.indices[i]]+triangulation.vertices[triangulation.indices[i+1]]+triangulation.vertices[triangulation.indices[i+2]])/3;
            if(Vector3.Distance(p,spawn)<15||Vector3.Distance(p,spawn)>60||Mathf.Abs(p.y-spawn.y)>3||result.Any(q=>Vector3.Distance(q,p)<8))continue;
            var path=new NavMeshPath();if(!NavMesh.CalculatePath(spawn,p,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
            result.Add(p);if(result.Count==9)break;
        }
        return result;
    }
    private static void InstallGameplay(MapDefinition map,Vector3 spawn)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player/RobotPlayer.prefab");
        var player=(GameObject)PrefabUtility.InstantiatePrefab(prefab);player.name="RobotPlayer";player.tag="Player";player.transform.SetPositionAndRotation(layout.playerSpawn.position,layout.playerSpawn.rotation);
        var cc=player.GetComponent<CharacterController>();cc.height=1.8f;cc.radius=.35f;cc.center=Vector3.up*.9f;cc.stepOffset=.3f;cc.skinWidth=.035f;cc.minMoveDistance=0;
        var view=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(CinemachineBrain));view.tag="MainCamera";
        var brain=view.GetComponent<CinemachineBrain>();brain.UpdateMethod=CinemachineBrain.UpdateMethods.LateUpdate;brain.BlendUpdateMethod=CinemachineBrain.BrainUpdateMethods.LateUpdate;
        var camera=new GameObject("RobotThirdPersonCamera",typeof(CinemachineCamera),typeof(CinemachineOrbitalFollow),typeof(ThirdPersonCameraController));
        var cameraSO=new SerializedObject(camera.GetComponent<ThirdPersonCameraController>());cameraSO.FindProperty("target").objectReferenceValue=player.transform;cameraSO.FindProperty("targetOffset").vector3Value=Vector3.up*1.2f;cameraSO.FindProperty("defaultDistance").floatValue=5;cameraSO.ApplyModifiedPropertiesWithoutUndo();
        player.GetComponent<RobotMovementController>().SetCameraTransform(view.transform);
        var systems=new GameObject("Shared Gameplay System");var hunt=systems.AddComponent<ObjectHuntRoundManager>();systems.AddComponent<RoundGameLoop>();var spawner=systems.AddComponent<NpcSpawnManager>();
        var so=new SerializedObject(hunt);so.FindProperty("player").objectReferenceValue=player.transform;
        var catalog=so.FindProperty("targets");catalog.arraySize=12;
        string[] files={"Prop_Ball_01","Prop_Ball_02","Prop_Ball_03","Prop_Ball_04","Prop_TeddyBear_01","Prop_TeddyBear_02","Prop_TeddyBear_03","Prop_TeddyBear_04","Prop_ToyCar_Blue","Prop_ToyCar_Green","Prop_ToyCar_Yellow","Prop_ToyCar_Red"};
        for(int i=0;i<12;i++)
        {
            var entry=catalog.GetArrayElementAtIndex(i);entry.FindPropertyRelative("objectId").stringValue=files[i];entry.FindPropertyRelative("displayName").stringValue=files[i].Replace("Prop_","").Replace('_',' ');entry.FindPropertyRelative("category").enumValueIndex=i/4;
            entry.FindPropertyRelative("prefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Selected/toy/"+files[i]+".prefab");entry.FindPropertyRelative("worldScale").floatValue=i>=8?1.5f:1;entry.FindPropertyRelative("interactionRange").floatValue=2.4f;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        so=new SerializedObject(spawner);so.FindProperty("npcPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<NpcRobotController>("Assets/_Project/Prefabs/Characters/NPC/RobotNPC.prefab");so.FindProperty("player").objectReferenceValue=player.transform;var list=so.FindProperty("spawnPoints");list.arraySize=layout.npcSpawns.Length;for(int i=0;i<list.arraySize;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=layout.npcSpawns[i];so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void ValidateLocations()
    {
        Physics.SyncTransforms();if(layout.toySpawns.Length!=12)throw new Exception("Expected 12 toy points");
        foreach(var point in layout.toySpawns)
            if(!Physics.Raycast(point.position+Vector3.up*.15f,Vector3.down,out var hit,.4f,~0,QueryTriggerInteraction.Ignore)||hit.normal.y<.8f)throw new Exception("Unsupported target: "+point.name+" "+point.position);
    }
    private static void CaptureOverview(MapDefinition map)
    {
        var go=new GameObject("Overview");var camera=go.AddComponent<Camera>();var center=layout.playerSpawn.position;
        camera.transform.position=center+new Vector3(45,60,-65);camera.transform.LookAt(center+Vector3.forward*8);camera.farClipPlane=500;
        var rt=new RenderTexture(1200,800,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1200,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,800),0,0);image.Apply();File.WriteAllBytes("/tmp/roboseek-built-"+map.displayName+".png",image.EncodeToPNG());
        RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);
    }
}
#endif
