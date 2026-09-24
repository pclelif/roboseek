using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Robot.Combat;
using Robot.Input;
using Robot.NPC;
using Robot.ObjectHunt;
using Robot.Player;
using Robot.Player.CameraControl;
using Robot.Player.Movement;
using Robot.Robots.Customization;
using Robot.UI.HUD;
using Robot.UI.Production;

namespace Robot.Core
{
    /// <summary>
    /// Autonomous runtime bootstrapper for RoboSeek gameplay scenes.
    /// Ensures that any loaded map scene (City, Adventure, Polygon, PolygonStarter)
    /// automatically initializes the shared Player, Camera, Object Hunt, UI, and NPC systems
    /// without requiring any manual setup in the scene file.
    /// </summary>
    public sealed class MapSceneBootstrapper : MonoBehaviour
    {
        private static bool isInitializing = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoadedRuntime()
        {
            EnsureSceneBootstrapped();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSceneBootstrapped();
        }

        public static void EnsureSceneBootstrapped()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid()) return;
            string sceneName = activeScene.name;

            // Only bootstrap gameplay map scenes (do not bootstrap standalone lobby scene)
            if (sceneName == "RoboSeek_Lobby" || sceneName == "UI_System_Demo") return;
            if (isInitializing) return;
            MapManager.SelectMapForScene(sceneName);

