using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Robot.Player.Movement;
using Robot.Player.CameraControl;
using Robot.Player;
using Robot.Input;
using Robot.Robots.Customization;
using Robot.UI.HUD;
using Unity.Cinemachine;
using Robot.Combat;

namespace Robot.Editor
{
    public static class SetupRobotPlayer
    {
        [MenuItem("Tools/Robot/Setup Active Scene Player & Camera", false, 1)]
        public static void SetupPlayerInActiveScene()
        {
            // 1. Find or instantiate Player prefab
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("RobotPlayer");
            }

            if (player == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player/RobotPlayer.prefab");
                if (prefab != null)
                {
                    player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    Vector3 spawnPos = new Vector3(0f, 0.1f, 0f);
                    Vector3 rayOrigin = new Vector3(0f, 100f, 0f);
                    if (SceneView.lastActiveSceneView != null)
                    {
                        rayOrigin = SceneView.lastActiveSceneView.pivot + Vector3.up * 10f;
                    }
                    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 500f))
                    {
                        spawnPos = hit.point + Vector3.up * 0.05f;
                    }
                    player.transform.position = spawnPos;
                    Undo.RegisterCreatedObjectUndo(player, "Instantiate Robot Player");
                    Debug.Log($"[RobotSetup] Instantiated RobotPlayer prefab at {spawnPos} in active scene.");
                }
            }

            if (player != null)
            {
                player.tag = "Player";

                // Setup CharacterController
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc == null)
                {
                    cc = player.AddComponent<CharacterController>();
                }
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.radius = 0.35f;
                cc.height = 1.8f;

                // Input is intentionally the only component that knows Input System actions.
                PlayerInputReader input = player.GetComponent<PlayerInputReader>();
                if (input == null) input = player.AddComponent<PlayerInputReader>();
                input.Configure(AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                    "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));

                // Setup RobotMovementController
                RobotMovementController movement = player.GetComponent<RobotMovementController>();
                if (movement == null)
                {
                    movement = player.AddComponent<RobotMovementController>();
                }

                if (player.GetComponent<RobotAnimator>() == null)
                {
                    player.AddComponent<RobotAnimator>();
                }

                CombatHealth health = player.GetComponent<CombatHealth>();
                if (health == null) health = player.AddComponent<CombatHealth>();
                health.SetTeam(CombatTeam.Player);
                if (player.GetComponent<CombatAttack>() == null) player.AddComponent<CombatAttack>();
                if (player.GetComponent<PlayerCombatInput>() == null) player.AddComponent<PlayerCombatInput>();

                foreach (VirtualJoystick joystick in Object.FindObjectsByType<VirtualJoystick>(FindObjectsSortMode.None))
                {
                    joystick.Configure(input);
                    EditorUtility.SetDirty(joystick);
                }
                foreach (MobileTouchLook lookArea in Object.FindObjectsByType<MobileTouchLook>(FindObjectsSortMode.None))
                {
                    lookArea.Configure(input);
                    EditorUtility.SetDirty(lookArea);
                }

                // Setup RobotColorCustomizer
                RobotColorCustomizer customizer = player.GetComponent<RobotColorCustomizer>();
                if (customizer == null)
                {
                    customizer = player.AddComponent<RobotColorCustomizer>();
                }

                // Setup CameraTarget child
                Transform cameraTarget = player.transform.Find("CameraTarget");
                if (cameraTarget == null)
                {
                    GameObject ctGo = new GameObject("CameraTarget");
                    ctGo.transform.SetParent(player.transform, false);
                    ctGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                    cameraTarget = ctGo.transform;
                }

                // Setup Showcase UI
                RobotShowcaseUI showcaseUI = player.GetComponent<RobotShowcaseUI>();
                if (showcaseUI == null)
                {
                    showcaseUI = player.AddComponent<RobotShowcaseUI>();
                }

                Debug.Log($"[RobotSetup] Successfully configured player components on '{player.name}'.");
            }
            else
            {
                Debug.LogWarning("[RobotSetup] Could not find or instantiate 'RobotPlayer' in active scene.");
            }

            // 2. Setup Cinemachine camera and main camera brain
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                if (mainCam.GetComponent<CinemachineBrain>() == null)
                {
                    mainCam.gameObject.AddComponent<CinemachineBrain>();
                }

                GameObject virtualCameraObject = GameObject.Find("RobotThirdPersonCamera");
                if (virtualCameraObject == null)
                {
                    virtualCameraObject = new GameObject("RobotThirdPersonCamera");
                }
                if (virtualCameraObject.GetComponent<CinemachineCamera>() == null)
                {
                    virtualCameraObject.AddComponent<CinemachineCamera>();
                }

                CinemachineOrbitalFollow orbitalFollow = virtualCameraObject.GetComponent<CinemachineOrbitalFollow>();
                if (orbitalFollow == null)
                {
                    orbitalFollow = virtualCameraObject.AddComponent<CinemachineOrbitalFollow>();
                }
                orbitalFollow.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;

                CinemachineHardLookAt hardLookAt = virtualCameraObject.GetComponent<CinemachineHardLookAt>();
                if (hardLookAt == null)
                {
                    hardLookAt = virtualCameraObject.AddComponent<CinemachineHardLookAt>();
                }
                hardLookAt.LookAtOffset = new Vector3(0f, 1.2f, 0f);
                
                CinemachineDeoccluder deoccluder = virtualCameraObject.GetComponent<CinemachineDeoccluder>();
                if (deoccluder == null)
                {
                    deoccluder = virtualCameraObject.AddComponent<CinemachineDeoccluder>();
                }
                deoccluder.IgnoreTag = "Player";
                deoccluder.MinimumDistanceFromTarget = 0.8f;

                ThirdPersonCameraController tpc = virtualCameraObject.GetComponent<ThirdPersonCameraController>();
                if (tpc == null)
                {
                    tpc = virtualCameraObject.AddComponent<ThirdPersonCameraController>();
                }

                if (player != null)
                {
                    tpc.SetTarget(player.transform);
                    RobotMovementController rmc = player.GetComponent<RobotMovementController>();
                    if (rmc != null)
                    {
                        rmc.SetCameraTransform(mainCam.transform);
                    }
                }

                Debug.Log($"[RobotSetup] Successfully attached ThirdPersonCamera to '{mainCam.name}'.");
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
