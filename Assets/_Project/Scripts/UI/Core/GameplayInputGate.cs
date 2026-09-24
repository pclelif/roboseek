using UnityEngine;
using Robot.Input;
using Robot.Combat;
using Robot.ObjectHunt;
using Robot.Player.Movement;
namespace Robot.UI.Production
{
    public sealed class GameplayInputGate : MonoBehaviour
    {
        public RobotMovementController movement;
        public PlayerCombatInput combatInput;
        public PlayerInputReader input;
        public ObjectHuntRoundManager hunt;
        private bool captured, movementEnabled, combatEnabled;
        public void SetBlocked(bool blocked)
        {
            if (blocked && !captured)
            {
                captured = true; movementEnabled = movement != null && movement.enabled; combatEnabled = combatInput != null && combatInput.enabled;
                // Component gate cannot be overridden by an in-flight pickup/attack's SetControlEnabled(true).
                if (movement != null) movement.enabled = false; if (combatInput != null) combatInput.enabled = false;
            }
            else if (!blocked && captured)
            {
                if (movement != null) movement.enabled = movementEnabled;
                if (combatInput != null) combatInput.enabled = combatEnabled;
                captured = false;
            }
            if (input != null) input.SetPaused(blocked);
            if (hunt != null) hunt.SetInteractionEnabled(!blocked);
        }
        public void GroundForRoundIntro()
        {
            if (movement == null) return;
            var controller = movement.GetComponent<CharacterController>();
            if (controller == null || !controller.enabled) return;
            // Sweep the player's own capsule down to the nearest floor, preserving
            // its horizontal spawn and collision clearance even while time is paused.
            Physics.SyncTransforms();
            controller.Move(Vector3.down * 50f);
            movement.ResetGroundedMotion();
        }
        public void ResetTransientInput()
        {
            if (combatInput != null)
            {
                combatInput.StopAllCoroutines();
                var attack = combatInput.GetComponent<CombatAttack>();
                if (attack != null) attack.CancelPendingAttack();
            }
            if (movement != null)
            {
                var health = movement.GetComponent<CombatHealth>();
                movement.SetControlEnabled(health == null || !health.IsKnockedOut);
            }
            if (input != null) input.ClearBufferedInput();
        }
        private void OnDisable() { if (captured) SetBlocked(false); }
    }
}
