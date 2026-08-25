using System.Collections;
using UnityEngine;
using Robot.Combat;
using Robot.Player;
using Robot.Player.Movement;

namespace Robot.ObjectHunt
{
    public sealed class CollectibleTarget : MonoBehaviour
    {
        private ObjectHuntRoundManager manager;
        private TargetDefinition definition;
        private Vector3 basePosition;
        private bool collecting;

        public TargetDefinition Definition => definition;
        public bool IsCollecting => collecting;

        public void Configure(ObjectHuntRoundManager owner, TargetDefinition target)
        {
            manager = owner;
            definition = target;
            basePosition = transform.position;
            foreach (Rigidbody body in GetComponentsInChildren<Rigidbody>())
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        private void Update()
        {
            if (collecting) return;
            transform.position = basePosition + Vector3.up * (0.12f + Mathf.Sin(Time.time * 2.2f) * 0.08f);
            transform.Rotate(0f, 22f * Time.deltaTime, 0f, Space.World);
        }

        public bool TryCollect(Transform collector, Transform pickupTarget)
        {
            if (collecting || definition == null || collector == null) return false;
            collecting = true;
            StartCoroutine(CollectRoutine(collector, pickupTarget));
            return true;
        }

        private IEnumerator CollectRoutine(Transform collector, Transform pickupTarget)
        {
            RobotMovementController movement = collector.GetComponent<RobotMovementController>();
            CombatHealth health = collector.GetComponent<CombatHealth>();
            RobotAnimator animator = collector.GetComponent<RobotAnimator>();
            movement?.SetControlEnabled(false);

            Vector3 flatDirection = transform.position - collector.position;
            flatDirection.y = 0f;
            Quaternion startRotation = collector.rotation;
            Quaternion targetRotation = flatDirection.sqrMagnitude > 0.001f ? Quaternion.LookRotation(flatDirection) : startRotation;
            float turnTime = 0f;
            while (turnTime < 0.25f)
            {
                turnTime += Time.deltaTime;
                collector.rotation = Quaternion.Slerp(startRotation, targetRotation, Mathf.Clamp01(turnTime / 0.25f));
                yield return null;
            }

            animator?.PlayPickupGesture();
            Vector3 startPosition = transform.position;
            Vector3 startScale = transform.localScale;
            float travelTime = 0f;
            const float duration = 0.38f;
            while (travelTime < duration)
            {
                travelTime += Time.deltaTime;
                float t = Mathf.Clamp01(travelTime / duration);
                Vector3 destination = pickupTarget != null ? pickupTarget.position : collector.position + Vector3.up;
                transform.position = Vector3.Lerp(startPosition, destination, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.45f);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            manager.NotifyCollected(this);
            gameObject.SetActive(false);
            if (movement != null && (health == null || !health.IsKnockedOut)) movement.SetControlEnabled(true);
        }
    }
}
