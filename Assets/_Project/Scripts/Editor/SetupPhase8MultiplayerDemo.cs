#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Robot.Multiplayer;
using Robot.Robots.Customization;
using Robot.Spawning;

namespace Robot.Editor
{
    public static class SetupPhase8MultiplayerDemo
    {
        private const string PalettePath = "Assets/_Project/Data/RobotColorPalette.asset";
        private const string NetworkPrefabPath = "Assets/_Project/Prefabs/Characters/Player/NetworkRobotPlayer.prefab";
        private const string SourceDemoScenePath = "Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity";
        private const string TargetFolder = "Assets/_Project/Scenes/Multiplayer";
        private const string TargetDemo2ScenePath = TargetFolder + "/DEMO-2.unity";

        [MenuItem("Tools/Robot Hunt/Phase 8/Build DEMO-2 Multiplayer Scene", false, 2)]
        public static void BuildDemo2Scene()
        {
            EnsureFolder(TargetFolder);

            if (!File.Exists(SourceDemoScenePath))
            {
                throw new InvalidOperationException($"Source scene not found at: {SourceDemoScenePath}");
            }

            bool copied = AssetDatabase.CopyAsset(SourceDemoScenePath, TargetDemo2ScenePath);
            if (!copied)
            {
                File.Copy(SourceDemoScenePath, TargetDemo2ScenePath, true);
                AssetDatabase.Refresh();
            }

            Scene scene = EditorSceneManager.OpenScene(TargetDemo2ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open scene: {TargetDemo2ScenePath}");
            }

            ConfigureMultiplayerInScene(scene);

            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            var sceneList = new List<EditorBuildSettingsScene>(existingScenes);
            if (!sceneList.Exists(s => s.path == TargetDemo2ScenePath))
            {
                sceneList.Add(new EditorBuildSettingsScene(TargetDemo2ScenePath, true));
                EditorBuildSettings.scenes = sceneList.ToArray();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Phase 8 DEMO-2] Successfully built multiplayer DEMO-2 scene at '{TargetDemo2ScenePath}'!");
        }

        [MenuItem("Tools/Robot Hunt/Phase 8/Setup Multiplayer in Active Scene", false, 3)]
        public static void SetupMultiplayerInActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                throw new InvalidOperationException("No valid active scene found.");
            }

            ConfigureMultiplayerInScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Phase 8 Multiplayer] Successfully configured multiplayer in active scene '{scene.name}'!");
        }

        private static void ConfigureMultiplayerInScene(Scene scene)
        {
            // 1. Remove all static singleplayer player objects to avoid double spawn
            var singlePlayers = UnityEngine.Object.FindObjectsByType<Robot.Player.Movement.RobotMovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sp in singlePlayers)
            {
                if (sp != null && sp.GetComponent<NetworkObject>() == null)
                {
                    UnityEngine.Object.DestroyImmediate(sp.gameObject);
                    Debug.Log("[Phase 8 Multiplayer] Removed static single-player robot object: " + sp.gameObject.name);
                }
            }

            // Remove any singleplayer ObjectHunt HUD in multiplayer mode if present
            var objectHuntHUDs = UnityEngine.Object.FindObjectsByType<Robot.ObjectHunt.ObjectHuntHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var hud in objectHuntHUDs)
            {
                if (hud != null)
                {
                    UnityEngine.Object.DestroyImmediate(hud);
                }
            }

            // 2. Configure Palette & RobotColorService
            RobotColorPalette palette = AssetDatabase.LoadAssetAtPath<RobotColorPalette>(PalettePath);
            if (palette == null)
            {
                Debug.LogWarning("[Phase 8 Multiplayer] Color palette not found, running Phase 8 foundation setup first.");
                SetupPhase8NetworkFoundation.Build();
                palette = AssetDatabase.LoadAssetAtPath<RobotColorPalette>(PalettePath);
            }

            GameObject services = GameObject.Find("PlayerServices");
            if (services == null) services = new GameObject("PlayerServices");
            RobotColorService colorService = services.GetComponent<RobotColorService>();
            if (colorService == null) colorService = services.AddComponent<RobotColorService>();
            colorService.Configure(palette);

            // 3. Configure Spawns and snap directly to ground
            GameObject spawnRoot = GameObject.Find("SpawnManager");
            if (spawnRoot == null) spawnRoot = new GameObject("SpawnManager");
            SpawnPointManager spawnManager = spawnRoot.GetComponent<SpawnPointManager>();
            if (spawnManager == null) spawnManager = spawnRoot.AddComponent<SpawnPointManager>();

            var spawnTransforms = new List<Transform>();
            Vector3[] locations = {
                new Vector3(-5f, 5f, -5f),
                new Vector3(5f, 5f, -5f),
                new Vector3(-5f, 5f, 5f),
                new Vector3(5f, 5f, 5f),
                new Vector3(0f, 5f, -8f),
                new Vector3(0f, 5f, 8f)
            };

            for (int i = 0; i < locations.Length; i++)
            {
                Vector3 pos = locations[i];
                if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f))
                {
                    pos = hit.point + Vector3.up * 0.05f;
                }
                else
                {
                    pos.y = 0.05f;
                }

                string pointName = $"SpawnPoint_{i + 1:00}";
                Transform existing = spawnRoot.transform.Find(pointName);
                GameObject point = existing != null ? existing.gameObject : new GameObject(pointName);
                point.transform.SetParent(spawnRoot.transform);
                point.transform.position = pos;
                spawnTransforms.Add(point.transform);
            }
            spawnManager.Configure(spawnTransforms);

            // 4. Configure NetworkManager & Prefab
            GameObject networkPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPrefabPath);
            if (networkPlayerPrefab == null)
            {
                Debug.LogWarning("[Phase 8 Multiplayer] NetworkRobotPlayer prefab not found, running Phase 8 foundation setup.");
                SetupPhase8NetworkFoundation.Build();
                networkPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPrefabPath);
            }

            GameObject networkRoot = GameObject.Find("NetworkManager");
            if (networkRoot == null) networkRoot = new GameObject("NetworkManager");
            NetworkManager manager = networkRoot.GetComponent<NetworkManager>();
            if (manager == null) manager = networkRoot.AddComponent<NetworkManager>();

            UnityTransport transport = networkRoot.GetComponent<UnityTransport>();
            if (transport == null) transport = networkRoot.AddComponent<UnityTransport>();

            if (networkRoot.GetComponent<NetworkSessionController>() == null)
                networkRoot.AddComponent<NetworkSessionController>();

            if (networkRoot.GetComponent<NetworkDebugLauncher>() == null)
                networkRoot.AddComponent<NetworkDebugLauncher>();

            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                PlayerPrefab = networkPlayerPrefab,
                ConnectionApproval = true,
                TickRate = 30
            };

            EditorUtility.SetDirty(services);
            EditorUtility.SetDirty(spawnRoot);
            EditorUtility.SetDirty(networkRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
