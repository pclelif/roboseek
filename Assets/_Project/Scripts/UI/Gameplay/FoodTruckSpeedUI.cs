using UnityEngine;
using UnityEngine.UI;
using Robot.ObjectHunt;
using Robot.Combat;
using Robot.Player.Movement;

namespace Robot.UI.Production
{
    public sealed class FoodTruckSpeedUI : MonoBehaviour
    {
        private UIRootController root;
        private CombatHealth health;
        private RobotMovementController movement;
        private ObjectHuntRoundManager hunt;
        private Image promptPanel;
        private Text hintText;
        private float nextScan;
        private Collider foodTruck;
        private bool bWasHeld;
        private float boostActiveUntil;
        private bool usedThisRound;

        private void Start()
        {
            Destroy(this);
            return;
        }

        private void OnDestroy()
        {
            if (hunt != null) hunt.RoundStarted -= OnRoundStarted;
        }

        private void OnRoundStarted(System.Collections.Generic.IReadOnlyList<TargetDefinition> _)
        {
            usedThisRound = false;
        }

        private float DistanceTo(Collider collider)
        {
            if (movement == null) ResolvePlayer();
            Vector3 position = movement != null ? movement.transform.position : Vector3.zero;
            Vector3 closest = collider is MeshCollider mesh && !mesh.convex
                ? collider.bounds.ClosestPoint(position) : collider.ClosestPoint(position);
            return Vector3.Distance(position, closest);
        }

        private void ResolvePlayer()
        {
            if (movement != null) return;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                movement = p.GetComponent<RobotMovementController>();
                health = p.GetComponent<CombatHealth>();
            }
        }

        private void Update()
        {
            if (movement == null) ResolvePlayer();
            if (hunt == null) hunt = FindFirstObjectByType<ObjectHuntRoundManager>();

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            bool held = (keyboard != null && keyboard.bKey.isPressed) || UnityEngine.Input.GetKey(KeyCode.B);
            bool pressed = held && !bWasHeld;
            bWasHeld = held;

            if (root == null || root.state == null || hunt == null) return;

            bool allowed = root.state.IsGameplay && hunt.IsRoundActive && hunt.InteractionEnabled && (health == null || !health.IsKnockedOut);

            if (allowed && Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + .2f;
                foodTruck = null;
                float closest = 4f;
                Vector3 playerPos = movement != null ? movement.transform.position : Vector3.zero;

                foreach (var col in Physics.OverlapSphere(playerPos, 4f, ~0, QueryTriggerInteraction.Ignore))
                {
                    bool isFoodTruck = false;
                    for (var t = col.transform; t != null; t = t.parent)
                    {
                        string name = t.name.ToLowerInvariant();
                        if (name.Contains("food") || name.Contains("icecream") || name.Contains("ice_cream") || name.Contains("burger") || name.Contains("taco") || name.Contains("cart") || name.Contains("van"))
                        {
                            if (!name.Contains("ambo") && !name.Contains("ambulance") && !name.Contains("fire"))
                            {
                                isFoodTruck = true;
                                break;
                            }
                        }
                    }
                    if (!isFoodTruck) continue;
                    float distance = DistanceTo(col);
                    if (distance < closest) { closest = distance; foodTruck = col; }
                }
            }

            bool nearby = allowed && foodTruck != null && DistanceTo(foodTruck) <= 4f;
            promptPanel.gameObject.SetActive(nearby);

            if (nearby && pressed && !usedThisRound)
            {
                if (movement != null)
                {
                    usedThisRound = true;
                    movement.ApplySpeedBoost(1.4f, 10f);
                    boostActiveUntil = Time.time + 10f;
                    var targetHUD = root.GetComponent<TargetHUD>();
                    if (targetHUD != null && targetHUD.feedback != null)
                    {
                        targetHUD.feedback.Show(UILocalization.IsTurkish ? "+%40 HIZ DOPİNGİ!" : "+40% SPEED BOOST!");
                    }
                }
            }

            if (nearby)
            {
                bool isBoosting = Time.time < boostActiveUntil;
                hintText.text = isBoosting
                    ? (UILocalization.IsTurkish ? "DOPİNG AKTİF!" : "BOOST ACTIVE!")
                    : usedThisRound
                    ? (UILocalization.IsTurkish ? "KULLANILDI" : "USED THIS ROUND")
                    : (UILocalization.IsTurkish ? "HIZ DOPİNGİ" : "SPEED BOOST");
            }
        }
    }
}
