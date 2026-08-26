using System.Collections;
using UnityEngine;
using Robot.Player.Movement;
using Robot.Combat;

namespace Robot.Player
{
    public sealed class RobotAnimator : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speedDampTime = 0.1f;
        [SerializeField] private string idleState = "StaticIdle";
        [SerializeField] private string walkState = "Walk";
        [SerializeField] private string runState = "Run";
        [SerializeField] private string jumpStartState = "Jump_Start";
        [SerializeField] private string jumpAirState = "Jump_Air";
        [SerializeField] private string jumpLandingState = "Jump_Landing";
        [SerializeField] private string deathState = "Death";
        [SerializeField] private string attackState = "BasicAttack";
        [SerializeField] private string pickupState = "BasicAttack";
        [SerializeField, Min(0.05f)] private float pickupGestureDuration = 0.45f;

        private RobotMovementController movement;
        private Animator animator;
        private CombatHealth health;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

        private int activeState;
        private bool hasSpeedParameter;
        private bool hasGroundedParameter;
        private bool isShutdown = false;
        private bool wasGrounded = true;
        private bool isLanding = false;
        private bool isAttacking;
        private float externalSpeed;

        private void Awake()
        {
            movement = GetComponent<RobotMovementController>();
            health = GetComponent<CombatHealth>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            if (animator != null)
            {
                animator.applyRootMotion = false;
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                {
                    hasSpeedParameter |= parameter.nameHash == Speed;
                    hasGroundedParameter |= parameter.nameHash == IsGroundedHash;
                }
            }
        }

        private void OnEnable()
        {
            if (health == null) health = GetComponent<CombatHealth>();
            if (health != null)
            {
                health.KnockedOut += OnKnockedOut;
                health.Recovered += OnRecovered;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.KnockedOut -= OnKnockedOut;
                health.Recovered -= OnRecovered;
            }
        }

        private void Update()
        {
            if (animator == null) return;

            if (isShutdown) return;
            if (isAttacking) return;

            if (movement == null)
            {
                PlayState(externalSpeed <= 0.01f ? idleState : externalSpeed < 0.8f ? walkState : runState);
                return;
            }

            if (hasSpeedParameter) animator.SetFloat(Speed, movement.CurrentSpeedNormalized, speedDampTime, Time.deltaTime);
            if (hasGroundedParameter) animator.SetBool(IsGroundedHash, movement.IsGrounded);

            string targetState = idleState;

            if (!movement.IsGrounded)
            {
                if (movement.VerticalVelocity > 1.0f)
                {
                    targetState = jumpStartState;
                }
                else
                {
                    targetState = jumpAirState;
                }
            }
            else
            {
                if (!wasGrounded)
                {
                    StartCoroutine(PlayLandingRoutine());
                }

                if (!isLanding)
                {
                    targetState = movement.CurrentSpeedNormalized <= 0.01f ? idleState :
                        movement.CurrentSpeedNormalized < 0.8f ? walkState : runState;
                }
                else
                {
                    targetState = jumpLandingState;
                }
            }

            wasGrounded = movement.IsGrounded;

            PlayState(targetState);
        }

        private IEnumerator PlayLandingRoutine()
        {
            isLanding = true;
            PlayState(jumpLandingState, 0.1f);
            yield return new WaitForSeconds(0.25f);
            isLanding = false;
        }

        private void PlayState(string stateName, float customDampTime = -1f)
        {
            int stateHash = Animator.StringToHash(stateName);
            if (stateHash != activeState)
            {
                float damp = customDampTime >= 0f ? customDampTime : speedDampTime;
                animator.CrossFade(stateHash, damp);
                activeState = stateHash;
            }
        }

        public void TriggerShutdown()
        {
            if (animator == null) return;
            isShutdown = true;
            PlayState(deathState, 0.2f);
        }

        public void PlayAttack()
        {
            if (animator == null || isShutdown) return;
            StopCoroutine(nameof(AttackRoutine));
            StartCoroutine(nameof(AttackRoutine));
        }

        public void SetLocomotion(float normalizedSpeed) => externalSpeed = Mathf.Clamp01(normalizedSpeed);

        public void PlayPickupGesture()
        {
            if (animator == null || isShutdown) return;
            StopCoroutine(nameof(PickupRoutine));
            StartCoroutine(nameof(PickupRoutine));
        }

        private IEnumerator PickupRoutine()
        {
            isAttacking = true;
            PlayState(pickupState, 0.08f);
            yield return new WaitForSeconds(pickupGestureDuration);
            isAttacking = false;
            activeState = 0;
        }

        private IEnumerator AttackRoutine()
        {
            isAttacking = true;
            PlayState(attackState, 0.08f);
            yield return new WaitForSeconds(attackAnimationDuration);
            isAttacking = false;
            activeState = 0;
        }

        private void OnKnockedOut(CombatHealth _)
        {
            StopAllCoroutines();
            isAttacking = false;
            TriggerShutdown();
            movement?.SetControlEnabled(false);
        }

        private void OnRecovered(CombatHealth _)
        {
            isShutdown = false;
            activeState = 0;
            movement?.SetControlEnabled(true);
            PlayState(idleState, 0.2f);
        }
    }
}
