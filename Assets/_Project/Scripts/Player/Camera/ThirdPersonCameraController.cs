using Robot.Input;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace Robot.Player.CameraControl
{
    /// <summary>
    /// Cinemachine 3 third-person orbital camera controller with smooth mouse orbit controls.
    /// Manages Body (CinemachineOrbitalFollow) and Aim (CinemachineHardLookAt) pipeline stages.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera), typeof(CinemachineOrbitalFollow))]
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("Target & Offset")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.2f, 0f);

        [Header("Distance & Angles")]
        [SerializeField, Min(0.5f)] private float defaultDistance = 5.0f;
        [SerializeField] private float defaultPitch = 15.0f;
        [SerializeField] private float defaultYaw = 0.0f;

        [Header("Mouse Look Controls")]
        [SerializeField] private float mouseSensitivityX = 0.25f;
        [SerializeField] private float mouseSensitivityY = 0.25f;
        [SerializeField] private bool invertY = false;
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
        private float distance;
        private bool isCursorLocked = true;

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
            target = value;
            EnsureInputReader();
            AssignTarget();
            ConfigurePipelineComponents();
            ApplyOrbit();
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
            if (inputReader == null) return;
            if (lockCursor && !requireRightClickToOrbit && !isCursorLocked) return;
            if (requireRightClickToOrbit && !UnityEngine.Input.GetMouseButton(1)) return;

            Vector2 lookInput = inputReader.LookInput;
            if (lookInput.sqrMagnitude > 0.0001f)
            {
                yaw += lookInput.x * mouseSensitivityX;
                if (yaw > 180f) yaw -= 360f;
                else if (yaw < -180f) yaw += 360f;

                float pitchDelta = (invertY ? lookInput.y : -lookInput.y) * mouseSensitivityY;
                pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
            }
        }

        private void AssignTarget()
        {
            if (virtualCamera == null || target == null) return;

            if (virtualCamera.Follow != target) virtualCamera.Follow = target;
            if (virtualCamera.LookAt != target) virtualCamera.LookAt = target;
        }

        private void ConfigurePipelineComponents()
        {
            if (orbitalFollow == null) orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow == null) orbitalFollow = gameObject.AddComponent<CinemachineOrbitalFollow>();

            if (hardLookAt == null) hardLookAt = GetComponent<CinemachineHardLookAt>();
            if (hardLookAt == null) hardLookAt = gameObject.AddComponent<CinemachineHardLookAt>();

            if (deoccluder == null) deoccluder = GetComponent<CinemachineDeoccluder>();
            if (deoccluder == null) deoccluder = gameObject.AddComponent<CinemachineDeoccluder>();

            orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbitalFollow.TargetOffset = targetOffset;
            orbitalFollow.TrackerSettings.BindingMode = BindingMode.WorldSpace;

            hardLookAt.LookAtOffset = targetOffset;

            deoccluder.IgnoreTag = "Player";
            deoccluder.MinimumDistanceFromTarget = 0.8f;
        }

        private void ApplyOrbit()
        {
            if (orbitalFollow == null) return;

            orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbitalFollow.TargetOffset = targetOffset;
            orbitalFollow.Radius = distance;
            orbitalFollow.TrackerSettings.BindingMode = BindingMode.WorldSpace;

            orbitalFollow.HorizontalAxis.Value = yaw;
            orbitalFollow.HorizontalAxis.Wrap = true;
            orbitalFollow.HorizontalAxis.Range = new Vector2(-180f, 180f);
            orbitalFollow.HorizontalAxis.Recentering.Enabled = false;

            orbitalFollow.VerticalAxis.Value = pitch;
            orbitalFollow.VerticalAxis.Wrap = false;
            orbitalFollow.VerticalAxis.Range = new Vector2(minPitch, maxPitch);
            orbitalFollow.VerticalAxis.Recentering.Enabled = false;

            if (hardLookAt != null)
            {
                hardLookAt.LookAtOffset = targetOffset;
            }
        }
    }
}
