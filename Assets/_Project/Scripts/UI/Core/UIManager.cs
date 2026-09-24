using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI.Core
{
    /// <summary>
    /// Central manager for the Robot Hunt UI Framework. Controls UI layers, window stacks,
    /// modal backdrops, toast notifications, HUD visibility, and cursor states.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;
        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<UIManager>();
                }
                return instance;
            }
        }

        [Header("Layer References")]
        [SerializeField] private RectTransform hudLayer;
        [SerializeField] private RectTransform windowsLayer;
        [SerializeField] private CanvasGroup modalBackdrop;
        [SerializeField] private RectTransform modalLayer;
        [SerializeField] private UINotification notificationSystem;
        [SerializeField] private RectTransform overlayLayer;
        [SerializeField] private RectTransform debugLayer;

        [Header("Default UI Windows (Optional)")]
        [SerializeField] private UIWindow pauseWindow;
        [SerializeField] private UIWindow settingsWindow;

        [Header("Cursor Settings")]
        [SerializeField] private bool autoManageCursor = true;

        private readonly Stack<UIPanel> activePanelStack = new Stack<UIPanel>();
        private bool isHudVisible = true;

        public bool IsAnyWindowOpen => activePanelStack.Count > 0;
        public bool IsHUDVisible => isHudVisible;
        public RectTransform HUDLayer => hudLayer;
        public RectTransform WindowsLayer => windowsLayer;
        public RectTransform ModalLayer => modalLayer;
        public UINotification NotificationSystem => notificationSystem;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            InitializeLayers();
        }

        private void Start()
        {
            UpdateModalBackdrop(false);
            if (autoManageCursor && !IsAnyWindowOpen)
            {
                SetCursorVisible(false);
            }
        }

        private void Update()
        {
            HandleGlobalInput();
        }

        private void InitializeLayers()
        {
            if (modalBackdrop != null)
            {
                Button backdropBtn = modalBackdrop.GetComponent<Button>();
                if (backdropBtn == null) backdropBtn = modalBackdrop.gameObject.AddComponent<Button>();
                backdropBtn.transition = Selectable.Transition.None;
                backdropBtn.onClick.RemoveAllListeners();
                backdropBtn.onClick.AddListener(OnModalBackdropClicked);
            }
        }

        private void HandleGlobalInput()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                if (activePanelStack.Count > 0)
                {
                    var top = activePanelStack.Peek();
                    if (top != null && top.CloseOnEscape)
                    {
                        ClosePanel(top);
                        return;
                    }
                }

                // If no window open, toggle Pause window if available
                if (pauseWindow != null)
                {
                    if (pauseWindow.IsOpen) ClosePanel(pauseWindow);
                    else OpenPanel(pauseWindow);
                }
            }
        }

        /// <summary>
        /// Opens a panel or window and pushes it onto the navigation stack.
        /// </summary>
        public void OpenPanel(UIPanel panel)
        {
            if (panel == null) return;

            if (!activePanelStack.Contains(panel))
            {
                activePanelStack.Push(panel);
            }

            panel.Open();

            if (panel.IsModal)
            {
                UpdateModalBackdrop(true);
            }

            if (autoManageCursor)
            {
                SetCursorVisible(true);
            }
        }

        /// <summary>
        /// Closes a panel or window and updates the navigation stack.
        /// </summary>
        public void ClosePanel(UIPanel panel)
        {
            if (panel == null) return;

            panel.Close();

            if (activePanelStack.Contains(panel))
            {
                // Reconstruct stack without this panel
                var temp = new List<UIPanel>(activePanelStack);
                temp.Remove(panel);
                activePanelStack.Clear();
                for (int i = temp.Count - 1; i >= 0; i--)
                {
                    activePanelStack.Push(temp[i]);
                }
            }

            // Check if any modal panels remain open
            bool hasModalOpen = false;
            foreach (var p in activePanelStack)
            {
                if (p != null && p.IsModal && p.IsOpen)
                {
                    hasModalOpen = true;
                    break;
                }
            }

            UpdateModalBackdrop(hasModalOpen);

            if (autoManageCursor && activePanelStack.Count == 0)
            {
                SetCursorVisible(false);
            }
        }

        /// <summary>
        /// Closes the top-most active panel.
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (activePanelStack.Count > 0)
            {
                UIPanel top = activePanelStack.Pop();
                if (top != null) ClosePanel(top);
            }
        }

        /// <summary>
        /// Shows a UI Window.
        /// </summary>
        public void ShowWindow(UIWindow window) => OpenPanel(window);

        /// <summary>
        /// Hides a UI Window.
        /// </summary>
        public void HideWindow(UIWindow window) => ClosePanel(window);

        /// <summary>
        /// Spawns a floating toast notification.
        /// </summary>
        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info, float duration = 3.5f)
        {
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification(title, message, type, duration);
            }
            else
            {
                Debug.Log($"[Notification - {type}] {title}: {message}");
            }
        }

        /// <summary>
        /// Sets HUD visibility.
        /// </summary>
        public void SetHUDVisible(bool visible)
        {
            isHudVisible = visible;
            if (hudLayer != null)
            {
                hudLayer.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Sets hardware cursor visibility and lock state.
        /// </summary>
        public void SetCursorVisible(bool visible, bool unlock = true)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? (unlock ? CursorLockMode.None : CursorLockMode.Confined) : CursorLockMode.Locked;
        }

        /// <summary>
        /// Switches input mode between UI and Gameplay.
        /// </summary>
        public void SetInputMode(bool uiActive)
        {
            SetCursorVisible(uiActive);
        }

        private void UpdateModalBackdrop(bool show)
        {
            if (modalBackdrop != null)
            {
                modalBackdrop.gameObject.SetActive(show);
                modalBackdrop.alpha = show ? 1f : 0f;
                modalBackdrop.blocksRaycasts = show;
                modalBackdrop.interactable = show;
            }
        }

        private void OnModalBackdropClicked()
        {
            if (activePanelStack.Count > 0)
            {
                UIPanel top = activePanelStack.Peek();
                if (top != null && top.IsModal && top.CloseOnEscape)
                {
                    ClosePanel(top);
                }
            }
        }
    }
}
