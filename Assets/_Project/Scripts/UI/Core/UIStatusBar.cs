using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI.Core
{
    /// <summary>
    /// Reusable Sci-Fi Status/Progress Bar component (Health, Energy, Shields, Round Timer, Objective Progress)
    /// supporting sliced Kenney bars, smooth animated fill transitions, label, value text, and state color modes.
    /// </summary>
    public class UIStatusBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text labelText;
        [SerializeField] private Text valueText;

        [Header("Configuration")]
        [SerializeField] private string barLabel = "STATUS";
        [SerializeField] private float currentValue = 100f;
        [SerializeField] private float maxValue = 100f;
        [SerializeField] private UIStateType currentState = UIStateType.Blue;
        [SerializeField] private bool showPercentage = true;
        [SerializeField] private bool animateFill = true;
        [SerializeField] private float animationSpeed = 4f;

        private Coroutine fillRoutine;
        private float displayedRatio = 1f;

        public float CurrentValue => currentValue;
        public float MaxValue => maxValue;
        public float Ratio => maxValue > 0f ? Mathf.Clamp01(currentValue / maxValue) : 0f;

        private void Awake()
        {
            ApplyState();
            UpdateLabels();
            SetDirectFill(Ratio);
        }

        /// <summary>
        /// Updates the current and max values of the status bar.
        /// </summary>
        public void SetValue(float current, float max, bool animate = true)
        {
            maxValue = Mathf.Max(0.001f, max);
            currentValue = Mathf.Clamp(current, 0f, maxValue);
            UpdateLabels();

            float targetRatio = Ratio;
            if (animate && animateFill && gameObject.activeInHierarchy)
            {
                if (fillRoutine != null) StopCoroutine(fillRoutine);
                fillRoutine = StartCoroutine(AnimateFillRoutine(targetRatio));
            }
            else
            {
                SetDirectFill(targetRatio);
            }
        }

        /// <summary>
        /// Updates the normalized progress [0..1].
        /// </summary>
        public void SetProgress(float ratio, bool animate = true)
        {
            SetValue(ratio * 100f, 100f, animate);
        }

        /// <summary>
        /// Sets the title/label of the status bar.
        /// </summary>
        public void SetLabel(string label)
        {
            barLabel = label;
            if (labelText != null) labelText.text = label;
        }

        /// <summary>
        /// Sets the state color of the bar.
        /// </summary>
        public void SetState(UIStateType state)
        {
            currentState = state;
            ApplyState();
        }

        private void ApplyState()
        {
            Color stateColor = UIStateColor.GetColor(currentState);
            if (fillImage != null)
            {
                fillImage.color = stateColor;
            }
        }

        private void UpdateLabels()
        {
            if (labelText != null && !string.IsNullOrEmpty(barLabel))
            {
                labelText.text = barLabel;
            }

            if (valueText != null)
            {
                if (showPercentage)
                {
                    valueText.text = $"{Mathf.RoundToInt(Ratio * 100f)}%";
                }
                else
                {
                    valueText.text = $"{Mathf.RoundToInt(currentValue)} / {Mathf.RoundToInt(maxValue)}";
                }
            }
        }

        private void SetDirectFill(float ratio)
        {
            displayedRatio = ratio;
            if (fillImage != null)
            {
                if (fillImage.type == Image.Type.Filled)
                {
                    fillImage.fillAmount = ratio;
                }
                else
                {
                    // If using standard Image with anchor scaling
                    RectTransform rect = fillImage.rectTransform;
                    rect.anchorMax = new Vector2(ratio, rect.anchorMax.y);
                }
            }
        }

        private IEnumerator AnimateFillRoutine(float targetRatio)
        {
            while (Mathf.Abs(displayedRatio - targetRatio) > 0.005f)
            {
                displayedRatio = Mathf.MoveTowards(displayedRatio, targetRatio, Time.unscaledDeltaTime * animationSpeed);
                SetDirectFill(displayedRatio);
                yield return null;
            }

            SetDirectFill(targetRatio);
            fillRoutine = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyState();
            UpdateLabels();
            SetDirectFill(Ratio);
        }
#endif
    }
}
