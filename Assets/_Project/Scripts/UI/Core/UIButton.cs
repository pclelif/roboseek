using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Robot.UI.Core
{
    /// <summary>
    /// Advanced Sci-Fi button component with Kenney UI assets, state handling (Normal, Hover, Pressed, Selected, Disabled),
    /// micro-animations, and audio feedback hooks.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Button References")]
        [SerializeField] private Text labelText;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Image accentBar;
        [SerializeField] private Image iconImage;

        [Header("State & Styling")]
        [SerializeField] private UIStateType buttonState = UIStateType.Blue;
        [SerializeField] private bool isInteractable = true;
        [SerializeField] private bool enableScaleAnimation = true;

        [Header("Sprites (Optional Swaps)")]
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite pressedSprite;
        [SerializeField] private Sprite hoverSprite;
        [SerializeField] private Sprite disabledSprite;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;

        [Header("Events")]
        public UnityEvent onClick = new UnityEvent();

        private bool isHovered = false;
        private bool isPressed = false;
        private bool isSelected = false;
        private Vector3 originalScale = Vector3.one;

        public bool IsInteractable => isInteractable;
        public string Text => labelText != null ? labelText.text : string.Empty;

        private void Awake()
        {
            if (buttonImage == null) buttonImage = GetComponent<Image>();
            originalScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            RefreshVisualState();
        }

        private void OnEnable()
        {
            isHovered = false;
            isPressed = false;
            isSelected = false;
            transform.localScale = originalScale;
            RefreshVisualState();
        }

        public void SetText(string text)
        {
            if (labelText != null) labelText.text = text;
        }

        public void SetState(UIStateType state)
        {
            buttonState = state;
            RefreshVisualState();
        }

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            if (!interactable)
            {
                isHovered = false;
                isPressed = false;
            }
            RefreshVisualState();
        }

        public void SetIcon(Sprite icon)
        {
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(icon != null);
                iconImage.sprite = icon;
            }
        }

        public void AddListener(UnityAction action)
        {
            onClick.AddListener(action);
        }

        public void RemoveListener(UnityAction action)
        {
            onClick.RemoveListener(action);
        }

        public void RemoveAllListeners()
        {
            onClick.RemoveAllListeners();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) return;
            isHovered = true;
            RefreshVisualState();

            if (enableScaleAnimation) transform.localScale = originalScale * 1.03f;
            PlaySound(hoverSound);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable) return;
            isHovered = false;
            isPressed = false;
            RefreshVisualState();

            if (enableScaleAnimation) transform.localScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) return;
            isPressed = true;
            RefreshVisualState();

            if (enableScaleAnimation) transform.localScale = originalScale * 0.96f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) return;

            bool wasPressed = isPressed;
            isPressed = false;
            RefreshVisualState();

            if (enableScaleAnimation) transform.localScale = isHovered ? originalScale * 1.03f : originalScale;

            if (wasPressed && isHovered)
            {
                PlaySound(clickSound);
                onClick?.Invoke();
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!isInteractable) return;
            isSelected = true;
            RefreshVisualState();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            if (buttonImage == null) return;

            Color baseColor = UIStateColor.GetColor(buttonState);
            Color glowColor = UIStateColor.GetGlowColor(buttonState);

            if (!isInteractable)
            {
                if (disabledSprite != null) buttonImage.sprite = disabledSprite;
                buttonImage.color = new Color(0.2f, 0.24f, 0.30f, 0.6f);
                if (labelText != null) labelText.color = UIStateColor.TextDisabled;
                if (accentBar != null) accentBar.color = new Color(0.3f, 0.35f, 0.4f, 0.4f);
                return;
            }

            if (isPressed)
            {
                if (pressedSprite != null) buttonImage.sprite = pressedSprite;
                buttonImage.color = baseColor * 0.82f;
                if (labelText != null) labelText.color = UIStateColor.TextPrimary;
                if (accentBar != null) accentBar.color = glowColor;
            }
            else if (isHovered || isSelected)
            {
                if (hoverSprite != null) buttonImage.sprite = hoverSprite;
                else if (normalSprite != null) buttonImage.sprite = normalSprite;

                buttonImage.color = baseColor * 1.15f;
                if (labelText != null) labelText.color = Color.white;
                if (accentBar != null) accentBar.color = Color.white;
            }
            else
            {
                if (normalSprite != null) buttonImage.sprite = normalSprite;
                buttonImage.color = baseColor;
                if (labelText != null) labelText.color = UIStateColor.TextPrimary;
                if (accentBar != null) accentBar.color = glowColor;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 0.6f);
            }
            else
            {
                Robot.Audio.AudioManager.Instance?.PlayUIClick();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (buttonImage == null) buttonImage = GetComponent<Image>();
            RefreshVisualState();
        }
#endif
    }
}
