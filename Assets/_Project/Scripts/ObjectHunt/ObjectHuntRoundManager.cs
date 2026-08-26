using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Robot.Combat;
using Robot.Input;
using Robot.Player.Movement;

namespace Robot.ObjectHunt
{
    [DisallowMultipleComponent]
    public sealed class ObjectHuntRoundManager : MonoBehaviour
    {
        [Header("Target Catalog")]
        [SerializeField] private List<TargetDefinition> targets = new List<TargetDefinition>();
        [Header("Round")]
        [SerializeField, Min(1f)] private float roundDuration = 600f;
        [SerializeField, Min(0f)] private float minimumPlayerSpawnDistance = 25f;
        [SerializeField, Min(0f)] private float minimumTargetSpacing = 30f;
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 3f;
        [SerializeField] private Transform player;

        private readonly List<TargetDefinition> selectedTargets = new List<TargetDefinition>(3);
        private readonly List<CollectibleTarget> activeTargets = new List<CollectibleTarget>(3);
        private readonly List<TargetDefinition> collectedTargets = new List<TargetDefinition>(3);
        private PlayerInputReader input;
        private Transform pickupTarget;
        private float timeRemaining;
        private bool roundActive;

        public event Action<IReadOnlyList<TargetDefinition>> RoundStarted;
        public event Action<TargetDefinition, int> TargetCollected;
        public event Action RoundCompleted;
        public event Action RoundFailed;
        public event Action<float> TimerChanged;

        public IReadOnlyList<TargetDefinition> SelectedTargets => selectedTargets;
        public IReadOnlyList<CollectibleTarget> ActiveTargets => activeTargets;
        public IReadOnlyList<TargetDefinition> CollectedTargets => collectedTargets;
        public float TimeRemaining => timeRemaining;
        public float RoundDuration => roundDuration;
        public int CollectedCount { get; private set; }
        public bool IsRoundActive => roundActive;

        private void Start()
        {
            ResolvePlayer();
            if (GetComponent<RoundGameLoop>() == null) BeginRound();
        }

        private void Update()
        {
            if (player == null || input == null) ResolvePlayer();
            if (!roundActive) return;
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            TimerChanged?.Invoke(timeRemaining);
            if (timeRemaining <= 0f) FailRound();

            bool interactPressed = false;
            if (input != null && input.ConsumeInteractPressed()) interactPressed = true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.E)) interactPressed = true;
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                    interactPressed = true;
            }
            catch { }
