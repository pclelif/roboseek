using UnityEngine;
using UnityEngine.InputSystem;

namespace Robot.Input
{
    /// <summary>Owns Input System access and exposes device-independent player intent with guaranteed zero-reset when keys are released.</summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        private const string PlayerMapName = "Player";
        private const string UiMapName = "UI";

        private InputActionMap playerMap;
        private InputActionMap uiMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction runAction;
        private InputAction zoomAction;
        private InputAction jumpAction;
        private InputAction interactAction;
        private InputAction pauseAction;
        private InputAction attackAction;

        private Vector2 moveInput;
        private Vector2 lookInput;
        private Vector2 mobileMoveInput;
        private Vector2 mobileLookInput;
        private float zoomInput;
        private bool jumpPressed;
        private bool interactPressed;
        private bool pausePressed;
        private bool attackPressed;
        private bool isListening;

        public Vector2 MoveInput => Vector2.ClampMagnitude(moveInput + mobileMoveInput, 1f);
        public Vector2 LookInput => lookInput + mobileLookInput;
        public bool RunHeld => (runAction != null && runAction.enabled && runAction.IsPressed()) || UnityEngine.Input.GetKey(KeyCode.LeftShift);
        public float ZoomInput => zoomInput;

        public void Configure(InputActionAsset inputActions)
        {
            actions = inputActions;
            Initialize();
            if (isActiveAndEnabled) StartListening();
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (actions == null) return;
            if (playerMap != null) return;

            try
            {
                playerMap = actions.FindActionMap(PlayerMapName, false);
                uiMap = actions.FindActionMap(UiMapName, false);
                if (playerMap != null)
                {
                    moveAction = playerMap.FindAction("Move", false);
                    lookAction = playerMap.FindAction("Look", false);
                    runAction = playerMap.FindAction("Run", false) ?? playerMap.FindAction("Sprint", false);
                    zoomAction = playerMap.FindAction("Zoom", false);
                    jumpAction = playerMap.FindAction("Jump", false);
                    interactAction = playerMap.FindAction("Interact", false);
                    pauseAction = playerMap.FindAction("Pause", false);
                    attackAction = playerMap.FindAction("Attack", false);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlayerInputReader] Could not initialize input maps: {ex.Message}");
            }
        }

        private void OnEnable()
        {
            Initialize();
            StartListening();
        }

        private void StartListening()
        {
            if (isListening) return;

            if (actions != null)
            {
                try
                {
                    actions.Enable();
                }
                catch (System.Exception)
                {
                    // Fallback to enabling individual map safely
                    if (playerMap != null)
                    {
                        try { playerMap.Enable(); } catch (System.Exception) {}
                    }
                }
            }

            if (moveAction != null) { moveAction.performed += ReadMove; moveAction.canceled += ReadMove; }
            if (lookAction != null) { lookAction.performed += ReadLook; lookAction.canceled += ReadLook; }
            if (zoomAction != null) { zoomAction.performed += ReadZoom; zoomAction.canceled += ReadZoom; }
            SubscribeButtons();
            isListening = true;
        }

        private void OnDisable()
        {
            if (moveAction != null) { moveAction.performed -= ReadMove; moveAction.canceled -= ReadMove; }
            if (lookAction != null) { lookAction.performed -= ReadLook; lookAction.canceled -= ReadLook; }
            if (zoomAction != null) { zoomAction.performed -= ReadZoom; zoomAction.canceled -= ReadZoom; }
            UnsubscribeButtons();

            if (actions != null)
            {
                try
                {
                    actions.Disable();
                }
                catch (System.Exception) {}
            }
            else if (playerMap != null)
            {
                try { playerMap.Disable(); } catch (System.Exception) {}
            }

            isListening = false;
        }

        private void Update()
        {
            Vector2 rawInput = Vector2.zero;
            if (moveAction != null && moveAction.enabled)
            {
                try
                {
                    rawInput = moveAction.ReadValue<Vector2>();
                }
                catch (System.Exception) {}
            }

            if (lookAction != null && lookAction.enabled)
            {
                try
                {
                    lookInput = lookAction.ReadValue<Vector2>();
                }
                catch (System.Exception) {}
            }

            // Direct keyboard WASD check
            float x = 0f;
            float y = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)) y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)) x -= 1f;

            Vector2 kbDir = new Vector2(x, y);

            if (kbDir.sqrMagnitude > 0.001f)
            {
                moveInput = kbDir.normalized;
            }
            else if (rawInput.sqrMagnitude > 0.001f)
            {
                moveInput = rawInput;
            }
            else
            {
                // Guaranteed zero when no key is held to prevent endless Z movement
                moveInput = Vector2.zero;
            }
        }

        public void SetMobileMove(Vector2 value) => mobileMoveInput = Vector2.ClampMagnitude(value, 1f);
        public void SetMovementInput(Vector2 value) => SetMobileMove(value);
        public void SetMobileLook(Vector2 value) => mobileLookInput = value;
        public bool ConsumeJumpPressed() => Consume(ref jumpPressed) || UnityEngine.Input.GetKeyDown(KeyCode.Space);
        public bool ConsumeInteractPressed() => Consume(ref interactPressed) || UnityEngine.Input.GetKeyDown(KeyCode.E);
        public bool ConsumePausePressed() => Consume(ref pausePressed) || UnityEngine.Input.GetKeyDown(KeyCode.Escape);
        public bool ConsumeAttackPressed()
        {
            bool keyboardPressed = false;
            try { keyboardPressed = UnityEngine.Input.GetKeyDown(KeyCode.Q); } catch (System.Exception) {}
            return Consume(ref attackPressed) || keyboardPressed;
        }

        public void SetPaused(bool paused)
        {
            if (paused)
            {
                if (playerMap != null) try { playerMap.Disable(); } catch (System.Exception) {}
                if (uiMap != null) try { uiMap.Enable(); } catch (System.Exception) {}
            }
            else
            {
                if (uiMap != null) try { uiMap.Disable(); } catch (System.Exception) {}
                if (playerMap != null) try { playerMap.Enable(); } catch (System.Exception) {}
            }
        }

        private void ReadMove(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
        private void ReadLook(InputAction.CallbackContext context) => lookInput = context.ReadValue<Vector2>();
        private void ReadZoom(InputAction.CallbackContext context) => zoomInput = context.ReadValue<float>();

        private void SubscribeButtons()
        {
            if (jumpAction != null) jumpAction.performed += OnJump;
            if (interactAction != null) interactAction.performed += OnInteract;
            if (pauseAction != null) pauseAction.performed += OnPause;
            if (attackAction != null) attackAction.performed += OnAttack;
        }

        private void UnsubscribeButtons()
        {
            if (jumpAction != null) jumpAction.performed -= OnJump;
            if (interactAction != null) interactAction.performed -= OnInteract;
            if (pauseAction != null) pauseAction.performed -= OnPause;
            if (attackAction != null) attackAction.performed -= OnAttack;
        }

        private void OnJump(InputAction.CallbackContext _) => jumpPressed = true;
        private void OnInteract(InputAction.CallbackContext _) => interactPressed = true;
        private void OnPause(InputAction.CallbackContext _) => pausePressed = true;
        private void OnAttack(InputAction.CallbackContext _) => attackPressed = true;
        private static bool Consume(ref bool value) { bool result = value; value = false; return result; }
    }
}
