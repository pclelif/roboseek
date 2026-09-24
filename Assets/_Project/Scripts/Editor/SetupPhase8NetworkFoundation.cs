#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
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
    public static class SetupPhase8NetworkFoundation
    {
        private const string DataFolder = "Assets/_Project/Data";
        private const string PalettePath = DataFolder + "/RobotColorPalette.asset";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player/RobotPlayer.prefab";
        private const string NetworkPrefabPath = "Assets/_Project/Prefabs/Characters/Player/NetworkRobotPlayer.prefab";
        private const string SceneFolder = "Assets/_Project/Scenes/Multiplayer";
        private const string ScenePath = SceneFolder + "/NetworkFoundationTest.unity";

        [MenuItem("Tools/Robot Hunt/Phase 8/Build Network Foundation", false, 1)]
        public static void Build()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(SceneFolder);
            RobotColorPalette palette = CreateOrUpdatePalette();
            AssignPaletteToSinglePlayerPrefab(palette);
            GameObject networkPlayer = CreateNetworkPlayerPrefab(palette);
            CreateTestScene(palette, networkPlayer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase 8] Color palette, spawn foundation, network player and test scene created successfully.");
        }

        private static RobotColorPalette CreateOrUpdatePalette()
        {
            RobotColorPalette palette = AssetDatabase.LoadAssetAtPath<RobotColorPalette>(PalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<RobotColorPalette>();
                AssetDatabase.CreateAsset(palette, PalettePath);
            }
            palette.SetEntries(new[]
            {
                Entry("Black", "Black", 0.14f, 0.14f, 0.16f, 0.08f),
                Entry("Red", "Red", 0.74f, 0.12f, 0.12f),
                Entry("Orange", "Orange", 0.90f, 0.45f, 0.10f),
                Entry("Yellow", "Yellow", 0.94f, 0.72f, 0.10f),
                Entry("Green", "Green", 0.16f, 0.64f, 0.16f),
                Entry("Blue", "Blue", 0.15f, 0.42f, 0.78f),
                Entry("Purple", "Purple", 0.52f, 0.20f, 0.68f),
                Entry("Pink", "Pink", 0.85f, 0.35f, 0.52f),
                Entry("Brown", "Brown", 0.48f, 0.30f, 0.18f),
                Entry("White", "White", 0.92f, 0.93f, 0.95f, 0.20f)
            });
            EditorUtility.SetDirty(palette);
            return palette;
        }

        private static RobotColorPalette.Entry Entry(string id, string name, float r, float g, float b, float joint = 0.14f)
        {
            return new RobotColorPalette.Entry
            {
                id = id, displayName = name, bodyColor = new Color(r, g, b),
                jointColor = new Color(joint, joint, joint)
            };
        }

        private static GameObject CreateNetworkPlayerPrefab(RobotColorPalette palette)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (source == null) throw new System.InvalidOperationException("RobotPlayer prefab was not found.");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "NetworkRobotPlayer";
            AddIfMissing<NetworkObject>(instance);
            AddIfMissing<OwnerNetworkTransform>(instance);
            AddIfMissing<NetworkRobotPlayer>(instance);
            RobotColorCustomizer colors = instance.GetComponent<RobotColorCustomizer>();
            if (colors != null) colors.SetPalette(palette);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, NetworkPrefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.ImportAsset(NetworkPrefabPath, ImportAssetOptions.ForceUpdate);
            NetworkObject networkObject = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPrefabPath).GetComponent<NetworkObject>();
            MethodInfo validate = typeof(NetworkObject).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            validate?.Invoke(networkObject, null);
            EditorUtility.SetDirty(networkObject);
            AssetDatabase.SaveAssetIfDirty(networkObject);
            saved = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPrefabPath);
            return saved;
        }

        private static void AssignPaletteToSinglePlayerPrefab(RobotColorPalette palette)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            RobotColorCustomizer colors = root.GetComponent<RobotColorCustomizer>();
            if (colors != null) colors.SetPalette(palette);
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void CreateTestScene(RobotColorPalette palette, GameObject networkPlayer)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "NetworkFoundationTest";

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "NetworkTestGround";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);

            var lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 13f, -15f), Quaternion.Euler(32f, 0f, 0f));
            camera.fieldOfView = 60f;

            var services = new GameObject("PlayerServices");
            RobotColorService colorService = services.AddComponent<RobotColorService>();
            colorService.Configure(palette);

            var spawnRoot = new GameObject("SpawnManager");
            SpawnPointManager spawnManager = spawnRoot.AddComponent<SpawnPointManager>();
            var spawnTransforms = new List<Transform>();
            Vector3[] locations = { new Vector3(-8f, 0.05f, -8f), new Vector3(8f, 0.05f, -8f), new Vector3(-8f, 0.05f, 8f), new Vector3(8f, 0.05f, 8f) };
            for (int i = 0; i < locations.Length; i++)
            {
                var point = new GameObject($"SpawnPoint_{i + 1:00}");
                point.transform.SetParent(spawnRoot.transform);
                point.transform.position = locations[i];
                spawnTransforms.Add(point.transform);
            }
            spawnManager.Configure(spawnTransforms);

            var networkRoot = new GameObject("NetworkManager");
            NetworkManager manager = networkRoot.AddComponent<NetworkManager>();
            UnityTransport transport = networkRoot.AddComponent<UnityTransport>();
            networkRoot.AddComponent<NetworkSessionController>();
            networkRoot.AddComponent<NetworkDebugLauncher>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                PlayerPrefab = networkPlayer,
                ConnectionApproval = true,
                TickRate = 30
            };

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            var scenes = new List<EditorBuildSettingsScene>(existing);
            if (!scenes.Exists(item => item.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T AddIfMissing<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
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
