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
            Release(entityId);
            int start = points.Count > 0 ? (int)(entityId % (ulong)points.Count) : 0;
            for (int offset = 0; offset < points.Count; offset++)
            {
                SpawnPoint point = points[(start + offset) % points.Count];
                if (!IsSafe(point)) continue;
                point.occupant = entityId;
                pose = new Pose(point.transform.position, point.transform.rotation);
                return true;
            }
            pose = default;
            return false;
        }

        public void Release(ulong entityId)
        {
            foreach (SpawnPoint point in points) if (point.occupant == entityId) point.occupant = null;
        }

        public bool IsSafe(SpawnPoint point)
        {
            if (point == null || point.transform == null || point.occupant.HasValue) return false;
            foreach (SpawnPoint other in points)
                if (other != point && other.occupant.HasValue && Vector3.Distance(other.transform.position, point.transform.position) < minimumSpawnDistance) return false;

            Vector3 bottom = point.transform.position + Vector3.up * collisionRadius;
            Vector3 top = point.transform.position + Vector3.up * Mathf.Max(collisionRadius, collisionHeight - collisionRadius);
            return !Physics.CheckCapsule(bottom, top, collisionRadius, blockingLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
