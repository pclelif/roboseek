using UnityEngine;

namespace Robot.NPC
{
    [RequireComponent(typeof(Collider))]
    public sealed class AggressionZone : MonoBehaviour
    {
        private Collider zoneCollider;

        private void Awake() => zoneCollider = GetComponent<Collider>();

        public bool Contains(Vector3 worldPosition)
        {
            if (zoneCollider == null) zoneCollider = GetComponent<Collider>();
            return zoneCollider != null && zoneCollider.enabled &&
                   zoneCollider.ClosestPoint(worldPosition) == worldPosition;
        }

        private void Reset()
        {
            Collider value = GetComponent<Collider>();
            value.isTrigger = true;
        }
    }
}
