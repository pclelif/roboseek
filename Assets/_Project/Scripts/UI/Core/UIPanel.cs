using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Robot.UI.Core
{
    /// <summary>
    /// Base panel class for all screens, windows, dialogs, and overlays in the UI Framework.
    /// Provides lifecycle methods, smooth CanvasGroup alpha fading, scale transitions, and modal support.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class UIPanel : MonoBehaviour
    {
        [Header("Panel Configuration")]
        [SerializeField] private bool openOnStart = false;
        [SerializeField] private bool isModal = false;
        [SerializeField] private bool closeOnEscape = true;
        [SerializeField] private bool playAnimations = true;
        [SerializeField] private float transitionDuration = 0.2f;

        [Header("Events")]
        public UnityEvent onOpened;
        public UnityEvent onClosed;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Coroutine transitionRoutine;
        private bool isOpen;
        private bool isTransitioning;
        private Vector3 originalScale = Vector3.one;

        public bool IsOpen => isOpen;
        public bool IsModal => isModal;
        public bool CloseOnEscape => closeOnEscape;
        public bool IsTransitioning => isTransitioning;
        public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());
        public CanvasGroup CanvasGroup => canvasGroup != null ? canvasGroup : (canvasGroup = GetComponent<CanvasGroup>());

        protected virtual void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            originalScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;

            if (!openOnStart)
            {
                SetDirectState(false);
            }
        }

        protected virtual void Start()
        {
            if (openOnStart)
            {
                Open(immediate: true);
            }
        }

        public virtual void Open(bool immediate = false)
        {
            if (isOpen && !isTransitioning) return;

            gameObject.SetActive(true);
            OnOpening();

            if (transitionRoutine != null) StopCoroutine(transitionRoutine);

            if (immediate || !playAnimations || transitionDuration <= 0f)
            {
                SetDirectState(true);
                OnOpened();
                onOpened?.Invoke();
            }
            else
            {
                transitionRoutine = StartCoroutine(TransitionRoutine(true));
            }
        }

        public virtual void Close(bool immediate = false)
        {
            if (!isOpen && !isTransitioning && !gameObject.activeSelf) return;

            OnClosing();

            if (transitionRoutine != null) StopCoroutine(transitionRoutine);

            if (immediate || !playAnimations || transitionDuration <= 0f)
            {
                SetDirectState(false);
                OnClosed();
                onClosed?.Invoke();
            }
            else
            {
                transitionRoutine = StartCoroutine(TransitionRoutine(false));
            }
        }

        public virtual void Show() => Open();
        public virtual void Hide() => Close();
        public virtual void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        private IEnumerator TransitionRoutine(bool targetOpen)
        {
            isTransitioning = true;
            float startAlpha = CanvasGroup.alpha;
            float targetAlpha = targetOpen ? 1f : 0f;

            Vector3 startScale = targetOpen ? originalScale * 0.94f : originalScale;
            Vector3 targetScale = targetOpen ? originalScale : originalScale * 0.94f;

            if (targetOpen)
            {
                gameObject.SetActive(true);
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                CanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);
                transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

                yield return null;
            }

            SetDirectState(targetOpen);
            isTransitioning = false;
            transitionRoutine = null;

            if (targetOpen)
            {
                OnOpened();
                onOpened?.Invoke();
            }
            else
            {
                OnClosed();
                onClosed?.Invoke();
            }
        }

        private void SetDirectState(bool active)
        {
            isOpen = active;
            CanvasGroup.alpha = active ? 1f : 0f;
            CanvasGroup.interactable = active;
            CanvasGroup.blocksRaycasts = active;
            transform.localScale = active ? originalScale : originalScale * 0.94f;

            if (!active)
            {
                gameObject.SetActive(false);
            }
        }

        protected virtual void OnOpening() { }
        protected virtual void OnOpened() { }
        protected virtual void OnClosing() { }
        protected virtual void OnClosed() { }
    }
}
