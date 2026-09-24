using UnityEngine;
using UnityEngine.UI;
using Robot.ObjectHunt;
using Robot.Combat;

namespace Robot.UI.Production
{
    public sealed class FireTruckTimeUI : MonoBehaviour
    {
        private UIRootController root;
        private CombatHealth health;
        private ObjectHuntRoundManager hunt;
        private Image promptPanel;
        private Text hintText;
        private float nextScan;
        private Collider fireTruck;
        private bool fWasHeld;
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
            if (health == null && playerTransform == null) ResolvePlayer();
            Vector3 position = playerTransform != null ? playerTransform.position : Vector3.zero;
            Vector3 closest = collider is MeshCollider mesh && !mesh.convex
                ? collider.bounds.ClosestPoint(position) : collider.ClosestPoint(position);
            return Vector3.Distance(position, closest);
        }

        private Transform playerTransform;
        private void ResolvePlayer()
        {
            if (health != null) { playerTransform = health.transform; return; }
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) { playerTransform = p.transform; health = p.GetComponent<CombatHealth>(); }
        }

        private void Update()
        {
            if (health == null || playerTransform == null) ResolvePlayer();
            if (hunt == null) hunt = FindFirstObjectByType<ObjectHuntRoundManager>();

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            bool held = (keyboard != null && keyboard.fKey.isPressed) || UnityEngine.Input.GetKey(KeyCode.F);
            bool pressed = held && !fWasHeld;
            fWasHeld = held;

            if (root == null || root.state == null || hunt == null) return;

            bool allowed = root.state.IsGameplay && hunt.IsRoundActive && hunt.InteractionEnabled && (health == null || !health.IsKnockedOut);

            if (allowed && Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + .2f;
                fireTruck = null;
                float closest = 4f;
                Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;

                foreach (var col in Physics.OverlapSphere(playerPos, 4f, ~0, QueryTriggerInteraction.Ignore))
                {
                    bool isFireTruck = false;
                    for (var t = col.transform; t != null; t = t.parent)
                    {
                        string name = t.name.ToLowerInvariant();
                        if (name.Contains("fire") || name.Contains("itfaiye") || name.Contains("firetruck") || name.Contains("truck"))
                        {
                            if (!name.Contains("ambo") && !name.Contains("ambulance") && !name.Contains("ambulans"))
                            {
                                isFireTruck = true;
                                break;
                            }
                        }
                    }
                    if (!isFireTruck) continue;
                    float distance = DistanceTo(col);
                    if (distance < closest) { closest = distance; fireTruck = col; }
                }
            }

            bool nearby = allowed && fireTruck != null && DistanceTo(fireTruck) <= 4f;
            promptPanel.gameObject.SetActive(nearby);

            if (nearby && pressed && !usedThisRound)
            {
                bool added = hunt.AddExtraTime(60f);
                if (added)
                {
                    usedThisRound = true;
                    var targetHUD = root.GetComponent<TargetHUD>();
                    if (targetHUD != null && targetHUD.feedback != null)
                    {
                        targetHUD.feedback.Show(UILocalization.IsTurkish ? "+1 DAKİKA EKLENDİ!" : "+1 MINUTE ADDED!");
                    }
                }
            }

            if (nearby)
            {
                hintText.text = usedThisRound
                    ? (UILocalization.IsTurkish ? "KULLANILDI" : "USED THIS ROUND")
                    : (UILocalization.IsTurkish ? "EK SÜRE (+1 DK)" : "EXTRA TIME (+1 MIN)");
            }
        }
    }
}
