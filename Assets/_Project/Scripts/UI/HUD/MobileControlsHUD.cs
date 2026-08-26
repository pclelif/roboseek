using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Robot.Input;
using Robot.ObjectHunt;

namespace Robot.UI.HUD
{
    [DisallowMultipleComponent]
    public sealed class MobileControlsHUD : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform joystickBackground;
        private RectTransform joystickHandle;
        private Vector2 inputVector;
        private PlayerInputReader inputReader;
        private ObjectHuntRoundManager huntManager;

        private void Start()
        {
            ApplySafeArea();
            BuildUI();
            FindInputReader();
            huntManager = FindFirstObjectByType<ObjectHuntRoundManager>();

            // Only show mobile UI on mobile devices or in editor for testing
            bool isMobile = Application.isMobilePlatform || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
#if !UNITY_EDITOR
            if (!isMobile) gameObject.SetActive(false);
#endif
        }

        private void Update()
        {
            if (inputReader == null) FindInputReader();
            if (inputReader != null && inputVector.sqrMagnitude > 0.001f)
            {
                inputReader.SetMovementInput(inputVector);
            }
        }

        private void FindInputReader()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) inputReader = p.GetComponent<PlayerInputReader>();
        }

        private void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector2 min = safeArea.position;
                Vector2 max = safeArea.position + safeArea.size;
                min.x /= Screen.width;
                min.y /= Screen.height;
                max.x /= Screen.width;
                max.y /= Screen.height;
                rect.anchorMin = min;
                rect.anchorMax = max;
            }
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            if (joystickBackground == null || joystickHandle == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBackground, eventData.position, eventData.pressEventCamera, out Vector2 pos))
            {
                pos.x /= (joystickBackground.sizeDelta.x * 0.5f);
                pos.y /= (joystickBackground.sizeDelta.y * 0.5f);
                inputVector = new Vector2(pos.x, pos.y);
                inputVector = inputVector.magnitude > 1.0f ? inputVector.normalized : inputVector;
                joystickHandle.anchoredPosition = new Vector2(inputVector.x * (joystickBackground.sizeDelta.x * 0.4f), inputVector.y * (joystickBackground.sizeDelta.y * 0.4f));
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            inputVector = Vector2.zero;
            if (joystickHandle != null) joystickHandle.anchoredPosition = Vector2.zero;
            if (inputReader != null) inputReader.SetMovementInput(Vector2.zero);
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MobileCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                transform.SetParent(canvasObj.transform, false);
            }

            // Left Joystick Zone
            GameObject joyBg = UITheme.CreatePanel(canvas.transform, "JoystickBackground", new Vector2(180f, 180f), new Color(0.1f, 0.15f, 0.22f, 0.7f), UITheme.GetButtonSquare());
            joystickBackground = joyBg.GetComponent<RectTransform>();
            joystickBackground.anchorMin = new Vector2(0f, 0f);
            joystickBackground.anchorMax = new Vector2(0f, 0f);
            joystickBackground.pivot = new Vector2(0.5f, 0.5f);
            joystickBackground.anchoredPosition = new Vector2(140f, 140f);

            GameObject joyHandle = UITheme.CreatePanel(joyBg.transform, "JoystickHandle", new Vector2(76f, 76f), UITheme.PrimaryBlue, UITheme.GetButtonSquare());
            joystickHandle = joyHandle.GetComponent<RectTransform>();
            joystickHandle.anchoredPosition = Vector2.zero;

            // Right Action Buttons
            // 1. Pickup Button
            Button pickupBtn = UITheme.CreateButton(canvas.transform, "PICK UP", new Vector2(110f, 110f), () =>
            {
                if (huntManager == null) huntManager = FindFirstObjectByType<ObjectHuntRoundManager>();
                huntManager?.TryPickupNearest();
            }, UITheme.AccentYellow, UITheme.GetButtonSquare());
            RectTransform pRect = pickupBtn.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(1f, 0f);
            pRect.anchorMax = new Vector2(1f, 0f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.anchoredPosition = new Vector2(-120f, 130f);

            // 2. Attack Button
            Button attackBtn = UITheme.CreateButton(canvas.transform, "ATTACK", new Vector2(90f, 90f), () =>
            {
                var atk = FindFirstObjectByType<Combat.CombatAttack>();
                atk?.TryAttack();
            }, UITheme.DangerRed, UITheme.GetButtonSquare());
            RectTransform aRect = attackBtn.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(1f, 0f);
            aRect.anchorMax = new Vector2(1f, 0f);
            aRect.pivot = new Vector2(0.5f, 0.5f);
            aRect.anchoredPosition = new Vector2(-230f, 90f);

            // 3. Sprint Button
            Button sprintBtn = UITheme.CreateButton(canvas.transform, "SPRINT", new Vector2(80f, 80f), null, UITheme.PrimaryBlue, UITheme.GetButtonSquare());
            RectTransform sRect = sprintBtn.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(1f, 0f);
            sRect.anchorMax = new Vector2(1f, 0f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(-120f, 250f);

            // 4. Pause Button (Top Right)
            Button pauseBtn = UITheme.CreateButton(canvas.transform, "||", new Vector2(60f, 60f), () =>
            {
                var pauseMenu = FindFirstObjectByType<PauseMenuUI>();
                pauseMenu?.Toggle();
            }, UITheme.GlassDark, UITheme.GetButtonSquare());
            RectTransform pauseRect = pauseBtn.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(1f, 1f);
            pauseRect.anchorMax = new Vector2(1f, 1f);
            pauseRect.pivot = new Vector2(1f, 1f);
            pauseRect.anchoredPosition = new Vector2(-30f, -30f);
        }
    }
}
