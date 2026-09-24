using System;
using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI.Core
{
    /// <summary>
    /// Reusable Sci-Fi UI Header component supporting titles, subtitles, state color themes, and Kenney header graphics.
    /// </summary>
    public class UIHeader : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Image headerBackground;
        [SerializeField] private Image stateAccentBar;

        [Header("Styling")]
        [SerializeField] private UIStateType currentState = UIStateType.Blue;
        [SerializeField] private bool uppercaseTitle = true;

        public string Title => titleText != null ? titleText.text : string.Empty;
        public string Subtitle => subtitleText != null ? subtitleText.text : string.Empty;
        public UIStateType CurrentState => currentState;

        private void Awake()
        {
            ApplyState();
        }

        /// <summary>
        /// Updates the title and optional subtitle.
        /// </summary>
        public void SetTitle(string title, string subtitle = null)
        {
            if (titleText != null)
            {
                titleText.text = uppercaseTitle ? title?.ToUpper() : title;
            }

            if (subtitleText != null)
            {
                if (string.IsNullOrEmpty(subtitle))
                {
                    subtitleText.gameObject.SetActive(false);
                }
                else
                {
                    subtitleText.gameObject.SetActive(true);
                    subtitleText.text = subtitle;
                }
            }
        }

        /// <summary>
        /// Sets the color theme/state of the header.
        /// </summary>
        public void SetState(UIStateType state)
        {
            currentState = state;
            ApplyState();
        }

        private void ApplyState()
        {
            Color stateColor = UIStateColor.GetColor(currentState);

            if (stateAccentBar != null)
            {
                stateAccentBar.color = stateColor;
            }

            if (headerBackground != null && stateAccentBar == null)
            {
                headerBackground.color = stateColor;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyState();
        }
#endif
    }
}
