using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Robot.Combat;
using Robot.Input;
using Robot.NPC;
using Robot.Player.Movement;
using Robot.Robots.Customization;
using Robot.UI.HUD;
using Robot.ObjectHunt;

namespace Robot.Editor
{
    public static class SetupPhase4Combat
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player/RobotPlayer.prefab";
        private const string NpcFolder = "Assets/_Project/Prefabs/Characters/NPC";
        private const string NpcPrefabPath = NpcFolder + "/RobotNPC.prefab";
        private const string GeneratedRootName = "Phase 4 NPC System";
        private const int NpcCount = 9;
        private const int AggressionZoneCount = 6;
        private const float PlayerSafetyDistance = 30f;
        private const float MinimumNpcSpacing = 24f;
        private const string ExpectedSceneSuffix = "/PolygonCity/Scenes/Demo.unity";
        private const string DemoScenePath = "Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity";

        [InitializeOnLoadMethod]
        private static void RegisterAutomaticObjectHuntInstall()
        {
            EditorApplication.delayCall += TryInstallObjectHuntIntoExistingDemoSetup;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += TryInstallObjectHuntIntoExistingDemoSetup;
            };
        }

        private static void TryInstallObjectHuntIntoExistingDemoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.EndsWith(ExpectedSceneSuffix, StringComparison.OrdinalIgnoreCase)) return;
            GameObject root = GameObject.Find(GeneratedRootName);
            GameObject player = FindPlayer();
            if (root == null || player == null) return;
            if (root.GetComponent<ObjectHuntRoundManager>() != null && root.GetComponent<RoundGameLoop>() != null &&
                root.GetComponent<ObjectHuntHUD>() != null) return;
            InstallObjectHunt(scene, root, player);
        }

        [MenuItem("Tools/Robot Hunt/Phase 5-6/Setup Object Hunt", false, 1)]
        public static void SetupObjectHuntInDemoScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.EndsWith(ExpectedSceneSuffix, StringComparison.OrdinalIgnoreCase))
                scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find(GeneratedRootName);
            GameObject player = FindPlayer();
            if (root == null || player == null)
                throw new InvalidOperationException("Demo Scene Phase 4 NPC setup or Player is missing.");
            InstallObjectHunt(scene, root, player);
        }

        private static void InstallObjectHunt(Scene scene, GameObject root, GameObject player)
        {
            ObjectHuntRoundManager manager = root.GetComponent<ObjectHuntRoundManager>();
            if (manager == null) manager = root.AddComponent<ObjectHuntRoundManager>();
            ConfigureObjectHunt(manager, player.transform);
            if (root.GetComponent<RoundGameLoop>() == null) root.AddComponent<RoundGameLoop>();
            if (root.GetComponent<ObjectHuntHUD>() == null) root.AddComponent<ObjectHuntHUD>();
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Object Hunt] Demo Scene upgraded; manager, 13-item catalog and HUD are installed.");
        }

        [MenuItem("Tools/Robot Hunt/Phase 4/Setup NPC System", false, 1)]
        public static void SetupNpcSystem()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.EndsWith(ExpectedSceneSuffix, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Robot Hunt Phase 4", "PolygonCity/Scenes/Demo.unity sahnesini açıp tekrar çalıştırın. Başka sahne değiştirilmedi.", "Tamam");
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Phase 4] Exit Play Mode before running scene setup.");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Robot Hunt Phase 4", "Combat prefabları hazırlanıyor...", 0.1f);
                CreateCombatPrefabs();
                GameObject player = FindPlayer();
                if (player == null) throw new InvalidOperationException("Demo Scene içinde Player/RobotPlayer bulunamadı.");
                ConfigureScenePlayer(player);

                GameObject oldRoot = GameObject.Find(GeneratedRootName);
                if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);
                GameObject systemRoot = new GameObject(GeneratedRootName);

                EditorUtility.DisplayProgressBar("Robot Hunt Phase 4", "Şehir NavMesh'i oluşturuluyor...", 0.35f);
                NavMeshSurface surface = systemRoot.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
                surface.layerMask = ~0;
                surface.BuildNavMesh();

                EditorUtility.DisplayProgressBar("Robot Hunt Phase 4", "Dengeli ve erişilebilir NPC bölgeleri seçiliyor...", 0.65f);
                List<Vector3> spawnPositions = PlanSpawnPositions(player.transform.position);
                if (spawnPositions.Count < NpcCount)
                    throw new InvalidOperationException($"NavMesh üzerinde yalnızca {spawnPositions.Count} güvenli ve ayrık konum bulundu.");

                Transform spawnRoot = CreateChild(systemRoot.transform, "Generated Spawn Points");
                Transform[] spawnPoints = new Transform[NpcCount];
                for (int i = 0; i < NpcCount; i++)
                {
                    Transform point = CreateChild(spawnRoot, $"NPC Spawn {i + 1:00}");
                    point.position = spawnPositions[i];
                    point.rotation = Quaternion.Euler(0f, (i * 137.508f) % 360f, 0f);
                    spawnPoints[i] = point;
                }

                AggressionZone[] zones = CreateAggressionZones(systemRoot.transform, spawnPositions);
                AttackSlotCoordinator coordinator = systemRoot.AddComponent<AttackSlotCoordinator>();
                SerializedObject serializedCoordinator = new SerializedObject(coordinator);
                serializedCoordinator.FindProperty("maxActiveAttackers").intValue = 2;
                serializedCoordinator.ApplyModifiedPropertiesWithoutUndo();

                NpcSpawnManager spawner = systemRoot.AddComponent<NpcSpawnManager>();
                SerializedObject serializedSpawner = new SerializedObject(spawner);
                serializedSpawner.FindProperty("npcCount").intValue = NpcCount;
                serializedSpawner.FindProperty("npcPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<NpcRobotController>(NpcPrefabPath);
                serializedSpawner.FindProperty("player").objectReferenceValue = player.transform;
                serializedSpawner.FindProperty("minimumPlayerSpawnDistance").floatValue = PlayerSafetyDistance;
                SetObjectArray(serializedSpawner.FindProperty("spawnPoints"), spawnPoints);
                SetObjectArray(serializedSpawner.FindProperty("aggressionZones"), zones);
                serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

                ObjectHuntRoundManager objectHunt = systemRoot.AddComponent<ObjectHuntRoundManager>();
                ConfigureObjectHunt(objectHunt, player.transform);
                systemRoot.AddComponent<RoundGameLoop>();
                systemRoot.AddComponent<ObjectHuntHUD>();

                EditorUtility.SetDirty(systemRoot);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                string positions = string.Join(", ", spawnPositions.Select((p, i) => $"NPC {i + 1}: ({p.x:0.0}, {p.y:0.0}, {p.z:0.0})"));
                Debug.Log($"[Phase 4] Setup complete. Spawns: {positions}. Aggression zones: {zones.Length}. Player safety distance: {PlayerSafetyDistance:0}m.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Phase 4] Automatic setup failed: {exception.Message}\n{exception.StackTrace}");
                EditorUtility.DisplayDialog("Robot Hunt Phase 4", exception.Message, "Tamam");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("Tools/Robot Hunt/Phase 4/Create or Refresh Combat Prefabs", false, 20)]
        public static void CreateCombatPrefabs()
        {
            ConfigurePlayerPrefab();
            CreateNpcPrefab();
            AssetDatabase.SaveAssets();
        }

        private static GameObject FindPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player : GameObject.Find("RobotPlayer");
        }

        private static void ConfigureScenePlayer(GameObject player)
        {
            player.tag = "Player";
            CombatHealth health = player.GetComponent<CombatHealth>();
            if (health == null) health = player.AddComponent<CombatHealth>();
            health.SetTeam(CombatTeam.Player);
            SetHealthBalance(health, 200f, 60f, true);
            CombatAttack attack = player.GetComponent<CombatAttack>();
            if (attack == null) attack = player.AddComponent<CombatAttack>();
            SetAttackBalance(attack, 30f, 0.9f);
            if (player.GetComponent<PlayerCombatInput>() == null) player.AddComponent<PlayerCombatInput>();
            EditorUtility.SetDirty(player);
        }

        private static List<Vector3> PlanSpawnPositions(Vector3 playerPosition)
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            var candidates = new List<Vector3>();
            for (int i = 0; i + 2 < triangulation.indices.Length; i += 3)
            {
                Vector3 a = triangulation.vertices[triangulation.indices[i]];
                Vector3 b = triangulation.vertices[triangulation.indices[i + 1]];
                Vector3 c = triangulation.vertices[triangulation.indices[i + 2]];
                AddCandidate((a + b + c) / 3f, playerPosition, candidates);
                AddCandidate(a, playerPosition, candidates);
            }

            candidates = candidates.OrderBy(p => p.x).ThenBy(p => p.z).ToList();
            if (candidates.Count == 0) return new List<Vector3>();
            Vector3 playableCenter = new Vector3(candidates.Average(p => p.x), playerPosition.y, candidates.Average(p => p.z));
            List<Vector3> routeAnchors = FindToyRouteAnchors();
            routeAnchors.Add(playerPosition);
            var selected = new List<Vector3>();
            while (selected.Count < NpcCount && candidates.Count > 0)
            {
                Vector3 best = default;
                float bestScore = float.MinValue;
                foreach (Vector3 candidate in candidates)
                {
                    float nearestNpc = selected.Count == 0 ? MinimumNpcSpacing : selected.Min(p => FlatDistance(candidate, p));
                    if (selected.Count > 0 && nearestNpc < MinimumNpcSpacing) continue;
                    float centerDistance = FlatDistance(candidate, playableCenter);
                    float routeDistance = routeAnchors.Min(anchor => FlatDistance(candidate, anchor));
                    // Spacing still matters, but central streets and likely toy-search routes beat remote map corners.
                    float score = Mathf.Min(nearestNpc, 45f) - centerDistance * 0.45f - routeDistance * 0.30f;
                    if (score > bestScore) { bestScore = score; best = candidate; }
                }
                if (bestScore == float.MinValue) break;
                selected.Add(best);
                candidates.RemoveAll(p => FlatDistance(p, best) < MinimumNpcSpacing);
            }
            return selected;
        }

        private static List<Vector3> FindToyRouteAnchors()
        {
            return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(item => item.name.IndexOf("Ball", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               item.name.IndexOf("Teddy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               item.name.IndexOf("ToyCar", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => item.position)
                .ToList();
        }

        private static void AddCandidate(Vector3 source, Vector3 playerPosition, List<Vector3> candidates)
        {
            if (!NavMesh.SamplePosition(source, out NavMeshHit hit, 2.5f, NavMesh.AllAreas)) return;
            Vector3 point = hit.position;
            if (Mathf.Abs(point.y - playerPosition.y) > 5f || FlatDistance(point, playerPosition) < PlayerSafetyDistance) return;
            if (Physics.CheckCapsule(point + Vector3.up * 0.45f, point + Vector3.up * 1.55f, 0.32f, ~0, QueryTriggerInteraction.Ignore)) return;
            if (candidates.Any(existing => FlatDistance(existing, point) < 4f)) return;
            if (!NavMesh.SamplePosition(playerPosition, out NavMeshHit playerHit, 6f, NavMesh.AllAreas)) return;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(playerHit.position, point, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return;
            candidates.Add(point);
        }

        private static AggressionZone[] CreateAggressionZones(Transform root, IReadOnlyList<Vector3> spawns)
        {
            Transform zoneRoot = CreateChild(root, "Generated Aggression Zones");
            var zones = new AggressionZone[AggressionZoneCount];
            int[] dangerousSpawnIndices = { 0, 2, 3, 5, 7, 8 };
            for (int i = 0; i < zones.Length; i++)
            {
                GameObject zoneObject = new GameObject($"Danger Zone {i + 1:00}");
                zoneObject.transform.SetParent(zoneRoot, false);
                zoneObject.transform.position = spawns[dangerousSpawnIndices[i]] + Vector3.up * 2.5f;
                SphereCollider collider = zoneObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = i % 2 == 0 ? 26f : 22f;
                zones[i] = zoneObject.AddComponent<AggressionZone>();
            }
            return zones;
        }

        private static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y; return Vector3.Distance(a, b); }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void ConfigurePlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                CombatHealth health = root.GetComponent<CombatHealth>();
                if (health == null) health = root.AddComponent<CombatHealth>();
                health.SetTeam(CombatTeam.Player);
                SetHealthBalance(health, 200f, 60f, true);
                CombatAttack attack = root.GetComponent<CombatAttack>();
                if (attack == null) attack = root.AddComponent<CombatAttack>();
                SetAttackBalance(attack, 30f, 0.9f);
                if (root.GetComponent<PlayerCombatInput>() == null) root.AddComponent<PlayerCombatInput>();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void CreateNpcPrefab()
        {
            if (!AssetDatabase.IsValidFolder(NpcFolder)) AssetDatabase.CreateFolder("Assets/_Project/Prefabs/Characters", "NPC");
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                root.name = "RobotNPC";
                DestroyImmediateIfPresent<PlayerCombatInput>(root);
                DestroyImmediateIfPresent<RobotMovementController>(root);
                DestroyImmediateIfPresent<PlayerInputReader>(root);
                DestroyImmediateIfPresent<RobotShowcaseUI>(root);
                DestroyImmediateIfPresent<CharacterController>(root);
                NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
                if (agent == null) agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 0.35f; agent.height = 1.8f; agent.speed = 2.6f; agent.angularSpeed = 720f; agent.acceleration = 14f; agent.avoidancePriority = 50;
                CapsuleCollider collider = root.GetComponent<CapsuleCollider>();
                if (collider == null) collider = root.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 0.9f, 0f); collider.radius = 0.35f; collider.height = 1.8f;
                CombatHealth health = root.GetComponent<CombatHealth>();
                if (health == null) health = root.AddComponent<CombatHealth>();
                health.SetTeam(CombatTeam.Npc);
                SetHealthBalance(health, 200f, 0f, false);
                CombatAttack attack = root.GetComponent<CombatAttack>();
                if (attack == null) attack = root.AddComponent<CombatAttack>();
                SetAttackBalance(attack, 16f, 1.25f, 1.9f);
                NpcRobotController npc = root.GetComponent<NpcRobotController>();
                if (npc == null) npc = root.AddComponent<NpcRobotController>();
                SerializedObject serializedNpc = new SerializedObject(npc);
                serializedNpc.FindProperty("aggressionRadius").floatValue = 30f;
                serializedNpc.FindProperty("fieldOfViewAngle").floatValue = 160f;
                serializedNpc.FindProperty("closeAwarenessRadius").floatValue = 30f;
                serializedNpc.FindProperty("eyeHeight").floatValue = 1.35f;
                serializedNpc.FindProperty("loseTargetDistance").floatValue = 24f;
                serializedNpc.FindProperty("chaseSpeed").floatValue = 5f;
                serializedNpc.FindProperty("sprintChaseSpeed").floatValue = 6.7f;
                serializedNpc.FindProperty("reactionTimeRange").vector2Value = new Vector2(0.05f, 0.12f);
                serializedNpc.ApplyModifiedPropertiesWithoutUndo();
                if (root.GetComponent<RobotColorCustomizer>() == null) root.AddComponent<RobotColorCustomizer>();
                PrefabUtility.SaveAsPrefabAsset(root, NpcPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void DestroyImmediateIfPresent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null) UnityEngine.Object.DestroyImmediate(component, true);
        }

        private static void SetObjectArray<T>(SerializedProperty property, T[] values) where T : UnityEngine.Object
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void ConfigureObjectHunt(ObjectHuntRoundManager manager, Transform player)
        {
            string root = "Assets/ThirdParty/Selected/toy/";
            (string id, TargetCategory category, string name, string file)[] catalog =
            {
                ("ball_01", TargetCategory.Ball, "Ball 1", "Prop_Ball_01.prefab"),
                ("ball_02", TargetCategory.Ball, "Ball 2", "Prop_Ball_02.prefab"),
                ("ball_03", TargetCategory.Ball, "Ball 3", "Prop_Ball_03.prefab"),
                ("ball_04", TargetCategory.Ball, "Ball 4", "Prop_Ball_04.prefab"),
                ("teddy_01", TargetCategory.TeddyBear, "TeddyBear 1", "Prop_TeddyBear_01.prefab"),
                ("teddy_02", TargetCategory.TeddyBear, "TeddyBear 2", "Prop_TeddyBear_02.prefab"),
                ("teddy_03", TargetCategory.TeddyBear, "TeddyBear 3", "Prop_TeddyBear_03.prefab"),
                ("teddy_04", TargetCategory.TeddyBear, "TeddyBear 4", "Prop_TeddyBear_04.prefab"),
                ("teddy_05", TargetCategory.TeddyBear, "TeddyBear 5", "Prop_TeddyBear_05.prefab"),
                ("car_blue", TargetCategory.ToyCar, "ToyCar Blue", "Prop_ToyCar_Blue.prefab"),
                ("car_green", TargetCategory.ToyCar, "ToyCar Green", "Prop_ToyCar_Green.prefab"),
                ("car_yellow", TargetCategory.ToyCar, "ToyCar Yellow", "Prop_ToyCar_Yellow.prefab"),
                ("car_red", TargetCategory.ToyCar, "ToyCar Red", "Prop_ToyCar_Red.prefab")
            };

            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("roundDuration").floatValue = 600f;
            serialized.FindProperty("player").objectReferenceValue = player;
            SerializedProperty targets = serialized.FindProperty("targets");
            targets.arraySize = catalog.Length;
            for (int i = 0; i < catalog.Length; i++)
            {
                SerializedProperty item = targets.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("objectId").stringValue = catalog[i].id;
                item.FindPropertyRelative("category").enumValueIndex = (int)catalog[i].category;
                item.FindPropertyRelative("displayName").stringValue = catalog[i].name;
                item.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(root + catalog[i].file);
                item.FindPropertyRelative("worldScale").floatValue = catalog[i].category == TargetCategory.ToyCar ? 1.5f : 1f;
                item.FindPropertyRelative("interactionRange").floatValue = 2.2f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetHealthBalance(CombatHealth health, float maxHealth, float knockoutDuration, bool recovers)
        {
            SerializedObject serialized = new SerializedObject(health);
            serialized.FindProperty("maxHealth").floatValue = maxHealth;
            serialized.FindProperty("knockoutDuration").floatValue = knockoutDuration;
            serialized.FindProperty("recoverAtFullHealth").boolValue = true;
            serialized.FindProperty("recoverAfterKnockout").boolValue = recovers;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetAttackBalance(CombatAttack attack, float damage, float cooldown, float attackRange = 1.65f)
        {
            SerializedObject serialized = new SerializedObject(attack);
            serialized.FindProperty("damage").floatValue = damage;
            serialized.FindProperty("attackCooldown").floatValue = cooldown;
            serialized.FindProperty("attackRange").floatValue = attackRange;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
