using UnityEngine;
using Robot.Input;

namespace Robot.Player.Movement
{
    /// <summary>
    /// Handles camera-relative movement and character rotation facing the movement direction.
    /// Ensures cameraTransform ALWAYS points to the view camera, never to the player transform itself.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    public sealed class RobotMovementController : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;

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

        public bool IsGrounded { get; private set; } = true;
        public float CurrentSpeedNormalized { get; private set; }
        public float VerticalVelocity => verticalVelocity;
        public bool ControlEnabled => controlEnabled;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            lastProxyPosition = transform.position;
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

        public void SetCameraTransform(Transform value)
        {
            if (value != null && value != transform && !value.IsChildOf(transform))
            {
                cameraTransform = value;
            }
        }

        public void SetControlEnabled(bool value)
        {
            controlEnabled = value;
            if (!value)
            {
                currentSpeed = 0f;
                CurrentSpeedNormalized = 0f;
            }
        }

        private void EnsureCameraReference()
        {
            // If cameraTransform is missing or accidentally linked to the player itself, override to Main Camera
            if (cameraTransform == null || cameraTransform == transform || cameraTransform.IsChildOf(transform))
            {
                if (Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }
            }
        }

        private void UpdateVerticalVelocity()
        {
            float gravityMagnitude = gravity != 0f ? Mathf.Abs(gravity) : 20.0f;
            float downwardGravity = -gravityMagnitude;

            IsGrounded = controller != null && controller.isGrounded;
            if (IsGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedVerticalVelocity;
            }

            if (IsGrounded && input != null && input.ConsumeJumpPressed())
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravityMagnitude);
                IsGrounded = false;
            }

            verticalVelocity += downwardGravity * Time.deltaTime;
            verticalVelocity = Mathf.Clamp(verticalVelocity, -50.0f, 30.0f);
        }

        private void Move()
        {
            Vector2 moveInput = input.MoveInput;

            Vector3 cameraForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 cameraRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
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

            float targetSpeed = (input.RunHeld ? runSpeed : walkSpeed) * inputMagnitude;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

            Vector3 velocity = moveDirection * currentSpeed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);

            CurrentSpeedNormalized = runSpeed > 0f ? currentSpeed / runSpeed : 0f;
        }
    }
}
