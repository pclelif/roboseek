using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Robot.ObjectHunt;
using Robot.Combat;

namespace Robot.UI.Production
{
    public sealed class PoliceRadarUI : MonoBehaviour
    {
        private UIRootController root;
        private CombatHealth health;
        private ObjectHuntRoundManager hunt;
        private Image promptPanel;
        private Image radarStatusPanel;
        private Text radarStatusText;
        private float nextScan;
        private Collider policeCar;
        private bool rWasHeld;
        private float radarActiveUntil;
        private CollectibleTarget trackedTarget;
        private bool usedThisRound;
        private Transform playerTransform;

        private bool isInitialized;
        private bool EnsureInitialized()
        {
            if (isInitialized) return true;
            root = GetComponent<UIRootController>();
            if (root == null || root.state == null || root.state.gameplayHUD == null) return false;

            hunt = FindFirstObjectByType<ObjectHuntRoundManager>();
            if (hunt != null) hunt.RoundStarted += OnRoundStarted;
            ResolvePlayer();

            // Near-vehicle prompt panel [R] RADAR
            promptPanel = UIView.Panel("PolicePrompt", root.state.gameplayHUD.transform, new Vector2(400, 62), new Color(.035f, .035f, .035f, .96f));
            promptPanel.rectTransform.anchorMin = promptPanel.rectTransform.anchorMax = new Vector2(.5f, 0);
            promptPanel.rectTransform.anchoredPosition = new Vector2(0, 145);
            promptPanel.raycastTarget = false;

            UIView.Keycap(promptPanel.transform, "R", new Vector2(-140, 0), new Vector2(36, 38));
            UIView.Label("PoliceHint", promptPanel.transform, "RADAR", 19, new Vector2(278, 44), new Vector2(52, 0));

            // Floating Active Directional Radar HUD Banner
            radarStatusPanel = UIView.Panel("PoliceRadarHUD", root.state.gameplayHUD.transform, new Vector2(460, 48), new Color(.02f, .06f, .12f, .94f));
            radarStatusPanel.rectTransform.anchorMin = radarStatusPanel.rectTransform.anchorMax = new Vector2(.5f, .5f);
            radarStatusPanel.rectTransform.anchoredPosition = new Vector2(0, 235);
            radarStatusPanel.raycastTarget = false;

            radarStatusText = UIView.Label("RadarStatusText", radarStatusPanel.transform, "", 20, new Vector2(440, 40), Vector2.zero);
            radarStatusText.alignment = TextAnchor.MiddleCenter;
            radarStatusPanel.gameObject.SetActive(false);

            isInitialized = true;
            return true;
        }

        private void Start() => EnsureInitialized();

        private void OnDestroy()
        {
            if (hunt != null) hunt.RoundStarted -= OnRoundStarted;
        }

        private void OnRoundStarted(System.Collections.Generic.IReadOnlyList<TargetDefinition> _)
        {
            usedThisRound = false;
            trackedTarget = null;
            radarActiveUntil = 0f;
        }

        private float DistanceTo(Collider collider)
        {
            if (playerTransform == null) ResolvePlayer();
            Vector3 position = playerTransform != null ? playerTransform.position : Vector3.zero;
            Vector3 closest = collider is MeshCollider mesh && !mesh.convex
                ? collider.bounds.ClosestPoint(position) : collider.ClosestPoint(position);
            return Vector3.Distance(position, closest);
        }

        private void ResolvePlayer()
        {
            if (health != null && playerTransform != null) return;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) { playerTransform = p.transform; health = p.GetComponent<CombatHealth>(); }
        }

