using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Robot.UI.Core;

namespace Robot.UI.HUD
{
    /// <summary>
    /// Immersive, High-End Sci-Fi Gameplay HUD for Robot Hunt.
    /// Integrates:
    /// - Top-Left Unit & Sector Telemetry Badge
    /// - Top-Center Nav Compass & Round Countdown Timer
    /// - Top-Right Object Hunt Protocol Target Tracker (Cards, Status Chips, Progress Bar)
    /// - Center Targeting Reticle, Distance Readout & Floating [E] Interaction Badge
    /// - Bottom-Left Robot Vital Telemetry (Health, Energy, Shield, Subsystem LEDs)
    /// </summary>
    public class RobotHuntHUD : MonoBehaviour
    {
        [Header("Reticle & Targeting")]
        [SerializeField] private UIReticle reticle;
        [SerializeField] private Text targetDistanceText;

        [Header("Interaction Prompt")]
        [SerializeField] private GameObject interactionPromptRoot;
        [SerializeField] private Text promptKeyText;
        [SerializeField] private Text promptActionText;
        [SerializeField] private Text promptTargetText;
        [SerializeField] private Image promptKeyBadge;

        [Header("Top Navigation & Header")]
        [SerializeField] private Text sectorText;
        [SerializeField] private Text compassText;
        [SerializeField] private Text roundTimerText;
        [SerializeField] private UIIndicator unitStatusIndicator;

        [Header("Robot Telemetry (Bottom-Left)")]
        [SerializeField] private UIStatusBar healthBar;
        [SerializeField] private UIStatusBar energyBar;
        [SerializeField] private UIStatusBar shieldBar;
        [SerializeField] private UIIndicator scannerIndicator;
        [SerializeField] private UIIndicator radarIndicator;

        [Header("Object Hunt Target Tracker (Top-Right)")]
        [SerializeField] private GameObject targetTrackerRoot;
        [SerializeField] private Text targetCounterText;
        [SerializeField] private UIStatusBar huntProgressBar;
        [SerializeField] private Transform targetCardsContainer;
        [SerializeField] private List<TargetCardUI> targetCards = new List<TargetCardUI>();

        public UIReticle Reticle => reticle;
        public UIStatusBar HealthBar => healthBar;
        public UIStatusBar EnergyBar => energyBar;
        public UIStatusBar ShieldBar => shieldBar;
        public UIIndicator RadarIndicator => radarIndicator;

        [Serializable]
        public class TargetCardUI
        {
            public GameObject root;
            public Image previewImage;
            public Text nameText;
            public Text statusText;
            public Image statusBadge;
            public Text checkmarkText;
        }

        private void Awake()
        {
            SetInteractionPrompt(false);
            if (unitStatusIndicator != null)
            {
                unitStatusIndicator.SetIndicator("STATUS: NOMINAL", UIStateType.Green, pulse: true);
            }
            if (radarIndicator != null)
            {
                radarIndicator.SetIndicator("RADAR: 360°", UIStateType.Green, pulse: true);
            }
            if (scannerIndicator != null)
            {
                scannerIndicator.SetIndicator("SCANNER: ACTIVE", UIStateType.Blue, pulse: false);
            }
        }

        /// <summary>
        /// Shows or hides the floating interaction prompt.
        /// </summary>
        public void SetInteractionPrompt(bool visible, string key = "E", string actionName = "SCAN & RETRIEVE", string targetName = "")
        {
            if (interactionPromptRoot != null)
            {
                interactionPromptRoot.SetActive(visible);
            }

            if (!visible)
            {
                if (targetDistanceText != null) targetDistanceText.gameObject.SetActive(false);
                return;
            }

            if (promptKeyText != null) promptKeyText.text = key;
            if (promptActionText != null) promptActionText.text = actionName?.ToUpper();
            if (promptTargetText != null)
            {
                promptTargetText.text = string.IsNullOrEmpty(targetName) ? "" : $"// {targetName.ToUpper()}";
            }

            if (targetDistanceText != null)
            {
                targetDistanceText.gameObject.SetActive(true);
                targetDistanceText.text = $"TARGET LOCKED • DIST: 2.4M";
            }

            if (reticle != null)
            {
                reticle.SetReticleState(ReticleState.Interactable);
            }
        }

        /// <summary>
        /// Updates the crosshair reticle state.
        /// </summary>
        public void SetReticleState(ReticleState state)
        {
            if (reticle != null)
            {
                reticle.SetReticleState(state);
            }
        }

