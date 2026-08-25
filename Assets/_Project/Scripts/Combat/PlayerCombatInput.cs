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
        private Robot.Multiplayer.NetworkRobotPlayer networkPlayer;

        [SerializeField, Min(0f)] private float movementLockDuration = 0.7f;

        private void Awake()
        {
            attack = GetComponent<CombatAttack>();
            input = GetComponent<PlayerInputReader>();
            movement = GetComponent<RobotMovementController>();
            health = GetComponent<CombatHealth>();
            networkPlayer = GetComponent<Robot.Multiplayer.NetworkRobotPlayer>();
        }

        private void Update()
        {
            if (input.ConsumeAttackPressed() && attack.TryAttack())
            {
                if (networkPlayer != null && networkPlayer.IsOwner)
                {
                    networkPlayer.TriggerAttackServerRpc();
                }
                StartCoroutine(AttackMovementLock());
            }
        }

        private IEnumerator AttackMovementLock()
        {
            movement.SetControlEnabled(false);
            yield return new WaitForSeconds(movementLockDuration);
            if (health != null && !health.IsKnockedOut) movement.SetControlEnabled(true);
        }
    }
}
