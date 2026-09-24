using Robot.Input;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace Robot.Player.CameraControl
{
    /// <summary>
    /// Cinemachine 3 third-person orbital camera controller with smooth mouse orbit controls.
    /// OrbitalFollow positions the camera; player input alone owns its orientation.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CinemachineCamera), typeof(CinemachineOrbitalFollow))]
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("Target & Offset")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);
        [SerializeField, Min(1f)] private float distance = 4.5f;

        [Header("Default Angles")]
        [SerializeField, Range(-180f, 180f)] private float defaultYaw = 0f;
        [SerializeField, Range(-80f, 80f)] private float defaultPitch = 15f;
        [SerializeField, Range(1f, 20f)] private float defaultDistance = 4.5f;

        [Header("Orbit Sensitivity")]
        [SerializeField, Range(0.05f, 5f)] private float mouseSensitivityX = 1.2f;
        [SerializeField, Range(0.05f, 5f)] private float mouseSensitivityY = 1.2f;
        [SerializeField] private bool invertY = false;

        [Header("Cursor Control")]
        [SerializeField] private bool lockCursor = true;
        [SerializeField] private bool requireRightClickToOrbit = false;

        [Header("Pitch Angle Limits")]
        [SerializeField] private float minPitch = -30.0f;
        [SerializeField] private float maxPitch = 70.0f;

        [Header("Input Reference")]
        [SerializeField] private PlayerInputReader inputReader;

        private CinemachineCamera virtualCamera;
        private CinemachineOrbitalFollow orbitalFollow;
        private CinemachineHardLookAt hardLookAt;
        private CinemachineDeoccluder deoccluder;

        private float yaw;
        private float pitch;
        private bool isCursorLocked = true;
        private bool uiOwnsCursor;
        private bool uiBlocksInput;
        private float sensitivityMultiplier = 1f;

        public float Yaw => yaw;
        public float Pitch => pitch;
        public Vector3 CameraForward => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        public Vector3 CameraRight => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        public event System.Action LookPerformed;
        public void SetSensitivityMultiplier(float value) => sensitivityMultiplier = Mathf.Clamp(value, 0.2f, 3f);
        public void SetUIInputBlocked(bool value)
        {
            uiOwnsCursor = true;
            uiBlocksInput = value;
            isCursorLocked = !value;
        }
        public void ReleaseUICursorOwnership() { uiOwnsCursor = false; uiBlocksInput = false; }

        private void Awake()
        {
            virtualCamera = GetComponent<CinemachineCamera>();
            orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
            hardLookAt = GetComponent<CinemachineHardLookAt>();
            deoccluder = GetComponent<CinemachineDeoccluder>();

            distance = defaultDistance;
            pitch = Mathf.Clamp(defaultPitch, minPitch, maxPitch);
            yaw = defaultYaw;

            ConfigurePipelineComponents();
        }

        private void Start()
        {
            EnsureInputReader();
            AssignTarget();
            ConfigurePipelineComponents();
            ApplyOrbit();
        }

        private void Update()
        {
            EnsureInputReader();
            HandleCameraInput();
            ApplyCursorLock();
        }

        private void LateUpdate()
        {
            AssignTarget();
            ApplyOrbit();
        }

        public void SetTarget(Transform value)
        {
            target = ResolveMovementRoot(value);
            EnsureInputReader();
            AssignTarget();
            ConfigurePipelineComponents();
            ApplyOrbit();
        }

        public void SnapToRoundStart(Transform player)
        {
            if (player == null) return;
            target = ResolveMovementRoot(player);
            AssignTarget();
            distance = defaultDistance;
            pitch = Mathf.Clamp(defaultPitch, minPitch, maxPitch);
            yaw = Mathf.DeltaAngle(0, target.eulerAngles.y);
            inputReader?.ClearBufferedInput();
            ApplyOrbit();
            if (virtualCamera != null)
            {
                virtualCamera.PreviousStateIsValid = false;
                virtualCamera.InternalUpdateCameraState(Vector3.up, -1f);
                var state = virtualCamera.State;
                if (Camera.main != null)
                    Camera.main.transform.SetPositionAndRotation(state.GetFinalPosition(), state.GetFinalOrientation());
            }
        }

        private void EnsureInputReader()
        {
            if (inputReader != null) return;
            if (target != null)
            {
                inputReader = target.GetComponentInParent<PlayerInputReader>();
            }
            if (inputReader == null)
            {
                inputReader = Object.FindFirstObjectByType<PlayerInputReader>();
            }
        }

        private void ApplyCursorLock()
        {
            if (uiOwnsCursor) return;
            if (!lockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (requireRightClickToOrbit)
            {
                if (UnityEngine.Input.GetMouseButton(1))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            else
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    isCursorLocked = false;
                }
                else if (UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetMouseButtonDown(1))
                {
                    isCursorLocked = true;
                }

                if (isCursorLocked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private void HandleCameraInput()
        {
            if (inputReader == null || uiBlocksInput) return;
            if (lockCursor && !requireRightClickToOrbit && !isCursorLocked) return;
            if (requireRightClickToOrbit && !UnityEngine.Input.GetMouseButton(1)) return;

            Vector2 lookInput = inputReader.LookInput;
            if (lookInput.sqrMagnitude > 0.0001f)
            {
                LookPerformed?.Invoke();
                yaw += lookInput.x * mouseSensitivityX * sensitivityMultiplier;
                if (yaw > 180f) yaw -= 360f;
                else if (yaw < -180f) yaw += 360f;

                float pitchDelta = (invertY ? lookInput.y : -lookInput.y) * mouseSensitivityY * sensitivityMultiplier;
                pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
            }
        }

        private static Transform ResolveMovementRoot(Transform value)
        {
            if (value == null) return null;
            var movement = value.GetComponentInParent<Robot.Player.Movement.RobotMovementController>();
            if (movement != null) return movement.transform;
            var controller = value.GetComponentInParent<CharacterController>();
            return controller != null ? controller.transform : value;
        }

        private void AssignTarget()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }

            // Serialized targets and legacy callers can also supply a bone or
            // SolidRobotHull. Follow locomotion, never animated collision bounds.
            target = ResolveMovementRoot(target);

            if (virtualCamera != null && target != null)
            {
                if (virtualCamera.Follow != target) virtualCamera.Follow = target;
                if (virtualCamera.LookAt != target) virtualCamera.LookAt = target;
            }
        }

        private void ConfigurePipelineComponents()
        {
            if (virtualCamera == null) virtualCamera = GetComponent<CinemachineCamera>();
            if (virtualCamera != null) virtualCamera.enabled = true;

            if (orbitalFollow == null) orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow == null) orbitalFollow = gameObject.AddComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow != null) orbitalFollow.enabled = true;

            // A damped camera looking directly at the undamped character turns on
            // every movement/ground correction, even with no look input.
            if (hardLookAt == null) hardLookAt = GetComponent<CinemachineHardLookAt>();
            if (hardLookAt != null) hardLookAt.enabled = false;

            // Disabled: lateral push from obstacles/ground causes severe camera jitter
            if (deoccluder == null) deoccluder = GetComponent<CinemachineDeoccluder>();
            if (deoccluder != null)
            {
                deoccluder.enabled = false;
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var brain = mainCam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
                if (brain != null)
                {
                    brain.enabled = true;
                    brain.UpdateMethod = Unity.Cinemachine.CinemachineBrain.UpdateMethods.LateUpdate;
                    brain.BlendUpdateMethod = Unity.Cinemachine.CinemachineBrain.BrainUpdateMethods.LateUpdate;
                }
            }
        }

        private void ApplyOrbit()
        {
            if (orbitalFollow == null) return;

            // With no Aim component Cinemachine uses the virtual camera rotation.
            // This is the same rotation used by its spherical orbit, so follow
            // damping cannot feed back into yaw/pitch.
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbitalFollow.TargetOffset = targetOffset;
            orbitalFollow.Radius = distance;
            orbitalFollow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            // CharacterController already moves once per rendered frame. Keep a
            // constant relative offset. OrbitalFollow damps in camera-local axes:
            // even Y-only damping couples pitch into world-space X/Z lag.
            orbitalFollow.TrackerSettings.PositionDamping = Vector3.zero;
            orbitalFollow.TrackerSettings.RotationDamping = Vector3.zero;

            orbitalFollow.HorizontalAxis.Value = yaw;
            orbitalFollow.HorizontalAxis.Wrap = true;
            orbitalFollow.HorizontalAxis.Range = new Vector2(-180f, 180f);
            orbitalFollow.HorizontalAxis.Recentering.Enabled = false;

            orbitalFollow.VerticalAxis.Value = pitch;
            orbitalFollow.VerticalAxis.Wrap = false;
            orbitalFollow.VerticalAxis.Range = new Vector2(minPitch, maxPitch);
            orbitalFollow.VerticalAxis.Recentering.Enabled = false;

            orbitalFollow.RadialAxis.Value = 1f;
            orbitalFollow.RadialAxis.Recentering.Enabled = false;
        }
    }
}
