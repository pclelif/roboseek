using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Robot.UI.Core;
using Robot.UI.HUD;

namespace Robot.UI.Demo
{
    /// <summary>
    /// Interactive Test and Showcase Controller for the UI_System_Demo scene.
    /// Provides controls for HUD, Windows, Toasts, Gauges, Reticle, and 3D Showcase Turntable.
    /// </summary>
    public class UIDemoController : MonoBehaviour
    {
        [Header("Framework References")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private RobotHuntHUD hud;
        [SerializeField] private UIWindow settingsWindow;
        [SerializeField] private UIWindow pauseWindow;
        [SerializeField] private Transform showcaseTurntable;
        [SerializeField] private GameObject dockContentRoot;
        [SerializeField] private Text dockToggleText;

        [Header("Live Simulation Values")]
        [SerializeField] private float simulatedHealth = 100f;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float simulatedEnergy = 85f;
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float simulatedShield = 100f;
        [SerializeField] private int simulatedTargetsFound = 0;
        [SerializeField] private int totalTargets = 3;
        [SerializeField] private float turntableSpeed = 15f;

        private readonly string[] sampleTargets = { "Teddy Bear", "Sports Car", "Ceramic Vase" };
        private ReticleState currentReticleIndex = ReticleState.Default;
        private bool promptActive = true;
        private bool dockExpanded = true;

        private void Start()
        {
            if (uiManager == null) uiManager = UIManager.Instance;
            if (hud == null) hud = FindFirstObjectByType<RobotHuntHUD>();

            InitializeDemoState();
        }

        private void Update()
        {
            if (showcaseTurntable != null)
            {
                showcaseTurntable.Rotate(Vector3.up, turntableSpeed * Time.deltaTime, Space.World);
            }
        }

        private void InitializeDemoState()
        {
            if (hud != null)
            {
                hud.ConfigureTargets(sampleTargets);
                hud.SetHealth(simulatedHealth, maxHealth);
                hud.SetEnergy(simulatedEnergy, maxEnergy);
                hud.SetShield(simulatedShield, 100f);
                hud.SetRoundTimer(525f); // 08:45
                hud.SetTargetCount(1, totalTargets);
                hud.MarkTargetFound(0, true);
                simulatedTargetsFound = 1;
                hud.SetInteractionPrompt(true, "E", "SCAN & RETRIEVE", "TEDDY BEAR");
            }

            if (uiManager != null)
            {
                uiManager.ShowNotification("SYSTEM ONLINE", "Robot telemetry synced. 3 target objects marked in sector.", NotificationType.Info, 4f);
            }
        }

        // --- DOCK COLLAPSE / EXPAND ---

        public void ToggleDock()
        {
            dockExpanded = !dockExpanded;
            if (dockContentRoot != null)
            {
                dockContentRoot.SetActive(dockExpanded);
            }
            if (dockToggleText != null)
            {
                dockToggleText.text = dockExpanded ? "▼ HIDE CONTROLS" : "▲ TEST CONTROLS";
            }
        }

        // --- HUD CONTROLS ---

        public void ToggleHUD()
        {
            if (uiManager == null) return;
            bool nextState = !uiManager.IsHUDVisible;
            uiManager.SetHUDVisible(nextState);
            uiManager.ShowNotification("HUD DISPLAY", nextState ? "HUD Systems Activated" : "HUD Systems Deactivated", NotificationType.Info, 2f);
        }

        public void ToggleInteractionPrompt()
        {
            if (hud == null) return;
            promptActive = !promptActive;
            hud.SetInteractionPrompt(promptActive, "E", "SCAN & RETRIEVE", "TEDDY BEAR");
        }

        public void CycleReticleState()
        {
            if (hud == null) return;
            currentReticleIndex = (ReticleState)(((int)currentReticleIndex + 1) % 3);
            hud.SetReticleState(currentReticleIndex);

            if (uiManager != null)
            {
                uiManager.ShowNotification("RETICLE MODE", $"Targeting Mode: {currentReticleIndex}", NotificationType.Info, 1.5f);
            }
        }

        // --- WINDOW CONTROLS ---

        public void OpenSettingsWindow()
        {
            if (uiManager != null && settingsWindow != null)
            {
                uiManager.OpenPanel(settingsWindow);
            }
        }

        public void OpenPauseWindow()
        {
            if (uiManager != null && pauseWindow != null)
            {
                uiManager.OpenPanel(pauseWindow);
            }
        }

        // --- NOTIFICATION CONTROLS ---

        public void SpawnInfoToast()
        {
            if (uiManager != null)
            {
                uiManager.ShowNotification("SECTOR SCAN", "Area mapped. High-value salvage detected nearby.", NotificationType.Info, 3.5f);
            }
        }

        public void SpawnSuccessToast()
        {
            if (uiManager != null)
            {
                uiManager.ShowNotification("OBJECT RECOVERED", "TEDDY BEAR RETRIEVED! (+100 PTS)", NotificationType.Success, 3.5f);
            }
        }

        public void SpawnWarningToast()
        {
            if (uiManager != null)
            {
                uiManager.ShowNotification("POWER RESERVE", "Energy core at 25%. Recharge station pinged.", NotificationType.Warning, 4f);
            }
        }

        public void SpawnErrorToast()
        {
            if (uiManager != null)
            {
                uiManager.ShowNotification("SYSTEM ALERT", "Actuator thermal overload in right stabilizer!", NotificationType.Error, 4.5f);
            }
        }

        // --- STATUS GAUGE CONTROLS ---

        public void ModifyHealth(float delta)
        {
            simulatedHealth = Mathf.Clamp(simulatedHealth + delta, 0f, maxHealth);
            if (hud != null) hud.SetHealth(simulatedHealth, maxHealth);
        }

        public void ModifyEnergy(float delta)
        {
            simulatedEnergy = Mathf.Clamp(simulatedEnergy + delta, 0f, maxEnergy);
            if (hud != null) hud.SetEnergy(simulatedEnergy, maxEnergy);
        }

        // --- TARGET SIMULATION CONTROLS ---

        public void SimulateCollectNextTarget()
        {
            if (simulatedTargetsFound < totalTargets)
            {
                if (hud != null)
                {
                    hud.MarkTargetFound(simulatedTargetsFound, true);
                    simulatedTargetsFound++;
                    hud.SetTargetCount(simulatedTargetsFound, totalTargets);
                }

                string targetName = sampleTargets[Mathf.Clamp(simulatedTargetsFound - 1, 0, sampleTargets.Length - 1)];
                if (uiManager != null)
                {
                    if (simulatedTargetsFound == totalTargets)
                    {
                        uiManager.ShowNotification("MISSION ACCOMPLISHED", "All 3 target objects successfully recovered!", NotificationType.Success, 4f);
                    }
                    else
                    {
                        uiManager.ShowNotification("TARGET FOUND", $"{targetName.ToUpper()} RECOVERED! ({simulatedTargetsFound}/{totalTargets})", NotificationType.Success, 3f);
                    }
                }
            }
        }

        public void ResetTargetsSimulation()
        {
            simulatedTargetsFound = 0;
            if (hud != null)
            {
                hud.ConfigureTargets(sampleTargets);
            }
            if (uiManager != null)
            {
                uiManager.ShowNotification("HUNT RESET", "Target objectives refreshed.", NotificationType.Info, 2f);
            }
        }
    }
}
