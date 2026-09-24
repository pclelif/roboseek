using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI.Core
{
    /// <summary>
    /// Reusable Sci-Fi Indicator badge (e.g. "ONLINE", "READY", "TARGET FOUND", "WARNING", "COMPLETED")
    /// featuring LED dot / icon, status label, color state mapping, and optional live pulsing.
    /// </summary>
    public class UIIndicator : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image ledDot;
        [SerializeField] private Image backgroundPill;
        [SerializeField] private Text statusLabel;

        [Header("Settings")]
        [SerializeField] private string initialText = "ONLINE";
        [SerializeField] private UIStateType currentState = UIStateType.Green;
        [SerializeField] private bool enablePulse = false;
        [SerializeField] private float pulseSpeed = 3f;

        private Coroutine pulseRoutine;
        private Vector3 originalDotScale = Vector3.one;

        public string Text => statusLabel != null ? statusLabel.text : string.Empty;
        public UIStateType CurrentState => currentState;

        private void Awake()
        {
            if (ledDot != null) originalDotScale = ledDot.transform.localScale;
            SetIndicator(initialText, currentState, enablePulse);
        }

        private void OnEnable()
        {
            if (enablePulse) StartPulse();
        }

        private void OnDisable()
        {
            StopPulse();
        }

        /// <summary>
        /// Updates the text, color state, and pulsing behavior of the indicator.
        /// </summary>
        public void SetIndicator(string text, UIStateType state, bool pulse = false)
        {
            currentState = state;
            enablePulse = pulse;

            if (statusLabel != null)
            {
                statusLabel.text = text?.ToUpper();
            }

            Color stateColor = UIStateColor.GetColor(state);
            Color glowColor = UIStateColor.GetGlowColor(state);

            if (ledDot != null)
            {
                ledDot.color = stateColor;
            }

            if (backgroundPill != null)
            {
                backgroundPill.color = new Color(stateColor.r, stateColor.g, stateColor.b, 0.18f);
            }

            if (statusLabel != null)
            {
                statusLabel.color = glowColor;
            }

            if (pulse && gameObject.activeInHierarchy)
            {
                StartPulse();
            }
            else
            {
                StopPulse();
            }
        }

        private void StartPulse()
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private void StopPulse()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }
            if (ledDot != null)
            {
                ledDot.transform.localScale = originalDotScale;
                ledDot.color = new Color(ledDot.color.r, ledDot.color.g, ledDot.color.b, 1f);
            }
        }

        private IEnumerator PulseRoutine()
        {
            while (true)
            {
                float wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f; // [0..1]
                if (ledDot != null)
                {
                    ledDot.transform.localScale = Vector3.Lerp(originalDotScale * 0.85f, originalDotScale * 1.25f, wave);
                    ledDot.color = new Color(ledDot.color.r, ledDot.color.g, ledDot.color.b, Mathf.Lerp(0.5f, 1f, wave));
                }
                yield return null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetIndicator(initialText, currentState, enablePulse);
        }
#endif
    }
}
