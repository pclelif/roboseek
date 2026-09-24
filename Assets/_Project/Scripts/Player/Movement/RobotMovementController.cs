using UnityEngine;
using Robot.Input;
using Robot.Core;

namespace Robot.Player.Movement
{
    /// <summary>
    /// Handles orbit-relative movement and character rotation facing the movement direction.
    /// Uses the view camera as a fallback when no orbital controller is available.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    public sealed class RobotMovementController : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        private Robot.Player.CameraControl.ThirdPersonCameraController cameraController;

        [Header("Locomotion Speed")]
        [SerializeField, Min(0f)] private float walkSpeed = 3.5f;
        [SerializeField, Min(0f)] private float runSpeed = 6.0f;
        [SerializeField, Min(0f)] private float acceleration = 20.0f;
        [SerializeField, Min(0f)] private float rotationSpeed = 720.0f;

        [Header("Model Facing Offset")]
        [SerializeField] private float modelFacingOffsetDegrees = 0f;

        [Header("Gravity and Jump")]
        [SerializeField] private float gravity = -20.0f;
        [SerializeField] private float groundedVerticalVelocity = -2.0f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;

        private CharacterController controller;
        private PlayerInputReader input;
        private float verticalVelocity;
        private float currentSpeed;
        private bool controlEnabled = true;

        private Vector3 lastProxyPosition;
        private MapTraversal traversal;
        private RobotJetpackController jetpack;
        private int airJumpsRemaining;
        private float dashRemaining, dashCooldown, launchRemaining, launchDelay;
        private Vector3 dashDirection, launchVelocity;
        private const float DashDuration = .18f, DashCooldown = 1.15f;
        public MapTraversal Traversal => traversal;
        public float GravityMagnitude => Mathf.Abs(gravity) > .01f ? Mathf.Abs(gravity) : 20f;
        public bool IsDashing => dashRemaining > 0;
        public float DashReadyNormalized => 1f - Mathf.Clamp01(dashCooldown / DashCooldown);
        public int AirJumpsRemaining => airJumpsRemaining;
        public bool CanUseTraversal => enabled && controlEnabled && controller != null && controller.enabled && Time.timeScale > 0f;

        public void ConfigureTraversal(MapTraversal value)
        {
            traversal = value;
            jetpack = GetComponent<RobotJetpackController>();
            if (value == MapTraversal.Jetpack && jetpack == null) jetpack = gameObject.AddComponent<RobotJetpackController>();
            if (jetpack != null) jetpack.enabled = value == MapTraversal.Jetpack;
            ResetTraversal();
        }

        private void ResetTraversal()
        {
            airJumpsRemaining = traversal == MapTraversal.DoubleJump ? 1 : 0;
            dashRemaining = dashCooldown = launchRemaining = launchDelay = 0;
            launchVelocity = Vector3.zero;
            jetpack?.Refuel();
        }

        public bool Launch(Vector3 velocity, float horizontalDuration = 1.1f, float horizontalDelay = 0f)
        {
            if (!CanUseTraversal) return false;
            verticalVelocity = Mathf.Max(0, velocity.y);
            launchVelocity = new Vector3(velocity.x, 0, velocity.z);
            launchRemaining = Mathf.Max(0, horizontalDuration);
            launchDelay = Mathf.Max(0, horizontalDelay);
            dashRemaining = 0;
            IsGrounded = false;
            airJumpsRemaining = traversal == MapTraversal.DoubleJump ? 1 : 0;
            return true;
        }

        public bool IsGrounded { get; private set; } = true;
        public float CurrentSpeedNormalized { get; private set; }
        public float VerticalVelocity => verticalVelocity;
        public bool ControlEnabled => controlEnabled;

        private void Awake()
        {
            if (transform.parent != null && (transform.parent.name == "RobotShowcasePlatform" || transform.name.Contains("Showcase")))
            {
                var c = GetComponent<CharacterController>();
                if (c != null) c.enabled = false;
                enabled = false;
                return;
            }
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            lastProxyPosition = transform.position;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            if (controller != null)
            {
                controller.detectCollisions = true;
                controller.enableOverlapRecovery = true;
                controller.stepOffset = 0.3f;
                controller.skinWidth = 0.035f;
                controller.minMoveDistance = 0f;
            }
            if (GetComponent<SolidRobotBody>() == null) gameObject.AddComponent<SolidRobotBody>();
        }

        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