            isInitializing = true;
            try
            {
                var activeMap = MapManager.SelectedMap;
                Debug.Log($"[MapSceneBootstrapper] Initializing shared gameplay system for Map: {activeMap.displayName} in Scene: '{sceneName}'");

                // 0. Select world theme for map colors
                Robot.UI.WorldThemeManager.SelectWorldByScene(sceneName);

                // 1. Ensure Player & Camera
                GameObject player = EnsurePlayer(activeMap);
                EnsureCamera(player);

                // 2. Ensure Shared Gameplay System (ObjectHuntRoundManager, RoundGameLoop, ObjectHuntHUD)
                GameObject gameplayRoot = EnsureGameplaySystem(player);

                // 3. Ensure NPC System (NpcSpawnManager & 9 NPCs)
                EnsureNpcSystem(gameplayRoot, player);

                // 4. Ensure Environment Lighting & Sun Light
                EnsureLighting();

                // 5. Ensure Jetpack & Sky Rings for Arena (PolygonStarter) map
                EnsureJetpackAndSkyRings(activeMap, sceneName, player);

                // 6. Ensure Audio Manager
                if (Robot.Audio.AudioManager.Instance == null)
                {
                    var audioGO = new GameObject("AudioManager");
                    audioGO.AddComponent<Robot.Audio.AudioManager>();
                }
                // Gameplay is intentionally music-free; result events start their
                // own win/loss tracks after the round ends.
                Robot.Audio.AudioManager.Instance?.StopMusic();

                // 7. Ensure Production Canvas UI Root & HUD
                EnsureProductionUI(player, gameplayRoot);

                // 8. Suppress any standalone legacy canvases in the scene so only active gameplay HUD displays
                CleanupLegacyCanvases();
            }
            finally
            {
                isInitializing = false;
            }
        }

        private static void EnsureLighting()
        {
            Light mainLight = null;
            foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light != null && light.type == LightType.Directional)
                {
                    mainLight = light;
                    light.enabled = true;
                    if (light.intensity < 1.0f) light.intensity = 1.3f;
                    light.shadows = LightShadows.Soft;
                    break;
                }
            }

            if (mainLight == null)
            {
                GameObject lightObj = new GameObject("Sun Light", typeof(Light));
                mainLight = lightObj.GetComponent<Light>();
                mainLight.type = LightType.Directional;
                mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                mainLight.intensity = 1.3f;
                mainLight.color = new Color(1f, 0.96f, 0.88f);
                mainLight.shadows = LightShadows.Soft;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.75f, 0.75f, 0.78f);
            RenderSettings.ambientIntensity = 1.2f;
        }

        private static void CleanupLegacyCanvases()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas == null) continue;
                string cName = canvas.name;
                // Canvas_UI_Root is the production gameplay HUD created above.
                // It must survive the raw-demo canvas cleanup pass.
                if (cName == "Canvas_UI_Root" || cName == "GameplayHUDCanvas" || cName == "RobotShowcaseUICanvas" ||
                    canvas.GetComponent<UIRootController>() != null) continue;

                // Disable any canvas from Synty demo scenes (e.g. MainMenuCanvas, DemoCanvas, LegacyHUD, etc.)
                if (canvas.GetComponent<Robot.ObjectHunt.ObjectHuntHUD>() == null &&
                    canvas.GetComponent<Robot.UI.HUD.RobotHuntHUD>() == null &&
                    canvas.GetComponent<Robot.UI.HUD.PauseMenuUI>() == null)
                {
                    canvas.gameObject.SetActive(false);
                }
            }
        }

        private static GameObject EnsurePlayer(MapDefinition map)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("RobotPlayer");

            if (player == null)
            {
                // Try loading prefab or create instance
                GameObject prefab = Resources.Load<GameObject>("Characters/Player/RobotPlayer");
#if UNITY_EDITOR
                if (prefab == null)
                {
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player/RobotPlayer.prefab");
                }
#endif
                if (prefab != null)
                {
                    player = Instantiate(prefab);
                    player.name = "RobotPlayer";
                }
                else
                {
                    player = new GameObject("RobotPlayer");
                }
            }

            var layout = FindFirstObjectByType<MapLevelLayout>();
            Vector3 spawnPos = layout != null && layout.playerSpawn != null ? layout.playerSpawn.position :
                map != null ? map.playerSpawnPosition : new Vector3(0f, .1f, 0f);
            if (Physics.Raycast(spawnPos + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5.0f, ~0, QueryTriggerInteraction.Ignore))
            {
                spawnPos = hit.point + Vector3.up * 0.02f;
            }

            player.tag = "Player";

            // Ensure CharacterController position placement
            var cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = spawnPos;
            player.transform.rotation = layout != null && layout.playerSpawn != null ? layout.playerSpawn.rotation :
                Quaternion.Euler(map != null ? map.playerSpawnRotation : Vector3.zero);
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.35f;
            cc.height = 1.8f;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.035f;
            cc.minMoveDistance = 0f;
            cc.enabled = true;

            // Ensure PlayerInputReader
            var input = player.GetComponent<PlayerInputReader>();
            if (input == null) input = player.AddComponent<PlayerInputReader>();

            // Ensure RobotMovementController
            var movement = player.GetComponent<RobotMovementController>();
            if (movement == null) movement = player.AddComponent<RobotMovementController>();
            movement.SetSpawnPoint(spawnPos, player.transform.rotation);
            movement.ConfigureTraversal(map != null ? map.Traversal : MapTraversal.Standard);
            if (layout != null && player.GetComponent<MapTraversalSafety>() == null) player.AddComponent<MapTraversalSafety>();
            movement.enabled = true;
            movement.SetControlEnabled(true);

            // Ensure RobotAnimator
            if (player.GetComponent<RobotAnimator>() == null) player.AddComponent<RobotAnimator>();

            // Ensure CombatHealth & Attack
            var health = player.GetComponent<CombatHealth>();
            if (health == null) health = player.AddComponent<CombatHealth>();
            health.SetTeam(CombatTeam.Player);

            if (player.GetComponent<CombatAttack>() == null) player.AddComponent<CombatAttack>();
            if (player.GetComponent<PlayerCombatInput>() == null) player.AddComponent<PlayerCombatInput>();

            // Ensure RobotColorCustomizer
            if (player.GetComponent<RobotColorCustomizer>() == null) player.AddComponent<RobotColorCustomizer>();

            // Ensure Showcase UI is disabled in gameplay map scenes
            var showcaseUI = player.GetComponent<RobotShowcaseUI>();
            if (showcaseUI != null) showcaseUI.enabled = false;

            return player;
        }

        private static void EnsureCamera(GameObject player)
        {
            // Disable or destroy secondary static cameras from raw demo scenes
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Camera mainCam = null;

            foreach (var c in allCameras)
            {
                if (c == null) continue;
                if (c.CompareTag("MainCamera") || c.name == "Main Camera")
                {
                    mainCam = c;
                    break;
                }
            }

            if (mainCam == null && allCameras.Length > 0)
            {
                mainCam = allCameras[0];
            }

            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                mainCam = camObj.GetComponent<Camera>();
            }

            mainCam.gameObject.SetActive(true);
            mainCam.enabled = true;
            mainCam.tag = "MainCamera";
            mainCam.name = "Main Camera";
            mainCam.transform.SetParent(null); // Ensure camera is root, not moving with any parent

            // Untag and disable all OTHER cameras in the scene
            foreach (var c in allCameras)
            {
                if (c != null && c != mainCam)
                {
                    c.tag = "Untagged";
                    c.enabled = false;
                    c.gameObject.SetActive(false);
                }
            }

            // Remove any legacy demo flycam or orbit scripts on mainCam
            foreach (var mono in mainCam.GetComponents<MonoBehaviour>())
            {
                if (mono == null) continue;
                string typeName = mono.GetType().Name;
                if (typeName.Contains("Demo") || typeName.Contains("Orbit") || typeName.Contains("Fly") || typeName.Contains("Switcher"))
                {
                    Destroy(mono);
                }
            }

            // Ensure CinemachineBrain on mainCam
            var brain = mainCam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCam.gameObject.AddComponent<Unity.Cinemachine.CinemachineBrain>();
            }
            brain.enabled = true;
            brain.UpdateMethod = Unity.Cinemachine.CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = Unity.Cinemachine.CinemachineBrain.BrainUpdateMethods.LateUpdate;

            // Disable all other Cinemachine Cameras in demo scenes
            var allVCams = FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var vcam in allVCams)
            {
                if (vcam != null && vcam.name != "RobotThirdPersonCamera")
                {
                    vcam.gameObject.SetActive(false);
                }
            }

            // Ensure RobotThirdPersonCamera object
            GameObject virtualCameraObject = GameObject.Find("RobotThirdPersonCamera");
            if (virtualCameraObject == null)
            {
                virtualCameraObject = new GameObject("RobotThirdPersonCamera");
            }
            virtualCameraObject.SetActive(true);
            virtualCameraObject.transform.SetParent(null); // Ensure virtual camera is root

            var cinemachineCam = virtualCameraObject.GetComponent<Unity.Cinemachine.CinemachineCamera>();
            if (cinemachineCam == null)
            {
                cinemachineCam = virtualCameraObject.AddComponent<Unity.Cinemachine.CinemachineCamera>();
            }
            cinemachineCam.Priority.Value = 1000;
            cinemachineCam.enabled = true;

            var tpc = virtualCameraObject.GetComponent<ThirdPersonCameraController>();
            if (tpc == null) tpc = virtualCameraObject.AddComponent<ThirdPersonCameraController>();

            if (player != null)
            {
                tpc.SetTarget(player.transform);
                tpc.SnapToRoundStart(player.transform);

                Vector3 initialCamPos = player.transform.position + new Vector3(0f, 2.5f, -4.5f);
                Quaternion initialCamRot = Quaternion.LookRotation((player.transform.position + Vector3.up * 1.2f) - initialCamPos);
                mainCam.transform.SetPositionAndRotation(initialCamPos, initialCamRot);

                var rmc = player.GetComponent<RobotMovementController>();
                if (rmc != null)
                {
                    rmc.SetCameraTransform(mainCam.transform);
                    rmc.SetCameraController(tpc);
                }
            }
        }

        private static GameObject EnsureGameplaySystem(GameObject player)
        {
            GameObject root = GameObject.Find("Shared Gameplay System");
            if (root == null) root = GameObject.Find("Phase 4 NPC System");
            if (root == null) root = new GameObject("Shared Gameplay System");

            var huntManager = root.GetComponent<ObjectHuntRoundManager>();
            if (huntManager == null) huntManager = root.AddComponent<ObjectHuntRoundManager>();

            var loop = root.GetComponent<RoundGameLoop>();
            if (loop == null) loop = root.AddComponent<RoundGameLoop>();

            var hud = root.GetComponent<ObjectHuntHUD>();
            // Production UI owns the HUD and round navigation on every map.
            if (hud != null) hud.enabled = false;

            return root;
        }

        private static void EnsureNpcSystem(GameObject root, GameObject player)
        {
            var spawner = root.GetComponent<NpcSpawnManager>();
            var layout = FindFirstObjectByType<MapLevelLayout>();
            if (layout != null && layout.npcSpawns.Length > 0)
            {
                if (spawner == null) spawner = root.AddComponent<NpcSpawnManager>();
                spawner.ConfigureForRuntime(layout.npcSpawns, player.transform);
                return;
            }
            if (spawner != null && spawner.HasSpawnPoints) return;

            if (spawner == null) spawner = root.AddComponent<NpcSpawnManager>();

            // Generate 9 balanced spawn positions around map
            Vector3 origin = player != null ? player.transform.position : Vector3.zero;
            List<Vector3> spawnPositions = GenerateNpcSpawnPositions(origin, 9);

            Transform spawnRoot = root.transform.Find("Generated Spawn Points");
            if (spawnRoot == null)
            {
                spawnRoot = new GameObject("Generated Spawn Points").transform;
                spawnRoot.SetParent(root.transform, false);
            }

            Transform[] spawnPoints = new Transform[spawnPositions.Count];
            for (int i = 0; i < spawnPositions.Count; i++)
            {
                Transform pt = spawnRoot.Find($"NPC Spawn {i + 1:00}");
                if (pt == null)
                {
                    GameObject ptObj = new GameObject($"NPC Spawn {i + 1:00}");
                    ptObj.transform.SetParent(spawnRoot, false);
                    pt = ptObj.transform;
                }
                pt.position = spawnPositions[i];
                pt.rotation = Quaternion.Euler(0f, (i * 137.5f) % 360f, 0f);
                spawnPoints[i] = pt;
            }

            spawner.ConfigureForRuntime(spawnPoints, player != null ? player.transform : null);
        }

        private static List<Vector3> GenerateNpcSpawnPositions(Vector3 playerOrigin, int count)
        {
            var result = new List<Vector3>();

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices != null && triangulation.vertices.Length > 0)
            {
                var candidates = new List<Vector3>();
                for (int i = 0; i + 2 < triangulation.indices.Length; i += 3)
                {
                    Vector3 center = (triangulation.vertices[triangulation.indices[i]] +
                                      triangulation.vertices[triangulation.indices[i + 1]] +
                                      triangulation.vertices[triangulation.indices[i + 2]]) / 3f;

                    if (!NavMesh.SamplePosition(center, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;
                    Vector3 pt = hit.position;

                    if (Mathf.Abs(pt.y - playerOrigin.y) > 4f) continue;
                    if (Vector3.Distance(new Vector3(pt.x, 0, pt.z), new Vector3(playerOrigin.x, 0, playerOrigin.z)) < 20f) continue;

                    if (candidates.TrueForAll(c => Vector3.Distance(new Vector3(c.x, 0, c.z), new Vector3(pt.x, 0, pt.z)) >= 18f))
                    {
                        candidates.Add(pt);
                    }
                }

                foreach (var c in candidates)
                {
                    if (result.Count >= count) break;
                    result.Add(c);
                }
            }

            // Fallback points around origin if NavMesh had fewer points
            Vector3[] ringFallbacks = {
                new Vector3(-25f, 0.1f, -25f),
                new Vector3(25f, 0.1f, -25f),
                new Vector3(-25f, 0.1f, 25f),
                new Vector3(25f, 0.1f, 25f),
                new Vector3(0f, 0.1f, 35f),
                new Vector3(0f, 0.1f, -35f),
                new Vector3(35f, 0.1f, 0f),
                new Vector3(-35f, 0.1f, 0f),
                new Vector3(18f, 0.1f, 18f)
            };

            int index = 0;
            while (result.Count < count && index < ringFallbacks.Length)
            {
                Vector3 pt = playerOrigin + ringFallbacks[index++];
                if (Physics.Raycast(pt + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore))
                {
                    pt = hit.point + Vector3.up * 0.05f;
                }
                result.Add(pt);
            }

            return result;
        }

        private static void EnsureJetpackAndSkyRings(MapDefinition map, string sceneName, GameObject player)
        {
            // Traversal is explicitly configured on the player and authored pads/rings.
            // Never turn arbitrary meshes whose names contain "target" into triggers.
            if (player != null)
                player.GetComponent<RobotMovementController>()?.ConfigureTraversal(map.Traversal);
        }

        private static void EnsureProductionUI(GameObject player, GameObject gameplayRoot)
        {
            var hunt = gameplayRoot.GetComponent<ObjectHuntRoundManager>();
            var loop = gameplayRoot.GetComponent<RoundGameLoop>();
            var movement = player != null ? player.GetComponent<RobotMovementController>() : FindFirstObjectByType<RobotMovementController>();
            var camera = FindFirstObjectByType<ThirdPersonCameraController>();
            var input = movement != null ? movement.GetComponent<PlayerInputReader>() : FindFirstObjectByType<PlayerInputReader>();

            var existingRoot = FindFirstObjectByType<UIRootController>();
            if (existingRoot != null)
            {
                // City already contains the complete authored production UI: round
                // reveal/countdown, pause, result, target and vehicle screens. It
                // was hidden by the legacy-canvas cleanup pass, not obsolete.
                existingRoot.gameObject.SetActive(true);

                if (existingRoot.state != null)
                {
                    existingRoot.state.loop = loop;
                    loop?.UseRealtimePresentation();

                    var existingGate = existingRoot.state.inputGate;
                    if (existingGate != null)
                    {
                        existingGate.movement = movement;
                        existingGate.combatInput = movement != null ? movement.GetComponent<PlayerCombatInput>() : null;
                        existingGate.input = input;
                        existingGate.hunt = hunt;
                    }

                    var interaction = existingRoot.GetComponent<InteractionStateAdapter>();
                    if (interaction != null)
                    {
                        interaction.hunt = hunt;
                        interaction.state = existingRoot.state;
                    }
                }

                EnsureCityMechanicsUI(existingRoot.gameObject);
                return;
            }

            Debug.Log("[MapSceneBootstrapper] Creating runtime Canvas_UI_Root and Production UI elements...");

            var rootGO = new GameObject("Canvas_UI_Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            // Components such as TargetHUD subscribe from OnEnable. Build this root
            // inactive so every dependency is assigned before any subscription runs.
            rootGO.SetActive(false);
            var canvas = rootGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = rootGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var root = rootGO.AddComponent<UIRootController>();
            root.localization = rootGO.AddComponent<UILocalization>();
            var state = rootGO.AddComponent<UIStateManager>();
            root.state = state;
            state.loop = loop;
            loop?.UseRealtimePresentation();

            var gate = rootGO.AddComponent<GameplayInputGate>();
            gate.movement = movement;
            gate.combatInput = movement != null ? movement.GetComponent<PlayerCombatInput>() : null;
            gate.input = input;
            gate.hunt = hunt;
            state.inputGate = gate;

            var cursor = rootGO.AddComponent<CursorStateController>();
            cursor.gameplayCamera = camera;
            state.cursor = cursor;

            var visuals = rootGO.AddComponent<TargetVisualLibrary>();
            var source = rootGO.AddComponent<InteractionStateAdapter>();
            source.hunt = hunt;
            source.state = state;

            var screens = new GameObject[10];
            state.screens = screens;

            // Gameplay HUD Screen
            var gameplayScreen = UIView.Stretch("GameplayHUD", rootGO.transform).gameObject;
            screens[(int)UIScreen.Gameplay] = gameplayScreen;
            state.gameplayHUD = gameplayScreen.AddComponent<CanvasGroup>();

            // Target HUD Panel
            var targetPanel = UIView.Panel("TargetPanel", gameplayScreen.transform, new Vector2(224, 96), new Color(.08f, .12f, .14f, .65f));
            targetPanel.rectTransform.anchorMin = targetPanel.rectTransform.anchorMax = targetPanel.rectTransform.pivot = Vector2.one;
            targetPanel.rectTransform.anchoredPosition = new Vector2(-40, -40);
            targetPanel.raycastTarget = false;
            UIView.Label("Heading", targetPanel.transform, "TARGETS", 13, new Vector2(200, 20), new Vector2(0, 34), Color.white);

            var hud = rootGO.AddComponent<TargetHUD>();
            hud.hunt = hunt;
            hud.visuals = visuals;
            hud.icons = new RawImage[3];
            hud.names = new Text[3];
            hud.states = new Text[3];
            hud.checks = new RectTransform[3];

            for (int i = 0; i < 3; i++)
            {
                var slot = UIView.Rect("TargetSlot" + (i + 1), targetPanel.transform, new Vector2(68, 68), new Vector2((i - 1) * 72, -9));
                var icon = UIView.Rect("Visual", slot, new Vector2(45, 43), new Vector2(0, 15)).gameObject.AddComponent<RawImage>();
                icon.raycastTarget = false;
                hud.icons[i] = icon;
                hud.names[i] = UIView.Label("AccessibleName", slot, "", 9, new Vector2(68, 22), new Vector2(0, -11), Color.white);
                hud.states[i] = UIView.Label("FoundState", slot, "0", 15, new Vector2(40, 20), new Vector2(0, -27), Color.white);

                var check = UIView.Rect("CompletionCheckmark", slot, new Vector2(18, 18), new Vector2(0, -27));
                var a = UIView.Panel("ShortStroke", check, new Vector2(7, 3), UIView.Accent, new Vector2(-4, -1));
                a.transform.localRotation = Quaternion.Euler(0, 0, -45); a.raycastTarget = false;
                var b = UIView.Panel("LongStroke", check, new Vector2(13, 3), UIView.Accent, new Vector2(2, 1));
                b.transform.localRotation = Quaternion.Euler(0, 0, 45); b.raycastTarget = false;
                hud.checks[i] = check;
                check.gameObject.SetActive(false);
            }

            // Interaction Prompt UI ([E] SCAN & RETRIEVE)
            var promptRect = UIView.Rect("InteractionPrompt", gameplayScreen.transform, new Vector2(290, 42));
            promptRect.anchorMin = promptRect.anchorMax = new Vector2(.5f, 0);
            promptRect.anchoredPosition = new Vector2(0, 145);
            var prompt = rootGO.AddComponent<InteractionPromptUI>();
            prompt.group = promptRect.gameObject.AddComponent<CanvasGroup>();
            prompt.group.alpha = 0; prompt.group.blocksRaycasts = false; prompt.group.interactable = false;
            prompt.source = source;
            state.prompt = prompt;
            UIView.Keycap(promptRect, "E", new Vector2(-118, 0), new Vector2(36, 38));
            UIView.Label("Action", promptRect, "SCAN & RETRIEVE", 19, new Vector2(238, 40), new Vector2(23, 0), Color.white).gameObject.AddComponent<Shadow>();

            // Feedback Message UI
            var feedbackRect = UIView.Rect("FeedbackMessage", gameplayScreen.transform, new Vector2(440, 65));
            feedbackRect.anchorMin = feedbackRect.anchorMax = new Vector2(.5f, 1);
            feedbackRect.anchoredPosition = new Vector2(0, -150);
            var feedback = rootGO.AddComponent<FeedbackMessageUI>();
            feedback.group = feedbackRect.gameObject.AddComponent<CanvasGroup>();
            feedback.group.alpha = 0; feedback.group.blocksRaycasts = false; feedback.group.interactable = false;
            UIView.Label("Heading", feedbackRect, "TARGET RETRIEVED", 16, new Vector2(440, 28), new Vector2(0, 16), UIView.AccentYellow).gameObject.AddComponent<Shadow>();
            feedback.targetName = UIView.Label("TargetName", feedbackRect, "", 21, new Vector2(440, 32), new Vector2(0, -15), Color.white);
            feedback.targetName.gameObject.AddComponent<Shadow>();
            hud.feedback = feedback;

            // Tutorial Hint UI
            var hintRect = UIView.Rect("TutorialHint", gameplayScreen.transform, new Vector2(300, 40));
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(.5f, 0);
            hintRect.anchoredPosition = new Vector2(0, 100);
            var tutorial = rootGO.AddComponent<TutorialHintUI>();
            state.tutorial = tutorial;
            tutorial.group = hintRect.gameObject.AddComponent<CanvasGroup>();
            tutorial.group.blocksRaycasts = false; tutorial.group.interactable = false; tutorial.group.alpha = 0;
            tutorial.key = UIView.Keycap(hintRect, "W A S D", new Vector2(-100, 0), new Vector2(100, 38));
            tutorial.message = UIView.Label("Hint", hintRect, "MOVE", 18, new Vector2(195, 40), new Vector2(53, 0), Color.white);
            tutorial.message.gameObject.AddComponent<Shadow>();
            tutorial.input = input;
            tutorial.movement = movement;
            tutorial.cameraController = camera;
            if (movement != null) tutorial.attack = movement.GetComponent<CombatAttack>();
            tutorial.hunt = hunt;
            tutorial.interaction = source;

            EnsureCityMechanicsUI(rootGO);

            // All UI references are now ready; it is safe for OnEnable/Start hooks
            // to subscribe to the round and input systems.
            rootGO.SetActive(true);

            // Ensure EventSystem
            var es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                go.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
            }

            // Trigger game loop start and enable HUD view
            if (loop != null)
            {
                loop.StartGame();
            }
            state.Open(UIScreen.Gameplay);
            if (gate != null) gate.SetBlocked(false);
            UIView.Visible(state.gameplayHUD, true);
        }

        // City-specific abilities were originally serialized on the City scene's HUD.
        // Attach them at runtime so the prepared City map and every fresh HUD receive
        // the same interaction set without changing the camera or lobby scenes.
        private static void EnsureCityMechanicsUI(GameObject rootGO)
        {
            if (rootGO == null || !MapManager.SelectedMap.enableCityMechanics) return;

            if (rootGO.GetComponent<PlayerVitalsUI>() == null) rootGO.AddComponent<PlayerVitalsUI>();
            if (rootGO.GetComponent<PoliceRadarUI>() == null) rootGO.AddComponent<PoliceRadarUI>();
            if (rootGO.GetComponent<TaxiSpeedUI>() == null) rootGO.AddComponent<TaxiSpeedUI>();
            if (rootGO.GetComponent<VehicleCountdownHUD>() == null) rootGO.AddComponent<VehicleCountdownHUD>();
        }
    }
}
