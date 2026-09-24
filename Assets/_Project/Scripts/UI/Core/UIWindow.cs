using System;
using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI.Core
{
    /// <summary>
    /// Reusable Sci-Fi Window component with Kenney header, content body, close button, and optional footer.
    /// Inherits from UIPanel for smooth animated open/close lifecycle.
    /// </summary>
    public class UIWindow : UIPanel
    {
        [Header("Window References")]
        [SerializeField] private UIHeader header;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform footerRoot;
        [SerializeField] private UIButton closeButton;
        [SerializeField] private Image backgroundImage;

        [Header("Window Settings")]
        [SerializeField] private string initialTitle = "WINDOW TITLE";
        [SerializeField] private string initialSubtitle = "";
        [SerializeField] private UIStateType windowState = UIStateType.Blue;

        public UIHeader Header => header;
        public RectTransform ContentRoot => contentRoot != null ? contentRoot : RectTransform;
        public RectTransform FooterRoot => footerRoot;
        public UIButton CloseButton => closeButton;

        protected override void Awake()
        {
            base.Awake();

            if (header != null && !string.IsNullOrEmpty(initialTitle))
            {
                header.SetTitle(initialTitle, initialSubtitle);
                header.SetState(windowState);
            }

            if (closeButton != null)
            {
                closeButton.AddListener(() => Close());
            }
        }

        /// <summary>
        /// Sets window title and optional subtitle.
        /// </summary>
        public void SetTitle(string title, string subtitle = null)
        {
            if (header != null)
            {
                header.SetTitle(title, subtitle);
            }
        }

        /// <summary>
        /// Sets the color theme/state of the window.
        /// </summary>
        public void SetState(UIStateType state)
        {
            windowState = state;
            if (header != null)
            {
                header.SetState(state);
            }
        }
    }
}
