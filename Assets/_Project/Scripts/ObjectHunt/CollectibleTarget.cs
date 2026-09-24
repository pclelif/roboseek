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
        private RobotMovementController collectingMovement;

        public TargetDefinition Definition => definition;
        public bool IsCollecting => collecting;

        public void Configure(ObjectHuntRoundManager owner, TargetDefinition target)
        {
            manager = owner;
            definition = target;
            basePosition = transform.position;
            WorldSafety.AddMissingSolidColliders(gameObject);
            foreach (Rigidbody body in GetComponentsInChildren<Rigidbody>())
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                col.isTrigger = false;
                col.enabled = true;
            }
        }

        private void Update()
        {
            if (collecting) return;
            // Solid toys stay grounded; only their renderers animate on rejection.
        }

        public bool TryCollect(Transform collector, Transform pickupTarget)
        {
            if (collecting || definition == null || collector == null) return false;
            collectingMovement = collector.GetComponent<RobotMovementController>();
            collecting = true;
            CollectParticleEffect.Spawn(transform.position);
            Robot.Audio.AudioManager.Instance?.PlayTargetCorrectPickup();
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            StartCoroutine(CollectRoutine(collector, pickupTarget));
            return true;
        }

        public void Reject()
        {
            if (collecting) return;
            Robot.Audio.AudioManager.Instance?.PlayTargetWrongPickup();
            var feedback = GetComponent<WrongItemFeedback>();
            if (feedback == null) feedback = gameObject.AddComponent<WrongItemFeedback>();
            feedback.Play();
        }

        private void OnDisable()
        {
            // SetActive(false) stops this object's coroutines. Release here on completion
            // or round cancellation instead of after an unreachable coroutine yield.
            if (collectingMovement == null) return;
            var health = collectingMovement.GetComponent<CombatHealth>();
            collectingMovement.SetControlEnabled(health == null || !health.IsKnockedOut);
            collectingMovement = null;
        }

        private IEnumerator CollectRoutine(Transform collector, Transform pickupTarget)
        {
            RobotMovementController movement = collector.GetComponent<RobotMovementController>();
            RobotAnimator animator = collector.GetComponent<RobotAnimator>();
            collectingMovement = movement;

            // Step 1: Robot stops
            movement?.SetControlEnabled(false);

            // Step 2: Smoothly rotate to face the toy (approx 0.25s)
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
            collector.rotation = targetRotation;

            // Step 3: Pickup feedback gesture
            animator?.PlayPickupGesture();

            // Steps 4 & 5: Toy lifts into air, flies to PickupTarget, and shrinks to zero
            Vector3 startPosition = transform.position;
            Vector3 startScale = transform.localScale;
            float travelTime = 0f;
            const float duration = 0.42f;
            while (travelTime < duration)
            {
                travelTime += Time.deltaTime;
                float t = Mathf.Clamp01(travelTime / duration);
                Vector3 destination = pickupTarget != null ? pickupTarget.position : collector.position + Vector3.up * 1.1f;
                transform.position = Vector3.Lerp(startPosition, destination, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.45f);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            // Trigger visual particle feedback effect
            CollectParticleEffect.Spawn(transform.position);

            // Step 6: Target HUD marks this toy completed & Notify Network/Local Manager
            if (Robot.Multiplayer.NetworkRoundManager.Instance != null && Robot.Multiplayer.NetworkRoundManager.Instance.IsSpawned)
            {
                var netObj = collector.GetComponent<Unity.Netcode.NetworkObject>();
                ulong clientId = netObj != null ? netObj.OwnerClientId : 0;
                Robot.Multiplayer.NetworkRoundManager.Instance.RequestPickupTargetServerRpc(definition.objectId, clientId);
            }

            if (manager != null)
            {
                manager.NotifyCollected(this);
            }

            gameObject.SetActive(false);

        }
    }
}
