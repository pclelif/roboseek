using UnityEngine;
using UnityEngine.UI;
using Robot.ObjectHunt;
using Robot.Combat;
using Robot.Player.Movement;

namespace Robot.UI.Production
{
    public sealed class TaxiSpeedUI : MonoBehaviour
    {
        private UIRootController root;
        private CombatHealth health;
        private RobotMovementController movement;
        private ObjectHuntRoundManager hunt;
        private Image promptPanel;
        private float nextScan;
        private Collider taxiVehicle;
        private Transform taxiTransform;
        private bool tWasHeld;
        private bool usedThisRound;

        private bool isInitialized;
        private bool EnsureInitialized()
        {
            if (isInitialized) return true;
            root = GetComponent<UIRootController>();
            if (root == null || root.state == null || root.state.gameplayHUD == null) return false;

            ResolveHunt();
            ResolvePlayer();

            promptPanel = UIView.Panel("TaxiPrompt", root.state.gameplayHUD.transform, new Vector2(400, 62), new Color(.035f, .035f, .035f, .96f));
            promptPanel.rectTransform.anchorMin = promptPanel.rectTransform.anchorMax = new Vector2(.5f, 0);
            promptPanel.rectTransform.anchoredPosition = new Vector2(0, 145);
            promptPanel.raycastTarget = false;

            UIView.Keycap(promptPanel.transform, "T", new Vector2(-140, 0), new Vector2(36, 38));
            UIView.Label("TaxiHint", promptPanel.transform, "TURBO", 19, new Vector2(278, 44), new Vector2(52, 0));
            promptPanel.gameObject.SetActive(false);

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
            nextScan = 0f;
        }

        private void ResolveHunt()
        {
            var current = root != null && root.state != null && root.state.inputGate != null
                ? root.state.inputGate.hunt : null;
            if (current == null) current = hunt != null ? hunt : FindFirstObjectByType<ObjectHuntRoundManager>();
            if (current == hunt) return;
            if (hunt != null) hunt.RoundStarted -= OnRoundStarted;
            hunt = current;
            if (hunt != null) hunt.RoundStarted += OnRoundStarted;
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

        private float DistanceTo(Transform vehicle)
        {
            if (movement == null || vehicle == null) return 999f;
            Vector3 position = movement.transform.position;
            var renderers = vehicle.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return Vector3.Distance(position, vehicle.position);
            float nearest = float.MaxValue;
            foreach (var renderer in renderers)
                if (renderer != null) nearest = Mathf.Min(nearest, Vector3.Distance(position, renderer.bounds.ClosestPoint(position)));
            return nearest;
        }

        private static bool IsTaxi(Transform source)
        {
            for (var t = source; t != null; t = t.parent)
            {
                string name = t.name.ToLowerInvariant();
                if (name.Contains("taxi") || name.Contains("cab") || name.Contains("taksi") || name.Contains("yellowcab")) return true;
            }
            return false;
        }

        private void ResolvePlayer()
        {
            // The gameplay gate holds the controlled robot. A Player-tagged
            // object can be a visual child or showcase rather than its controller.
            var controlled = root != null && root.state != null && root.state.inputGate != null
                ? root.state.inputGate.movement : null;
            if (controlled != null)
            {
                movement = controlled;
                health = controlled.GetComponent<CombatHealth>();
                return;
            }
            if (movement != null) return;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                movement = p.GetComponentInParent<RobotMovementController>() ?? p.GetComponentInChildren<RobotMovementController>();
                health = movement != null ? movement.GetComponent<CombatHealth>() : null;
            }
        }

        private void Update()
        {
            if (!EnsureInitialized()) return;
            ResolvePlayer();
            ResolveHunt();

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            bool held = (keyboard != null && keyboard.tKey.isPressed) || UnityEngine.Input.GetKey(KeyCode.T);
            bool pressed = held && !tWasHeld;
            tWasHeld = held;

            if (root == null || root.state == null || hunt == null || movement == null)
            {
                promptPanel.gameObject.SetActive(false);
                return;
            }

            bool allowed = root.state.IsGameplay && hunt.IsRoundActive && hunt.InteractionEnabled && (health == null || !health.IsKnockedOut) && Robot.Core.MapManager.SelectedMap.enableCityMechanics;

            if (allowed && !usedThisRound && Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + .2f;
                taxiVehicle = null;
                taxiTransform = null;
                float closest = 3f;
                Vector3 playerPos = movement != null ? movement.transform.position : Vector3.zero;

                foreach (var col in Physics.OverlapSphere(playerPos, 3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (col == null || !col.enabled || col.isTrigger || !col.gameObject.activeInHierarchy) continue;
                    if (!IsTaxi(col.transform)) continue;
                    float distance = DistanceTo(col);
                    if (distance <= closest) { closest = distance; taxiVehicle = col; }
                }
                // Synty City taxis are often renderer-only; use rendered bounds
                // so the ability doesn't disappear merely because a prefab lacks a collider.
                if (taxiVehicle == null)
                    foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    {
                        if (renderer == null || !renderer.enabled || !IsTaxi(renderer.transform)) continue;
                        float distance = DistanceTo(renderer.transform);
                        if (distance <= closest) { closest = distance; taxiTransform = renderer.transform; }
                    }
            }

            bool nearby = allowed && !usedThisRound && ((taxiVehicle != null && DistanceTo(taxiVehicle) <= 3f) || (taxiTransform != null && DistanceTo(taxiTransform) <= 3f));
            promptPanel.gameObject.SetActive(nearby);

            if (nearby && pressed && !usedThisRound)
            {
                if (movement != null)
                {
                    usedThisRound = true;
                    movement.ApplySpeedBoost(1.6f, 10f);
                    promptPanel.gameObject.SetActive(false);
                    Robot.Audio.AudioManager.Instance?.PlayTaxiTurboBoost(movement.transform.position);

                    var countdownHUD = root.GetComponent<VehicleCountdownHUD>();
                    if (countdownHUD != null)
                    {
                        countdownHUD.Trigger("TURBO ⚡", 10f);
                    }
                }
            }
        }
    }
}
