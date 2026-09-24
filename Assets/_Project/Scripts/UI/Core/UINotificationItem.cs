using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI.Core
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Individual toast notification element with slide-in/fade-out animation, Kenney sci-fi panel styling,
    /// title, description, accent bar, and duration countdown timer.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class UINotificationItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Image accentBar;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image timerBar;
        [SerializeField] private Image backgroundImage;

        [Header("Animation Settings")]
        [SerializeField] private float slideDuration = 0.25f;
        [SerializeField] private float fadeDuration = 0.2f;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Action<UINotificationItem> onDismissCallback;
        private Coroutine lifeRoutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void Initialize(string title, string message, NotificationType type, float duration, Action<UINotificationItem> onDismiss = null)
        {
            onDismissCallback = onDismiss;

            if (titleText != null) titleText.text = title?.ToUpper();
            if (messageText != null) messageText.text = message;

            UIStateType state = MapTypeToState(type);
            Color stateColor = UIStateColor.GetColor(state);

            if (accentBar != null) accentBar.color = stateColor;
            if (timerBar != null) timerBar.color = stateColor;
            if (iconImage != null) iconImage.color = stateColor;

            if (lifeRoutine != null) StopCoroutine(lifeRoutine);
            lifeRoutine = StartCoroutine(NotificationLifeRoutine(duration));
        }

        private IEnumerator NotificationLifeRoutine(float duration)
        {
            canvasGroup.alpha = 0f;
            Vector2 targetPos = rectTransform.anchoredPosition;
            Vector2 startPos = targetPos + new Vector2(250f, 0f);
            rectTransform.anchoredPosition = startPos;

            // Slide In & Fade In
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            rectTransform.anchoredPosition = targetPos;
            canvasGroup.alpha = 1f;

            // Display & Progress
            float timer = duration;
            while (timer > 0f)
            {
                timer -= Time.unscaledDeltaTime;
                if (timerBar != null)
                {
                    timerBar.fillAmount = Mathf.Clamp01(timer / duration);
                }
                yield return null;
            }

            // Fade & Slide Out
            elapsed = 0f;
            Vector2 exitPos = targetPos + new Vector2(100f, 0f);
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration);
                rectTransform.anchoredPosition = Vector2.Lerp(targetPos, exitPos, t);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            onDismissCallback?.Invoke(this);
            Destroy(gameObject);
        }

        private static UIStateType MapTypeToState(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success: return UIStateType.Green;
                case NotificationType.Warning: return UIStateType.Yellow;
                case NotificationType.Error: return UIStateType.Red;
                case NotificationType.Info:
                default: return UIStateType.Blue;
            }
        }
    }
}