        private void Update()
        {
            if (!EnsureInitialized()) return;
            if (playerTransform == null) ResolvePlayer();
            if (hunt == null) hunt = FindFirstObjectByType<ObjectHuntRoundManager>();

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            bool held = (keyboard != null && keyboard.rKey.isPressed) || UnityEngine.Input.GetKey(KeyCode.R);
            bool pressed = held && !rWasHeld;
            rWasHeld = held;

            if (root == null || root.state == null || hunt == null) return;

            bool allowed = root.state.IsGameplay && hunt.IsRoundActive && hunt.InteractionEnabled && (health == null || !health.IsKnockedOut) && Robot.Core.MapManager.SelectedMap.enableCityMechanics;

            // Scan strictly for nearby Police Car
            if (allowed && !usedThisRound && Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + .2f;
                policeCar = null;
                float closest = 3f;
                Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;

                foreach (var col in Physics.OverlapSphere(playerPos, 3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (col == null || !col.enabled || col.isTrigger || !col.gameObject.activeInHierarchy) continue;
                    bool isPolice = false;
                    for (var t = col.transform; t != null; t = t.parent)
                    {
                        string name = t.name.ToLowerInvariant();
                        if (name.Contains("police") || name.Contains("polis") || name.Contains("cop"))
                        {
                            isPolice = true;
                            break;
                        }
                    }
                    if (!isPolice) continue;
                    float distance = DistanceTo(col);
                    if (distance <= closest) { closest = distance; policeCar = col; }
                }
            }

            bool nearby = allowed && !usedThisRound && policeCar != null && DistanceTo(policeCar) <= 3f;
            promptPanel.gameObject.SetActive(nearby);

            if (nearby && pressed && !usedThisRound)
            {
                // Select closest target ONLY among the 3 selected round targets!
                trackedTarget = null;
                float closestDist = float.MaxValue;

                if (hunt.ActiveTargets != null && hunt.SelectedTargets != null)
                {
                    foreach (var target in hunt.ActiveTargets)
                    {
                        if (target != null && target.gameObject.activeSelf && !target.IsCollecting)
                        {
                            bool isSelected = hunt.SelectedTargets.Any(s => s.objectId == target.Definition.objectId);
                            if (!isSelected) continue;

                            float dist = Vector3.Distance(playerTransform.position, target.transform.position);
                            if (dist < closestDist)
                            {
                                closestDist = dist;
                                trackedTarget = target;
                            }
                        }
                    }
                }

                if (trackedTarget != null)
                {
                    usedThisRound = true;
                    radarActiveUntil = Time.time + 30f; // Exactly 30 seconds active duration
                    promptPanel.gameObject.SetActive(false);
                    Robot.Audio.AudioManager.Instance?.PlayPoliceRadarScan(playerTransform.position);

                    var countdownHUD = root.GetComponent<VehicleCountdownHUD>();
                    if (countdownHUD != null)
                    {
                        countdownHUD.Trigger("RADAR", 30f);
                    }
                }
            }

            // Directional Radar Tracking HUD (Active for 30s anywhere on map)
            bool isRadarActive = allowed && Time.time < radarActiveUntil && trackedTarget != null && trackedTarget.gameObject.activeSelf && !trackedTarget.IsCollecting;
            radarStatusPanel.gameObject.SetActive(isRadarActive);

            if (isRadarActive && playerTransform != null)
            {
                Transform camTransform = Camera.main != null ? Camera.main.transform : playerTransform;
                Vector3 playerForward = camTransform.forward;
                playerForward.y = 0;
                if (playerForward.sqrMagnitude < 0.001f) playerForward = playerTransform.forward;
                playerForward.Normalize();

                Vector3 targetPos = trackedTarget.transform.position;
                Vector3 playerPos = playerTransform.position;
                Vector3 toTarget = targetPos - playerPos;
                toTarget.y = 0;
                float distance = toTarget.magnitude;

                string directionInfo = GetDirectionInfo(playerForward, toTarget, distance);
                radarStatusText.text = $"<color=#FFC700>{directionInfo}</color>";
            }
        }

        private string GetDirectionInfo(Vector3 forward, Vector3 toTarget, float distance)
        {
            if (toTarget.sqrMagnitude < 0.25f)
            {
                return UILocalization.IsTurkish ? "BURADA! 🎯" : "HERE! 🎯";
            }

            float angle = Vector3.SignedAngle(forward, toTarget.normalized, Vector3.up);
            int meters = Mathf.RoundToInt(distance);

            string dirText;
            string arrow;

            if (angle >= -22.5f && angle <= 22.5f)
            {
                dirText = UILocalization.IsTurkish ? "İLERİDE" : "FORWARD";
                arrow = "⬆️";
            }
            else if (angle > 22.5f && angle <= 67.5f)
            {
                dirText = UILocalization.IsTurkish ? "İLERİ-SAĞDA" : "FORWARD-RIGHT";
                arrow = "↗️";
            }
            else if (angle > 67.5f && angle <= 112.5f)
            {
                dirText = UILocalization.IsTurkish ? "SAĞDA" : "RIGHT";
                arrow = "➡️";
            }
            else if (angle > 112.5f && angle <= 157.5f)
            {
                dirText = UILocalization.IsTurkish ? "GERİ-SAĞDA" : "BACK-RIGHT";
                arrow = "↘️";
            }
            else if (angle < -22.5f && angle >= -67.5f)
            {
                dirText = UILocalization.IsTurkish ? "İLERİ-SOLDA" : "FORWARD-LEFT";
                arrow = "↖️";
            }
            else if (angle < -67.5f && angle >= -112.5f)
            {
                dirText = UILocalization.IsTurkish ? "SOLDA" : "LEFT";
                arrow = "⬅️";
            }
            else if (angle < -112.5f && angle >= -157.5f)
            {
                dirText = UILocalization.IsTurkish ? "GERİ-SOLDA" : "BACK-LEFT";
                arrow = "↙️";
            }
            else
            {
                dirText = UILocalization.IsTurkish ? "GERİDE" : "BACKWARD";
                arrow = "⬇️";
            }

            return $"{dirText} {arrow} ({meters}m)";
        }
    }
}
