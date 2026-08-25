using System;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.Spawning
{
    [DisallowMultipleComponent]
    public sealed class SpawnPointManager : MonoBehaviour
    {
        [Serializable]
        public sealed class SpawnPoint
        {
            public string id;
            public Transform transform;
            [NonSerialized] public ulong? occupant;
        }

        [SerializeField] private List<SpawnPoint> points = new List<SpawnPoint>();
        [SerializeField, Min(0f)] private float minimumSpawnDistance = 4f;
        [SerializeField, Min(0.1f)] private float collisionRadius = 0.4f;
        [SerializeField, Min(0.1f)] private float collisionHeight = 1.8f;
        [SerializeField] private LayerMask blockingLayers = ~0;

        public static SpawnPointManager Instance { get; private set; }
        public IReadOnlyList<SpawnPoint> Points => points;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Configure(IEnumerable<Transform> spawnTransforms)
        {
            points.Clear();
            foreach (Transform item in spawnTransforms)
                if (item != null) points.Add(new SpawnPoint { id = item.name, transform = item });
        }

        public bool TryReserve(ulong entityId, out Pose pose)
        {
            return TryReserve(entityId, out pose, out _);
        }

        public bool TryReserve(ulong entityId, out Pose pose, out int assignedIndex)
        {
            // If already reserved by this entity, return that pose
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].occupant == entityId && points[i].transform != null)
                {
                    assignedIndex = i;
                    pose = new Pose(points[i].transform.position, points[i].transform.rotation);
                    return true;
                }
            }

            // Find first free slot in order (SpawnPoint_01, then _02, then _03, then _04)
            for (int i = 0; i < points.Count; i++)
            {
                SpawnPoint point = points[i];
                if (point.transform == null) continue;
                if (point.occupant.HasValue) continue;

                if (!IsSafe(point)) continue;

                point.occupant = entityId;
                assignedIndex = i;
                pose = new Pose(point.transform.position, point.transform.rotation);
                Debug.Log($"[SpawnPointManager] Reserved {point.id} (index {i}) for client {entityId} at {pose.position}.");
                return true;
            }

            assignedIndex = -1;
            pose = default;
            Debug.LogWarning($"[SpawnPointManager] No available SpawnPoint found for client {entityId}. Total points: {points.Count}.");
            return false;
        }

        public void Release(ulong entityId)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].occupant == entityId)
                {
                    Debug.Log($"[SpawnPointManager] Released {points[i].id} (index {i}) from client {entityId}. Slot is now FREE.");
                    points[i].occupant = null;
                }
            }
        }

        public bool IsOccupied(int index)
        {
            if (index < 0 || index >= points.Count) return false;
            return points[index].occupant.HasValue;
        }

        public ulong? GetOccupant(int index)
        {
            if (index < 0 || index >= points.Count) return null;
            return points[index].occupant;
        }

        public int GetAvailableCount()
        {
            int count = 0;
            foreach (var p in points) if (p.transform != null && !p.occupant.HasValue) count++;
            return count;
        }

        public bool IsSafe(SpawnPoint point)
        {
            if (point == null || point.transform == null || point.occupant.HasValue) return false;
            foreach (SpawnPoint other in points)
            {
                if (other != point && other.occupant.HasValue && other.transform != null &&
                    Vector3.Distance(other.transform.position, point.transform.position) < minimumSpawnDistance)
                {
                    return false;
                }
            }

            Vector3 bottom = point.transform.position + Vector3.up * (collisionRadius + 0.08f);
            Vector3 top = point.transform.position + Vector3.up * Mathf.Max(collisionRadius + 0.15f, collisionHeight - collisionRadius);
            return !Physics.CheckCapsule(bottom, top, collisionRadius * 0.85f, blockingLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
