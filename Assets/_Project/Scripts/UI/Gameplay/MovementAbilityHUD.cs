using UnityEngine;
using UnityEngine.UI;
using Robot.Core;
using Robot.Player.Movement;

namespace Robot.UI.Production
{
    public sealed class MovementAbilityHUD : MonoBehaviour
    {
        private RobotMovementController movement;
        private RobotJetpackController jetpack;
        private Image fill;
        private Text status;
        private void Start()
        {
            var root = GetComponent<UIRootController>();
            movement = root?.state?.inputGate?.movement;
            if (movement == null || movement.Traversal == MapTraversal.Standard) { enabled = false; return; }
            jetpack = movement.GetComponent<RobotJetpackController>();
            var panel = UIView.Panel("MovementAbility", root.state.gameplayHUD.transform, new Vector2(300, 90), new Color(.035f,.035f,.035f,.94f));
            panel.raycastTarget = false;
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = panel.rectTransform.pivot = Vector2.zero;
            panel.rectTransform.anchoredPosition = new Vector2(40, 40);
            string title = movement.Traversal == MapTraversal.Jetpack ? "SPACE  •  JETPACK" :
                movement.Traversal == MapTraversal.Dash ? "F  •  DASH" : "SHIFT  •  SPRINT / SPACE ×2";
            UIView.Label("AbilityName", panel.transform, title, 15, new Vector2(276, 28), new Vector2(0, 25), UIView.Accent);
            var track = UIView.Panel("ChargeTrack", panel.transform, new Vector2(260, 8), new Color(.2f,.23f,.27f), new Vector2(0, 1));
            track.raycastTarget = false; track.sprite = null;
            fill = UIView.Panel("Charge", track.transform, new Vector2(260, 8), UIView.Accent);
            fill.sprite = null; fill.raycastTarget = false;
            fill.rectTransform.pivot = new Vector2(0, .5f); fill.rectTransform.anchoredPosition = new Vector2(-130, 0);
            status = UIView.Label("AbilityStatus", panel.transform, "", 13, new Vector2(276, 26), new Vector2(0, -25), Color.white);
        }
        private void Update()
        {
            if (movement == null || fill == null) return;
            float value;
            if (movement.Traversal == MapTraversal.Jetpack)
            {
                value = jetpack.FuelNormalized;
                status.text = jetpack.IsFlying ? "THRUST" : jetpack.IsGliding ? "GLIDING • LAND TO REFUEL" : "FUEL  " + Mathf.CeilToInt(value * 100) + "%";
            }
            else if (movement.Traversal == MapTraversal.Dash)
            {
                value = movement.DashReadyNormalized;
                status.text = value >= .999f ? "READY" : "RECHARGING";
            }
            else
            {
                value = movement.AirJumpsRemaining;
                status.text = movement.IsGrounded || value > 0 ? "AIR JUMP READY" : "LAND TO RESET";
            }
            fill.rectTransform.sizeDelta = new Vector2(260 * Mathf.Clamp01(value), 8);
        }
    }
}
