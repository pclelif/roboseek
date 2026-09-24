using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI.Core
{
    public enum ReticleState
    {
        Default,
        Interactable,
        TargetLocked,
        Disabled
    }

    /// <summary>
    /// Centered Sci-Fi Crosshair/Reticle system utilizing Kenney crosshair assets,
    /// dynamic state changes (Neutral, Interactable Hover, Target Lock), and hit/interaction pulses.
    /// </summary>
    [RequireComponent(typeof(Image), typeof(RectTransform))]
    public class UIReticle : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image centerDot;
        [SerializeField] private Image outerRing;
        [SerializeField] private Image targetLockBracket;

        [Header("Sprites")]
        [SerializeField] private Sprite defaultCrosshair;
        [SerializeField] private Sprite interactCrosshair;
        [SerializeField] private Sprite lockCrosshair;

        [Header("State Settings")]
        [SerializeField] private ReticleState currentState = ReticleState.Default;

        private Image mainImage;
        private RectTransform rectTransform;
        private Coroutine pulseRoutine;
        private Vector3 originalScale = Vector3.one;

        public ReticleState CurrentState => currentState;

        private void Awake()
        {
            mainImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            originalScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            SetReticleState(currentState);
        }

        /// <summary>
        /// Updates the crosshair visual state.
        /// </summary>
        public void SetReticleState(ReticleState state)
        {
            currentState = state;

            if (state == ReticleState.Disabled)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            switch (state)
            {
                case ReticleState.Interactable:
                    if (interactCrosshair != null && mainImage != null) mainImage.sprite = interactCrosshair;
                    if (mainImage != null) mainImage.color = UIStateColor.Yellow;
                    if (centerDot != null) centerDot.color = UIStateColor.YellowGlow;
                    if (targetLockBracket != null) targetLockBracket.gameObject.SetActive(false);
                    transform.localScale = originalScale * 1.15f;
                    break;

                case ReticleState.TargetLocked:
                    if (lockCrosshair != null && mainImage != null) mainImage.sprite = lockCrosshair;
                    if (mainImage != null) mainImage.color = UIStateColor.Red;
                    if (centerDot != null) centerDot.color = UIStateColor.RedGlow;
                    if (targetLockBracket != null) targetLockBracket.gameObject.SetActive(true);
                    transform.localScale = originalScale * 1.25f;
                    break;

                case ReticleState.Default:
                default:
                    if (defaultCrosshair != null && mainImage != null) mainImage.sprite = defaultCrosshair;
                    if (mainImage != null) mainImage.color = new Color(1f, 1f, 1f, 0.75f);
                    if (centerDot != null) centerDot.color = UIStateColor.Blue;
                    if (targetLockBracket != null) targetLockBracket.gameObject.SetActive(false);
                    transform.localScale = originalScale;
                    break;
            }
        }

        /// <summary>
        /// Triggers a brief expanding hit/interaction pulse.
        /// </summary>
        public void TriggerPulse()
        {
            if (!gameObject.activeInHierarchy) return;
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            float elapsed = 0f;
            float duration = 0.18f;
            Vector3 peakScale = originalScale * 1.4f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(peakScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
            pulseRoutine = null;
        }

        /// <summary>
        /// Shows or hides the reticle.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mainImage == null) mainImage = GetComponent<Image>();
            SetReticleState(currentState);
        }
#endif
    }
}
