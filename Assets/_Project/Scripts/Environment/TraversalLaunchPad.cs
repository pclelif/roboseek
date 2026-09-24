using UnityEngine;
using Robot.Player.Movement;

namespace Robot.Environment
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TraversalLaunchPad : MonoBehaviour
    {
        public Vector3 launchVelocity = new Vector3(0, 17, 0);
        public Vector3 landingPosition;
        private float readyAt;
        private void Awake()
        {
            // The authored trigger and solid walking surface are separate colliders.
            if (GetComponents<BoxCollider>().Length == 1) GetComponent<BoxCollider>().isTrigger = true;
        }
        private void OnTriggerEnter(Collider other) => TryLaunch(other.GetComponentInParent<RobotMovementController>());
        public bool TryLaunch(RobotMovementController movement)
        {
            if (movement == null || Time.time < readyAt || !movement.CanUseTraversal) return false;
            // Rise clear of the ledge first, then cross onto the landing deck.
            // Calculate from the actual entry position, not the centre of the trigger.
            float gravity = movement.GravityMagnitude;
            float height = landingPosition.y - movement.transform.position.y;
            float vertical = Mathf.Sqrt(2 * gravity * Mathf.Max(2.5f, height + 2.5f));
            float clearHeight = Mathf.Max(0, height + .4f);
            float delay = (vertical - Mathf.Sqrt(Mathf.Max(0, vertical * vertical - 2 * gravity * clearHeight))) / gravity;
            float flightTime = (vertical + Mathf.Sqrt(Mathf.Max(0, vertical * vertical - 2 * gravity * height))) / gravity;
            float duration = Mathf.Max(.2f, flightTime - delay - .08f);
            Vector3 horizontal = landingPosition - movement.transform.position; horizontal.y = 0;
            if (!movement.Launch(horizontal / duration + Vector3.up * vertical, duration, delay)) return false;
            readyAt = Time.time + .75f;
            Robot.Audio.AudioManager.Instance?.PlayJump(movement.transform.position);
            return true;
        }
    }
}
