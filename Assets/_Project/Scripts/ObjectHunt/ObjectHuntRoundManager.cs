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

            bool interactPressed = (input != null && input.ConsumeInteractPressed()) || UnityEngine.Input.GetKeyDown(KeyCode.E);
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
                FlatDistance(player.position, item.transform.position) <= Mathf.Max(3.2f, item.Definition != null ? item.Definition.interactionRange : 3.2f) &&
                Mathf.Abs(player.position.y - item.transform.position.y) < 3.5f)
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
            List<TargetDefinition> choices = targets.Where(item => item != null && item.category == category && item.prefab != null).ToList();
            if (choices.Count > 0) selectedTargets.Add(choices[UnityEngine.Random.Range(0, choices.Count)]);
        }

        private List<Vector3> FindSpawnPositions(int count)
        {
            NavMeshTriangulation mesh = NavMesh.CalculateTriangulation();
            var candidates = new List<Vector3>();
            if (!NavMesh.SamplePosition(player.position, out NavMeshHit playerNavHit, 8f, NavMesh.AllAreas))
            {
                Debug.LogError("[Object Hunt] Player is not near the baked NavMesh.");
                return candidates;
            }
            for (int i = 0; i + 2 < mesh.indices.Length; i += 3)
            {
                Vector3 point = (mesh.vertices[mesh.indices[i]] + mesh.vertices[mesh.indices[i + 1]] + mesh.vertices[mesh.indices[i + 2]]) / 3f;
                if (!NavMesh.SamplePosition(point, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas)) continue;
                point = hit.position;
                if (Mathf.Abs(point.y - player.position.y) > 5f || FlatDistance(point, player.position) < minimumPlayerSpawnDistance) continue;
                if (Physics.CheckSphere(point + Vector3.up * 0.45f, 0.32f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (candidates.Any(existing => FlatDistance(existing, point) < 8f)) continue;
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(playerNavHit.position, point, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                candidates.Add(point);
            }
            // Randomize first, then enforce spacing. This avoids always favoring one side of the triangulation.
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int swap = UnityEngine.Random.Range(0, i + 1);
                (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
            }
            var result = new List<Vector3>();
            SelectSpacedCandidates(candidates, result, count, minimumTargetSpacing);
            if (result.Count < count)
            {
                // Large spacing is a preference, not a reason to prevent the round from starting.
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
            // Sample exact ground height with physical raycast
            Vector3 groundPos = position;
            if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 15f, ~0, QueryTriggerInteraction.Ignore))
            {
                groundPos = hit.point;
            }

            // Additional ground clearance: Balls need more offset because of center pivots
            float extraClearance = definition.category == TargetCategory.Ball ? 0.18f : 0.08f;
            float targetBottomY = groundPos.y + extraClearance + definition.groundOffset;

            GameObject instance = Instantiate(definition.prefab, groundPos + Vector3.up * (extraClearance + definition.groundOffset), Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), transform);
            instance.name = $"Target_{definition.objectId}";
            float spawnScale = definition.category == TargetCategory.ToyCar
                ? Mathf.Max(1.5f, definition.worldScale)
                : Mathf.Max(0.1f, definition.worldScale);
            instance.transform.localScale *= spawnScale;

            PlaceVisualBottomOnGround(instance, targetBottomY);

            CollectibleTarget collectible = instance.GetComponent<CollectibleTarget>();
            if (collectible == null) collectible = instance.AddComponent<CollectibleTarget>();
            collectible.Configure(this, definition);
            activeTargets.Add(collectible);
        }

        private static void PlaceVisualBottomOnGround(GameObject instance, float targetBottomY)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds visualBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) visualBounds.Encapsulate(renderers[i].bounds);

            // Align bottom of rendered geometry so it rests visibly above the ground surface
            float currentBottom = visualBounds.min.y;
            float shift = targetBottomY - currentBottom;
            instance.transform.position += Vector3.up * shift;
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
