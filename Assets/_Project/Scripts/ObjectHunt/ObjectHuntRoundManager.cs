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
            if (!ResolvePlayer() || targets.Count == 0) return false;
            CleanupTargets();
            selectedTargets.Clear();
            collectedTargets.Clear();
            SelectOne(TargetCategory.Ball);
            SelectOne(TargetCategory.TeddyBear);
            SelectOne(TargetCategory.ToyCar);
            if (selectedTargets.Count != 3) { Debug.LogError("[Object Hunt] Target catalog is incomplete."); return false; }

            List<Vector3> positions = FindSpawnPositions(3);
            if (positions.Count != 3) { Debug.LogError("[Object Hunt] Could not find three valid NavMesh target positions."); return false; }
            for (int i = 0; i < 3; i++) SpawnTarget(selectedTargets[i], positions[i]);
            CollectedCount = 0;
            timeRemaining = roundDuration;
            roundActive = false;
            RoundStarted?.Invoke(selectedTargets);
            TimerChanged?.Invoke(timeRemaining);
            return true;
        }

        public void StartSearch()
        {
            if (selectedTargets.Count != 3 || CollectedCount >= 3) return;
            roundActive = true;
        }

        public void StopSearch() => roundActive = false;

        public bool TryPickupNearest()
        {
            if (player == null) ResolvePlayer();
            if (player == null) return false;
            CombatHealth health = player.GetComponent<CombatHealth>();
            if (health != null && health.IsKnockedOut) return false;
            CollectibleTarget nearest = GetNearestInteractable();
            if (nearest == null) return false;
            return nearest.TryCollect(player, pickupTarget);
        }

        public CollectibleTarget GetNearestInteractable()
        {
            if (player == null) ResolvePlayer();
            if (player == null) return null;
            return activeTargets.Where(item => item != null && item.gameObject.activeSelf && !item.IsCollecting &&
                FlatDistance(player.position, item.transform.position) <= 4.5f &&
                Mathf.Abs(player.position.y - item.transform.position.y) < 4.0f)
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

        private bool ResolvePlayer()
        {
            if (player == null)
            {
                GameObject found = GameObject.FindGameObjectWithTag("Player");
                if (found == null) found = GameObject.Find("RobotPlayer");
                if (found == null)
                {
                    var rmc = UnityEngine.Object.FindFirstObjectByType<Robot.Player.Movement.RobotMovementController>();
                    if (rmc != null) found = rmc.gameObject;
                }
                if (found != null) player = found.transform;
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

        private void SelectOne(TargetCategory category)
        {
            List<TargetDefinition> pool = targets.Where(item => item.category == category).ToList();
            if (pool.Count == 0) return;
            selectedTargets.Add(pool[UnityEngine.Random.Range(0, pool.Count)]);
        }

        private List<Vector3> FindSpawnPositions(int count)
        {
            var result = new List<Vector3>();
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices == null || triangulation.vertices.Length == 0) return result;

            var candidates = new List<Vector3>();
            for (int i = 0; i < triangulation.vertices.Length; i++)
            {
                if (NavMesh.SamplePosition(triangulation.vertices[i], out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
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
                SelectSpacedCandidates(candidates, result, count, Mathf.Max(10f, minimumTargetSpacing * 0.5f));
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
            // Sample exact physical ground height with vertical raycast
            Vector3 groundPos = position;
            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 25f, ~0, QueryTriggerInteraction.Ignore))
            {
                groundPos = hit.point;
            }

            // Balls have a centered origin (radius ~ 0.22m), so their center must sit at groundPos.y + 0.38m
            // Other toys have a bottom origin, so their center/origin must sit at groundPos.y + 0.15m
            float originElevation = definition.category == TargetCategory.Ball ? 0.38f : 0.15f;
            originElevation += definition.groundOffset;

            Vector3 spawnWorldPos = new Vector3(groundPos.x, groundPos.y + originElevation, groundPos.z);

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