#endif
            if (interactPressed) TryPickupNearest();
        }

        public void BeginRound()
        {
            if (PrepareRound()) StartSearch();
        }

        public bool PrepareRound()
        {
            EnsureTargetsCatalog();
            ResolvePlayer();

            CleanupTargets();
            selectedTargets.Clear();
            collectedTargets.Clear();

            SelectOne(TargetCategory.Ball);
            SelectOne(TargetCategory.TeddyBear);
            SelectOne(TargetCategory.ToyCar);

            // If any category was missing, fill up from any available target
            if (selectedTargets.Count < 3 && targets.Count > 0)
            {
                foreach (var t in targets)
                {
                    if (selectedTargets.Count >= 3) break;
                    if (t.prefab != null && !selectedTargets.Contains(t)) selectedTargets.Add(t);
                }
            }

            List<Vector3> positions = FindSpawnPositions(selectedTargets.Count > 0 ? selectedTargets.Count : 3);
            for (int i = 0; i < selectedTargets.Count && i < positions.Count; i++)
            {
                SpawnTarget(selectedTargets[i], positions[i]);
            }

            CollectedCount = 0;
            timeRemaining = roundDuration;
            roundActive = false;
            RoundStarted?.Invoke(selectedTargets);
            TimerChanged?.Invoke(timeRemaining);
            return activeTargets.Count > 0;
        }

        public void StartSearch()
        {
            if (activeTargets.Count == 0 || CollectedCount >= activeTargets.Count) return;
            roundActive = true;
        }

        public void StopSearch() => roundActive = false;

        public bool TryPickupNearest()
        {
            if (player == null) ResolvePlayer();
            if (player == null) return false;
            CollectibleTarget nearest = GetNearestInteractable();
            if (nearest == null) return false;
            return nearest.TryCollect(player, pickupTarget);
        }

        public CollectibleTarget GetNearestInteractable()
        {
            if (player == null) ResolvePlayer();
            if (player == null) return null;
            return activeTargets.Where(item => item != null && item.gameObject.activeSelf && !item.IsCollecting &&
                FlatDistance(player.position, item.transform.position) <= 5.5f &&
                Mathf.Abs(player.position.y - item.transform.position.y) < 5.0f)
                .OrderBy(item => FlatDistance(player.position, item.transform.position)).FirstOrDefault();
        }

        internal void NotifyCollected(CollectibleTarget collectible)
        {
            if (collectible == null) return;
            CollectedCount++;
            collectedTargets.Add(collectible.Definition);
            TargetCollected?.Invoke(collectible.Definition, CollectedCount);
            if (CollectedCount >= 3)
            {
                roundActive = false;
                RoundCompleted?.Invoke();
            }
        }

        public bool ResolvePlayer()
        {
            if (player == null)
            {
                // 1. Check for owner NetworkRobotPlayer in multiplayer
                var netPlayers = UnityEngine.Object.FindObjectsByType<Robot.Multiplayer.NetworkRobotPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var np in netPlayers)
                {
                    if (np != null && np.IsOwner)
                    {
                        player = np.transform;
                        break;
                    }
                }

                // 2. Fallback to Tag "Player"
                if (player == null)
                {
                    GameObject found = GameObject.FindGameObjectWithTag("Player");
                    if (found != null) player = found.transform;
                }

                // 3. Fallback to "RobotPlayer"
                if (player == null)
                {
                    GameObject found = GameObject.Find("RobotPlayer");
                    if (found != null) player = found.transform;
                }

                // 4. Fallback to any RobotMovementController
                if (player == null)
                {
                    var rmc = UnityEngine.Object.FindFirstObjectByType<Robot.Player.Movement.RobotMovementController>();
                    if (rmc != null) player = rmc.transform;
                }
            }

            if (player == null) return false;

            if (input == null) input = player.GetComponent<PlayerInputReader>();
            pickupTarget = player.Find("PickupTarget");
            if (pickupTarget == null)
            {
                GameObject targetObject = new GameObject("PickupTarget");
                pickupTarget = targetObject.transform;
                pickupTarget.SetParent(player, false);
                pickupTarget.localPosition = new Vector3(0f, 1.05f, 0.4f);
            }
            return true;
        }

        private void EnsureTargetsCatalog()
        {
            if (targets != null && targets.Count >= 3 &&
                targets.Any(t => t.category == TargetCategory.Ball && t.prefab != null) &&
                targets.Any(t => t.category == TargetCategory.TeddyBear && t.prefab != null) &&
                targets.Any(t => t.category == TargetCategory.ToyCar && t.prefab != null)) return;

            if (targets == null) targets = new List<TargetDefinition>();
            targets.Clear();

#if UNITY_EDITOR
            string root = "Assets/ThirdParty/Selected/toy/";
            (string id, TargetCategory category, string name, string file)[] catalog =
            {
                ("ball_01", TargetCategory.Ball, "Futbol Topu", "Prop_Ball_01.prefab"),
                ("ball_02", TargetCategory.Ball, "Sarı-Beyaz Top", "Prop_Ball_02.prefab"),
                ("ball_03", TargetCategory.Ball, "Mavi Top", "Prop_Ball_03.prefab"),
                ("ball_04", TargetCategory.Ball, "Kırmızı Top", "Prop_Ball_04.prefab"),
                ("teddy_01", TargetCategory.TeddyBear, "Oyuncak Ayı 1", "Prop_TeddyBear_01.prefab"),
                ("teddy_02", TargetCategory.TeddyBear, "Oyuncak Ayı 2", "Prop_TeddyBear_02.prefab"),
                ("teddy_03", TargetCategory.TeddyBear, "Oyuncak Ayı 3", "Prop_TeddyBear_03.prefab"),
                ("teddy_04", TargetCategory.TeddyBear, "Oyuncak Ayı 4", "Prop_TeddyBear_04.prefab"),
                ("car_blue", TargetCategory.ToyCar, "Mavi Araba", "Prop_ToyCar_Blue.prefab"),
                ("car_green", TargetCategory.ToyCar, "Yeşil Araba", "Prop_ToyCar_Green.prefab"),
                ("car_yellow", TargetCategory.ToyCar, "Sarı Araba", "Prop_ToyCar_Yellow.prefab"),
                ("car_red", TargetCategory.ToyCar, "Kırmızı Araba", "Prop_ToyCar_Red.prefab")
            };
            foreach (var item in catalog)
            {
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(root + item.file);
                if (prefab != null)
                {
                    targets.Add(new TargetDefinition
                    {
                        objectId = item.id,
                        category = item.category,
                        displayName = item.name,
                        prefab = prefab,
                        worldScale = item.category == TargetCategory.ToyCar ? 1.5f : 1f,
                        interactionRange = 5.5f,
                        groundOffset = 0f
                    });
                }
            }
#endif
        }

        private void SelectOne(TargetCategory category)
        {
            List<TargetDefinition> pool = targets.Where(item => item != null && item.category == category && item.prefab != null).ToList();
            if (pool.Count == 0) return;
            selectedTargets.Add(pool[UnityEngine.Random.Range(0, pool.Count)]);
        }

        private List<Vector3> FindSpawnPositions(int count)
        {
            var result = new List<Vector3>();
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices != null && triangulation.vertices.Length > 0)
            {
                var candidates = new List<Vector3>();
                for (int i = 0; i < triangulation.vertices.Length; i++)
                {
                    if (NavMesh.SamplePosition(triangulation.vertices[i], out NavMeshHit hit, 3.5f, NavMesh.AllAreas))
                        candidates.Add(hit.position);
                }

                for (int i = 0; i < candidates.Count; i++)
                {
                    int swap = UnityEngine.Random.Range(i, candidates.Count);
                    (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
                }

                SelectSpacedCandidates(candidates, result, count, minimumTargetSpacing);
                if (result.Count < count)
                {
                    SelectSpacedCandidates(candidates, result, count, Mathf.Max(5f, minimumTargetSpacing * 0.5f));
                }
            }

            // Reliable fallback positions in city center
            Vector3[] fallbacks = {
                new Vector3(-4f, 0.05f, 5f),
                new Vector3(5f, 0.05f, -3f),
                new Vector3(0f, 0.05f, -8f),
                new Vector3(-6f, 0.05f, -4f)
            };
            int fbIndex = 0;
            while (result.Count < count && fbIndex < fallbacks.Length)
            {
                Vector3 fb = fallbacks[fbIndex++];
                if (!result.Any(r => FlatDistance(r, fb) < 3f)) result.Add(fb);
            }

            return result;
        }

        private static void SelectSpacedCandidates(List<Vector3> candidates, List<Vector3> result, int count, float spacing)
        {
            foreach (Vector3 candidate in candidates)
            {
                if (result.Count >= count) return;
                if (result.Any(existing => FlatDistance(existing, candidate) < spacing)) continue;
                result.Add(candidate);
            }
        }

        private void SpawnTarget(TargetDefinition definition, Vector3 position)
        {
            // Sample exact walk and physical collider heights
            float walkY = position.y;
            float groundY = walkY;
            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 25f, ~0, QueryTriggerInteraction.Ignore))
            {
                groundY = Mathf.Max(walkY, hit.point.y);
            }

            // High elevation so balls and toys clearly float above sidewalks and pavements
            float originElevation = definition.category == TargetCategory.Ball ? 0.50f : 0.35f;
            originElevation += definition.groundOffset;

            Vector3 spawnWorldPos = new Vector3(position.x, groundY + originElevation, position.z);

            GameObject instance = Instantiate(definition.prefab, spawnWorldPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
            instance.name = $"Target_{definition.objectId}";

            float spawnScale = definition.category == TargetCategory.ToyCar
                ? Mathf.Max(1.5f, definition.worldScale)
                : Mathf.Max(0.1f, definition.worldScale);
            instance.transform.localScale = Vector3.one * spawnScale;
            instance.transform.position = spawnWorldPos;

            CollectibleTarget collectible = instance.GetComponent<CollectibleTarget>();
            if (collectible == null) collectible = instance.AddComponent<CollectibleTarget>();
            collectible.Configure(this, definition);
            activeTargets.Add(collectible);
        }

        private void FailRound()
        {
            if (!roundActive) return;
            roundActive = false;
            RoundFailed?.Invoke();
        }

        private void CleanupTargets()
        {
            foreach (CollectibleTarget item in activeTargets) if (item != null) Destroy(item.gameObject);
            activeTargets.Clear();
        }

        private static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y; return Vector3.Distance(a, b); }
    }
}
