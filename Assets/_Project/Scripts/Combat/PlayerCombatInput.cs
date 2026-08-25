using System.Collections;
using UnityEngine;
using Robot.Input;
using Robot.Player.Movement;

namespace Robot.Combat
{
    [RequireComponent(typeof(CombatAttack), typeof(PlayerInputReader), typeof(RobotMovementController))]
    public sealed class PlayerCombatInput : MonoBehaviour
    {
        private CombatAttack attack;
        private PlayerInputReader input;
        private RobotMovementController movement;
        private CombatHealth health;

        [SerializeField, Min(0f)] private float movementLockDuration = 0.7f;

        private void Awake()
        {
            attack = GetComponent<CombatAttack>();
            input = GetComponent<PlayerInputReader>();
            movement = GetComponent<RobotMovementController>();
            health = GetComponent<CombatHealth>();
        }

        private void Update()
        {
            if (input.ConsumeAttackPressed() && attack.TryAttack())
                StartCoroutine(AttackMovementLock());
        }

        private IEnumerator AttackMovementLock()
        {
            movement.SetControlEnabled(false);
            yield return new WaitForSeconds(movementLockDuration);
            if (health != null && !health.IsKnockedOut) movement.SetControlEnabled(true);
        }
    }
}
