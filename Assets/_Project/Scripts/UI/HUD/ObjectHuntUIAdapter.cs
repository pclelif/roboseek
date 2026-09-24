using System;
using System.Collections.Generic;
using UnityEngine;
using Robot.ObjectHunt;
using Robot.UI.Core;

namespace Robot.UI.HUD
{
    /// <summary>
    /// Event adapter connecting ObjectHunt gameplay events (RoundStarted, TargetCollected, TimerChanged, PhaseChanged)
    /// to the RobotHuntHUD and UIManager toast notification system without modifying gameplay logic.
    /// </summary>
    public class ObjectHuntUIAdapter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RobotHuntHUD hud;
        [SerializeField] private ObjectHuntRoundManager roundManager;
        [SerializeField] private RoundGameLoop gameLoop;

        private void Awake()
        {
            if (hud == null) hud = GetComponent<RobotHuntHUD>();
            if (roundManager == null) roundManager = FindFirstObjectByType<ObjectHuntRoundManager>();
            if (gameLoop == null) gameLoop = FindFirstObjectByType<RoundGameLoop>();
        }

        private void OnEnable()
        {
            if (roundManager != null)
            {
                roundManager.RoundStarted += OnRoundStarted;
                roundManager.TargetCollected += OnTargetCollected;
                roundManager.TimerChanged += OnTimerChanged;
            }

            if (gameLoop != null)
            {
                gameLoop.PhaseChanged += OnPhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (roundManager != null)
            {
                roundManager.RoundStarted -= OnRoundStarted;
                roundManager.TargetCollected -= OnTargetCollected;
                roundManager.TimerChanged -= OnTimerChanged;
            }

            if (gameLoop != null)
            {
                gameLoop.PhaseChanged -= OnPhaseChanged;
            }
        }

        private void Update()
        {
            if (hud == null || roundManager == null) return;

            CollectibleTarget nearest = roundManager.GetNearestInteractable();
            bool showPrompt = nearest != null && !nearest.IsCollecting;

            if (showPrompt)
            {
                string name = nearest.Definition != null ? nearest.Definition.displayName : "OBJECT";
                hud.SetInteractionPrompt(true, "E", "PICK UP", name);
            }
            else
            {
                hud.SetInteractionPrompt(false);
                hud.SetReticleState(ReticleState.Default);
            }
        }

        private void OnRoundStarted(IReadOnlyList<TargetDefinition> targets)
        {
            if (hud == null || targets == null) return;

            List<string> names = new List<string>();
            List<Sprite> icons = new List<Sprite>();

            for (int i = 0; i < targets.Count; i++)
            {
                names.Add(targets[i] != null ? targets[i].displayName : $"TARGET {i + 1}");
                icons.Add(targets[i] != null ? targets[i].icon : null);
            }

            hud.ConfigureTargets(names, icons);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification("ROUND STARTED", "Find all hidden target objects before time runs out!", NotificationType.Info, 4f);
            }
        }

        private void OnTargetCollected(TargetDefinition target, int currentFound)
        {
            if (hud == null) return;

            int targetIndex = -1;
            if (roundManager != null && roundManager.SelectedTargets != null)
            {
                for (int i = 0; i < roundManager.SelectedTargets.Count; i++)
                {
                    if (roundManager.SelectedTargets[i].objectId == target.objectId)
                    {
                        targetIndex = i;
                        break;
                    }
                }
            }

            if (targetIndex >= 0)
            {
                hud.MarkTargetFound(targetIndex, true);
            }

            int total = roundManager != null && roundManager.SelectedTargets != null ? roundManager.SelectedTargets.Count : 3;
            hud.SetTargetCount(currentFound, total);

            string targetName = target != null ? target.displayName : "TARGET";
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification("TARGET FOUND", $"{targetName.ToUpper()} COLLECTED! (+100 PTS)", NotificationType.Success, 3f);
            }
        }

        private void OnTimerChanged(float seconds)
        {
            if (hud != null)
            {
                hud.SetRoundTimer(seconds);
            }
        }

        private void OnPhaseChanged(RoundPhase phase)
        {
            if (UIManager.Instance == null) return;

            switch (phase)
            {
                case RoundPhase.Search:
                    UIManager.Instance.ShowNotification("HUNT IN PROGRESS", "Locate the targets!", NotificationType.Info, 2.5f);
                    break;
                case RoundPhase.RoundComplete:
                    UIManager.Instance.ShowNotification("OBJECTIVE COMPLETE", "All target items successfully collected!", NotificationType.Success, 4f);
                    break;
            }
        }
    }
}