        /// <summary>
        /// Updates health bar status.
        /// </summary>
        public void SetHealth(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.SetValue(current, max);
                float ratio = max > 0 ? current / max : 0f;
                healthBar.SetState(ratio > 0.35f ? UIStateType.Green : UIStateType.Red);
            }
        }

        /// <summary>
        /// Updates energy bar status.
        /// </summary>
        public void SetEnergy(float current, float max)
        {
            if (energyBar != null)
            {
                energyBar.SetValue(current, max);
                float ratio = max > 0 ? current / max : 0f;
                energyBar.SetState(ratio > 0.25f ? UIStateType.Blue : UIStateType.Yellow);
            }
        }

        /// <summary>
        /// Updates shield bar status.
        /// </summary>
        public void SetShield(float current, float max)
        {
            if (shieldBar != null)
            {
                shieldBar.SetValue(current, max);
            }
        }

        /// <summary>
        /// Updates round countdown timer.
        /// </summary>
        public void SetRoundTimer(float seconds)
        {
            if (roundTimerText != null)
            {
                int totalSec = Mathf.Max(0, Mathf.CeilToInt(seconds));
                roundTimerText.text = $"⏱ {totalSec / 60:00}:{totalSec % 60:00}";
                roundTimerText.color = seconds < 60f ? UIStateColor.Red : UIStateColor.Yellow;
            }
        }

        /// <summary>
        /// Updates target counter & progress bar.
        /// </summary>
        public void SetTargetCount(int found, int total)
        {
            if (targetCounterText != null)
            {
                targetCounterText.text = $"TARGETS  [ {found} / {total} ]";
            }

            if (huntProgressBar != null)
            {
                float ratio = total > 0 ? (float)found / total : 0f;
                huntProgressBar.SetProgress(ratio);
                huntProgressBar.SetState(ratio >= 1f ? UIStateType.Green : UIStateType.Blue);
            }
        }

        /// <summary>
        /// Configures initial target cards.
        /// </summary>
        public void ConfigureTargets(IReadOnlyList<string> names, IReadOnlyList<Sprite> icons = null)
        {
            for (int i = 0; i < targetCards.Count; i++)
            {
                bool active = names != null && i < names.Count;
                if (targetCards[i].root != null)
                {
                    targetCards[i].root.SetActive(active);
                }

                if (!active) continue;

                if (targetCards[i].nameText != null)
                {
                    targetCards[i].nameText.text = names[i]?.ToUpper();
                    targetCards[i].nameText.color = UIStateColor.TextPrimary;
                }

                if (targetCards[i].statusText != null)
                {
                    targetCards[i].statusText.text = "SCANNING AREA...";
                    targetCards[i].statusText.color = UIStateColor.Blue;
                }

                if (targetCards[i].checkmarkText != null)
                {
                    targetCards[i].checkmarkText.text = "•";
                    targetCards[i].checkmarkText.color = UIStateColor.Blue;
                }

                if (targetCards[i].previewImage != null && icons != null && i < icons.Count && icons[i] != null)
                {
                    targetCards[i].previewImage.sprite = icons[i];
                }

                if (targetCards[i].statusBadge != null)
                {
                    targetCards[i].statusBadge.color = UIStateColor.Blue;
                }
            }

            SetTargetCount(0, names != null ? names.Count : 3);
        }

        /// <summary>
        /// Marks a specific target card as found.
        /// </summary>
        public void MarkTargetFound(int index, bool found = true)
        {
            if (index < 0 || index >= targetCards.Count) return;

            var card = targetCards[index];
            if (card.checkmarkText != null)
            {
                card.checkmarkText.text = found ? "✓" : "•";
                card.checkmarkText.color = found ? UIStateColor.Green : UIStateColor.Blue;
            }

            if (card.statusText != null)
            {
                card.statusText.text = found ? "RECOVERED" : "SCANNING AREA...";
                card.statusText.color = found ? UIStateColor.Green : UIStateColor.Blue;
            }

            if (card.nameText != null)
            {
                card.nameText.color = found ? UIStateColor.Green : UIStateColor.TextPrimary;
            }

            if (card.statusBadge != null)
            {
                card.statusBadge.color = found ? UIStateColor.Green : UIStateColor.Blue;
            }

            if (reticle != null)
            {
                reticle.TriggerPulse();
            }
        }
    }
}
