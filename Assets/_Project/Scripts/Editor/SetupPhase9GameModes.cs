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
using Robot.Player.Movement;
using Robot.UI;

namespace Robot.Editor
{
    public static class SetupPhase9GameModes
    {
        private const string PalettePath = "Assets/_Project/Data/RobotColorPalette.asset";
        private const string NetworkPrefabPath = "Assets/_Project/Prefabs/Characters/Player/NetworkRobotPlayer.prefab";
        private const string SingleplayerSourceScenePath = "Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity";
        private const string TargetScenesFolder = "Assets/_Project/Scenes";
        private const string MultiplayerDemo2ScenePath = TargetScenesFolder + "/Demo2.unity";
        private const string AltMultiplayerScenePath = TargetScenesFolder + "/Multiplayer/DEMO-2.unity";

        [MenuItem("Tools/Robot Hunt/Setup Singleplayer/Demo", false, 1)]
        public static void SetupSingleplayerDemo()
        {
            if (!File.Exists(SingleplayerSourceScenePath))
            {
                throw new InvalidOperationException($"Singleplayer Demo scene not found at: {SingleplayerSourceScenePath}");
            }

            Scene scene = EditorSceneManager.OpenScene(SingleplayerSourceScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open scene: {SingleplayerSourceScenePath}");
            }

            // Remove any network managers from singleplayer scene
            NetworkManager nm = UnityEngine.Object.FindFirstObjectByType<NetworkManager>();
            if (nm != null)
            {
                UnityEngine.Object.DestroyImmediate(nm.gameObject);
                Debug.Log("[Phase 9 Singleplayer] Removed NetworkManager from singleplayer scene.");
            }

            // Ensure singleplayer player & camera is setup
            SetupRobotPlayer.SetupPlayerInActiveScene();

            // Add GameModeSelectionUI if not present
            GameObject hudRoot = GameObject.Find("HUD_GameMode");
            if (hudRoot == null)
            {
                hudRoot = new GameObject("HUD_GameMode");
                hudRoot.AddComponent<GameModeSelectionUI>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            UpdateBuildSettings(SingleplayerSourceScenePath, MultiplayerDemo2ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Phase 9 Singleplayer] Successfully configured '{SingleplayerSourceScenePath}' for 1-Player Singleplayer mode.");
        }

        [MenuItem("Tools/Robot Hunt/Setup Multiplayer/Demo2", false, 2)]
        public static void SetupMultiplayerDemo2()
        {
            EnsureFolder(TargetScenesFolder);
            EnsureFolder(TargetScenesFolder + "/Multiplayer");

            if (!File.Exists(SingleplayerSourceScenePath))
            {
                throw new InvalidOperationException($"Source Demo scene not found at: {SingleplayerSourceScenePath}");
            }

            // Copy Demo.unity to Demo2.unity preserving 100% of the world geometry and lighting
            bool copied = AssetDatabase.CopyAsset(SingleplayerSourceScenePath, MultiplayerDemo2ScenePath);
            if (!copied)
            {
                File.Copy(SingleplayerSourceScenePath, MultiplayerDemo2ScenePath, true);
                AssetDatabase.Refresh();
            }

            // Also keep DEMO-2.unity updated
            File.Copy(SingleplayerSourceScenePath, AltMultiplayerScenePath, true);
            AssetDatabase.Refresh();

            Scene scene = EditorSceneManager.OpenScene(MultiplayerDemo2ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open scene: {MultiplayerDemo2ScenePath}");
            }

            ConfigureMultiplayerWorld(scene);

            UpdateBuildSettings(SingleplayerSourceScenePath, MultiplayerDemo2ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Phase 9 Multiplayer] Successfully built and configured '{MultiplayerDemo2ScenePath}' as the Multiplayer world!");
        }

        private static void ConfigureMultiplayerWorld(Scene scene)
        {
            // 1. Remove all static single-player objects to prevent duplicate spawn
            var singlePlayers = UnityEngine.Object.FindObjectsByType<RobotMovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sp in singlePlayers)
            {
                if (sp != null && sp.GetComponent<NetworkObject>() == null)
                {
                    UnityEngine.Object.DestroyImmediate(sp.gameObject);
                    Debug.Log("[Phase 9 Multiplayer] Removed static single-player object: " + sp.gameObject.name);
                }
            }

            // 2. Configure Palette & RobotColorService
            RobotColorPalette palette = AssetDatabase.LoadAssetAtPath<RobotColorPalette>(PalettePath);
            if (palette == null)
            {
                Debug.LogWarning("[Phase 9 Multiplayer] Color palette not found, building foundation first.");
                SetupPhase8NetworkFoundation.Build();
                palette = AssetDatabase.LoadAssetAtPath<RobotColorPalette>(PalettePath);
            }

            GameObject services = GameObject.Find("PlayerServices");
            if (services == null) services = new GameObject("PlayerServices");
            RobotColorService colorService = services.GetComponent<RobotColorService>();
            if (colorService == null) colorService = services.AddComponent<RobotColorService>();
            colorService.Configure(palette);

            // 3. Configure 4 Distinct Server-Authoritative SpawnPoints (SpawnPoint_01..04)
            GameObject spawnRoot = GameObject.Find("SpawnManager");
            if (spawnRoot == null) spawnRoot = new GameObject("SpawnManager");
            SpawnPointManager spawnManager = spawnRoot.GetComponent<SpawnPointManager>();
            if (spawnManager == null) spawnManager = spawnRoot.AddComponent<SpawnPointManager>();

            var spawnTransforms = new List<Transform>();
            Vector3[] locations = {
                new Vector3(-6f, 5f, -6f),   // SpawnPoint_01 (South-West)
                new Vector3(6f, 5f, -6f),    // SpawnPoint_02 (South-East)
                new Vector3(-6f, 5f, 6f),    // SpawnPoint_03 (North-West)
                new Vector3(6f, 5f, 6f),     // SpawnPoint_04 (North-East)
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
                point.transform.rotation = Quaternion.Euler(0f, (i * 90f) % 360f, 0f);
                spawnTransforms.Add(point.transform);
            }
            spawnManager.Configure(spawnTransforms);

            // 4. Configure NetworkManager & Prefab
            GameObject networkPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPrefabPath);
            if (networkPlayerPrefab == null)
            {
                Debug.LogWarning("[Phase 9 Multiplayer] NetworkRobotPlayer prefab not found, running Phase 8 foundation setup.");
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

            if (networkRoot.GetComponent<GameModeSelectionUI>() == null)
                networkRoot.AddComponent<GameModeSelectionUI>();

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

        private static void UpdateBuildSettings(params string[] scenePaths)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in scenePaths)
            {
                if (!scenes.Exists(s => s.path == path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
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