        public void SetSpawnPoint(Vector3 pos, Quaternion rot)
        {
            spawnPosition = pos;
            spawnRotation = rot;
        }

        public void ReturnToSpawn()
        {
            bool enabledBefore = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            controller.enabled = enabledBefore;
            GetComponent<SolidRobotBody>()?.RefreshHull();
            Physics.SyncTransforms();
            ResetGroundedMotion();
        }

        private void Start()
        {
            lastProxyPosition = transform.position;
            EnsureCameraReference();
        }

        private void Update()
        {
            if (!controlEnabled)
            {
                UpdateProxyLocomotion();
                return;
            }
            EnsureCameraReference();
            UpdateVerticalVelocity();
            Move();
            lastProxyPosition = transform.position;
        }

        private void UpdateProxyLocomotion()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 displacement = transform.position - lastProxyPosition;
            lastProxyPosition = transform.position;

            Vector3 velocity = displacement / dt;
            float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;

            float targetNormalized = runSpeed > 0.01f ? Mathf.Clamp01(horizontalSpeed / runSpeed) : 0f;
            CurrentSpeedNormalized = Mathf.MoveTowards(CurrentSpeedNormalized, targetNormalized, acceleration * dt);
            verticalVelocity = velocity.y;

            // Ground raycast check to ensure remote robot doesn't get stuck in Jump_Air state
            bool hitGround = Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 0.85f, ~0, QueryTriggerInteraction.Ignore);
            if (hitGround)
            {
                IsGrounded = (transform.position.y - hit.point.y) < 0.35f || Mathf.Abs(verticalVelocity) < 1.0f;
            }
            else
            {
                IsGrounded = Mathf.Abs(verticalVelocity) < 0.25f;
            }
        }

        public void ResetGroundedMotion()
        {
            verticalVelocity = groundedVerticalVelocity;
            ResetTraversal();
            currentSpeed = 0f;
            CurrentSpeedNormalized = 0f;
            IsGrounded = controller != null && controller.isGrounded;
            lastProxyPosition = transform.position;
        }

        public void SetCameraTransform(Transform value)
        {
            if (value != null && value != transform && !value.IsChildOf(transform))
            {
                cameraTransform = value;
                cameraController = null;
            }
        }

        public void SetCameraController(Robot.Player.CameraControl.ThirdPersonCameraController tpc)
        {
            cameraController = tpc;
            if (tpc != null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        public void SetControlEnabled(bool value)
        {
            controlEnabled = value;
            if (!value)
            {
                dashRemaining = launchRemaining = launchDelay = 0;
                jetpack?.StopThrust();
                currentSpeed = 0f;
                CurrentSpeedNormalized = 0f;
            }
        }

        private void EnsureCameraReference()
        {
            if (cameraController == null)
                cameraController = Object.FindFirstObjectByType<Robot.Player.CameraControl.ThirdPersonCameraController>();
            // If cameraTransform is missing or accidentally linked to the player itself, override to Main Camera
            if (cameraTransform == null || cameraTransform == transform || cameraTransform.IsChildOf(transform))
            {
                if (Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }
            }
        }

        private bool wasGroundedLastFrame = true;
        private float footstepTimer;

        private void UpdateVerticalVelocity()
        {
            float gravityMagnitude = gravity != 0f ? Mathf.Abs(gravity) : 20.0f;
            float downwardGravity = -gravityMagnitude;

            IsGrounded = controller != null && controller.isGrounded;
            if (!wasGroundedLastFrame && IsGrounded) Robot.Audio.AudioManager.Instance?.PlayLand(transform.position);

            if (IsGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedVerticalVelocity;
            }

            if (IsGrounded && verticalVelocity <= 0f)
                airJumpsRemaining = traversal == MapTraversal.DoubleJump ? 1 : 0;

            bool jumpPressed = input != null && input.ConsumeJumpPressed();
            if (jumpPressed && (IsGrounded || airJumpsRemaining > 0))
            {
                if (!IsGrounded) airJumpsRemaining--;
                float height = traversal == MapTraversal.DoubleJump ? 1.65f : jumpHeight;
                verticalVelocity = Mathf.Sqrt(height * 2f * gravityMagnitude);
                IsGrounded = false;
                Robot.Audio.AudioManager.Instance?.PlayJump(transform.position);
            }

            wasGroundedLastFrame = IsGrounded;
            verticalVelocity += downwardGravity * Time.deltaTime;
            if (jetpack != null && jetpack.isActiveAndEnabled)
                jetpack.Simulate(Time.deltaTime, IsGrounded, input != null && input.JumpHeld, ref verticalVelocity);
            verticalVelocity = Mathf.Clamp(verticalVelocity, -50.0f, 30.0f);
        }

        private void Move()
        {
            Vector2 moveInput = input.MoveInput;

            // Movement follows orbit intent, never the previous rendered frame's
            // damped/obstacle-adjusted camera orientation.
            bool useOrbit = cameraController != null && cameraController.isActiveAndEnabled;
            Vector3 cameraForward = useOrbit ? cameraController.CameraForward :
                cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 cameraRight = useOrbit ? cameraController.CameraRight :
                cameraTransform != null ? cameraTransform.right : Vector3.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
            float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                moveDirection.Normalize();

                // Rotate robot to face the screen/camera movement direction
                Quaternion offsetRot = Quaternion.Euler(0f, modelFacingOffsetDegrees, 0f);
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up) * offsetRot;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            float dt = Time.deltaTime;
            dashCooldown = Mathf.Max(0, dashCooldown - dt);
            if (traversal == MapTraversal.Dash && input.ConsumeDashPressed() && dashCooldown <= 0)
            {
                dashDirection = moveDirection.sqrMagnitude > .001f ? moveDirection.normalized : cameraForward;
                dashRemaining = DashDuration;
                dashCooldown = DashCooldown;
            }
            float speedMultiplier = Time.time < boostUntil ? boostMultiplier : 1f;
            float targetSpeed = (input.RunHeld ? (traversal == MapTraversal.DoubleJump ? 9f : runSpeed) : walkSpeed) * inputMagnitude * speedMultiplier;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

            Vector3 velocity = moveDirection * currentSpeed + Vector3.up * verticalVelocity;
            if (dashRemaining > 0)
            {
                // Integrate only the remaining dash duration, even on a long frame.
                float dashStep = Mathf.Min(dashRemaining, dt);
                Vector3 dashVelocity = dashDirection * 18f;
                velocity.x = Mathf.Lerp(velocity.x, dashVelocity.x, dashStep / Mathf.Max(dt, .0001f));
                velocity.z = Mathf.Lerp(velocity.z, dashVelocity.z, dashStep / Mathf.Max(dt, .0001f));
                dashRemaining = Mathf.Max(0, dashRemaining - dt);
            }
            float launchStep = dt;
            if (launchDelay > 0)
            {
                float delayStep = Mathf.Min(launchDelay, launchStep);
                launchDelay -= delayStep;
                launchStep -= delayStep;
            }
            if (launchRemaining > 0 && launchStep > 0)
            {
                launchStep = Mathf.Min(launchRemaining, launchStep);
                velocity += launchVelocity * (launchStep / Mathf.Max(dt, .0001f));
                launchRemaining -= launchStep;
            }
            var flags = SolidRobotBody.Move(controller, velocity * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
            IsGrounded = (flags & CollisionFlags.Below) != 0;

            CurrentSpeedNormalized = runSpeed > 0f ? currentSpeed / runSpeed : 0f;
            if (IsGrounded && currentSpeed > 0.5f)
            {
                footstepTimer += Time.deltaTime * currentSpeed;
                if (footstepTimer >= (input.RunHeld ? 1.90f : 1.45f))
                {
                    footstepTimer = 0f;
                    string surface = "Grass";
                    if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 0.8f)) surface = hit.collider.tag;
                    Robot.Audio.AudioManager.Instance?.PlayFootstep(surface, transform.position);
                }
            }
            else footstepTimer = 0f;
        }

        private void OnDisable()
        {
            dashRemaining = launchRemaining = launchDelay = 0;
            jetpack?.StopThrust();
        }

        private float boostMultiplier = 1.4f;
        private float boostUntil;
        public void ApplySpeedBoost(float multiplier, float duration)
        {
            boostMultiplier = multiplier;
            boostUntil = Time.time + duration;
        }
    }
}
