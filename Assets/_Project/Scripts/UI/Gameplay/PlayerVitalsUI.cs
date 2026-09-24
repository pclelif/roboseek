using UnityEngine;
using UnityEngine.UI;
using Robot.Combat;
using Robot.ObjectHunt;

namespace Robot.UI.Production
{
    public sealed class PlayerVitalsUI : MonoBehaviour
    {
        private UIRootController root;
        private CombatHealth health;
        private ObjectHuntRoundManager hunt;
        private Image fill, healingPanel;
        private Text healthText, healingHintText, keycapText;
        private Image keycapBg;
        private Transform keycapObj;
        private float nextScan;
        private Collider ambulance;
        private Transform ambulanceTransform;
        private bool healWasHeld;
        public bool IsHealing { get; private set; }
        private RectTransform healthPanel, trackRect;
        private Sprite yellowBar, greenBar;
        private readonly Vector2 trackPosition = new Vector2(0, -13);

        private float healingActiveUntil;
        private bool usedThisRound;

        private bool isInitialized;
        private bool EnsureInitialized()
        {
            if (isInitialized) return true;
            root = GetComponent<UIRootController>();
            if (root == null || root.state == null || root.state.gameplayHUD == null || root.state.inputGate == null || root.state.inputGate.movement == null) return false;

            health = root.state.inputGate.movement.GetComponent<CombatHealth>();
            if (health == null) return false;

            hunt = FindFirstObjectByType<ObjectHuntRoundManager>();
            if (hunt != null) hunt.RoundStarted += OnRoundStarted;
            health.Recovered += ResetHealing;

            var panel = UIView.Panel("PlayerHealth", root.state.gameplayHUD.transform, new Vector2(218, 78), new Color(.035f, .035f, .035f, .94f));
            healthPanel = panel.rectTransform;
            healthPanel.anchorMin = healthPanel.anchorMax = healthPanel.pivot = new Vector2(0, 1);
            healthPanel.anchoredPosition = new Vector2(40, -146);
            panel.raycastTarget = false;

            healthText = UIView.Label("HealthValue", panel.transform, "HEALTH", 16, new Vector2(190, 28), new Vector2(0, 19));
            var track = UIView.Panel("HealthTrack", panel.transform, new Vector2(182, 24), new Color(.22f, .22f, .22f), trackPosition);
            trackRect = track.rectTransform;
            yellowBar = Resources.Load<Sprite>("RobotHuntUI/HealthBar");
            greenBar = Resources.Load<Sprite>("RobotHuntUI/HealthBarGreen");
            track.sprite = yellowBar;
            track.raycastTarget = false;

            fill = UIView.Panel("HealthFill", track.transform, new Vector2(182, 24), Color.white);
            fill.sprite = yellowBar;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.raycastTarget = false;

            if (!Robot.Core.MapManager.SelectedMap.enableCityMechanics)
            {
                fill.sprite = null;
                track.sprite = null;
                fill.color = UIView.Accent;
                isInitialized = true;
                return true;
            }

            healingPanel = UIView.Panel("AmbulancePrompt", root.state.gameplayHUD.transform, new Vector2(400, 62), new Color(.035f, .035f, .035f, .96f));
            healingPanel.rectTransform.anchorMin = healingPanel.rectTransform.anchorMax = new Vector2(.5f, 0);
            healingPanel.rectTransform.anchoredPosition = new Vector2(0, 145);
            healingPanel.raycastTarget = false;

            keycapText = UIView.Keycap(healingPanel.transform, "H", new Vector2(-140, 0), new Vector2(36, 38));
            keycapObj = keycapText.transform.parent;
            keycapBg = keycapObj.GetComponent<Image>();
            healingHintText = UIView.Label("HealHint", healingPanel.transform, "", 19, new Vector2(278, 44), new Vector2(52, 0));

            isInitialized = true;
            return true;
        }

        private void Start() => EnsureInitialized();

        private void OnDestroy()
        {
            if (hunt != null) hunt.RoundStarted -= OnRoundStarted;
            if (health != null) health.Recovered -= ResetHealing;
        }

        private void OnRoundStarted(System.Collections.Generic.IReadOnlyList<TargetDefinition> _)
        {
            usedThisRound = false;
            healingActiveUntil = 0f;
            IsHealing = false;
        }

        private void ResetHealing(CombatHealth _)
        {
            IsHealing = false;
            if (trackRect != null) trackRect.anchoredPosition = trackPosition;
        }

        private void OnDisable()
        {
            healWasHeld = false;
            ResetHealing(null);
        }

        private float DistanceTo(Collider collider)
        {
            if (health == null) return 999f;
            Vector3 position = health.transform.position;
            Vector3 closest = collider is MeshCollider mesh && !mesh.convex
                ? collider.bounds.ClosestPoint(position) : collider.ClosestPoint(position);
            return Vector3.Distance(position, closest);
        }

        private float DistanceTo(Transform vehicle)
        {
            if (health == null || vehicle == null) return 999f;
            Vector3 position = health.transform.position;
            var renderers = vehicle.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return Vector3.Distance(position, vehicle.position);
            float nearest = float.MaxValue;
            foreach (var renderer in renderers)
                if (renderer != null) nearest = Mathf.Min(nearest, Vector3.Distance(position, renderer.bounds.ClosestPoint(position)));
            return nearest;
        }

        private static bool IsAmbulance(Transform source)
        {
            for (var t = source; t != null; t = t.parent)
            {
                string name = t.name.ToLowerInvariant();
                if (name.Contains("ambo") || name.Contains("ambulance") || name.Contains("ambulans")) return true;
            }
            return false;
        }

        private void Update()
        {
            if (!EnsureInitialized()) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            bool held = keyboard != null && keyboard.hKey.isPressed;
            bool pressed = held && !healWasHeld;
            healWasHeld = held;

            if (health == null || fill == null) return;
            if (hunt == null) hunt = FindFirstObjectByType<ObjectHuntRoundManager>();

            var timer = root.state.gameplayHUD.transform.Find("RoundTimer") as RectTransform;
            if (timer != null)
            {
                healthPanel.sizeDelta = new Vector2(timer.sizeDelta.x, 78);
                healthPanel.anchoredPosition = timer.anchoredPosition + Vector2.down * (timer.sizeDelta.y + 12);
                trackRect.sizeDelta = new Vector2(timer.sizeDelta.x - 36, 24);
                fill.rectTransform.sizeDelta = trackRect.sizeDelta;
            }

            if (!Robot.Core.MapManager.SelectedMap.enableCityMechanics)
            {
                fill.fillAmount = health.MaxHealth > 0 ? (float)health.CurrentHealth / health.MaxHealth : 0;
                // Filled Images need a sprite; a plain rect works with the shared theme.
                fill.rectTransform.localScale = new Vector3(fill.fillAmount, 1, 1);
                fill.rectTransform.pivot = new Vector2(0, .5f);
                fill.rectTransform.anchoredPosition = new Vector2(-trackRect.sizeDelta.x * .5f, 0);
                fill.color = fill.fillAmount > .3f ? UIView.Accent : new Color(1, .25f, .2f);
                healthText.text = "HEALTH";
                return;
            }

            bool allowed = root.state.IsGameplay && hunt != null && hunt.IsRoundActive && !health.IsKnockedOut;
            bool isHealthFull = health.CurrentHealth >= health.MaxHealth;

            if (allowed && !usedThisRound && Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + .2f;
                ambulance = null;
                ambulanceTransform = null;
                float closest = 3f;
                foreach (var col in Physics.OverlapSphere(health.transform.position, 3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (col == null || !col.enabled || col.isTrigger || !col.gameObject.activeInHierarchy) continue;
                    if (!IsAmbulance(col.transform)) continue;
                    float distance = DistanceTo(col);
                    if (distance <= closest) { closest = distance; ambulance = col; }
                }
                if (ambulance == null)
                    foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    {
                        if (renderer == null || !renderer.enabled || !IsAmbulance(renderer.transform)) continue;
                        float distance = DistanceTo(renderer.transform);
                        if (distance <= closest) { closest = distance; ambulanceTransform = renderer.transform; }
                    }
            }

            bool nearby = allowed && !usedThisRound && ((ambulance != null && DistanceTo(ambulance) <= 3f) || (ambulanceTransform != null && DistanceTo(ambulanceTransform) <= 3f));
            healingPanel.gameObject.SetActive(nearby);

            if (nearby)
            {
                if (isHealthFull)
                {
                    if (keycapText != null) keycapText.text = "H";
                    if (keycapBg != null) keycapBg.color = UIView.AccentYellow;
                    healingHintText.text = UILocalization.IsTurkish ? "CANIN DOLU" : "HEALTH FULL";
                }
                else
                {
                    if (keycapText != null) keycapText.text = "H";
                    if (keycapBg != null) keycapBg.color = UIView.AccentYellow;
                    healingHintText.text = UILocalization.IsTurkish ? "İYİLEŞ" : "HEAL";
                }
                healingHintText.rectTransform.anchoredPosition = new Vector2(52, 0);
            }

            if (nearby && pressed && !usedThisRound && !isHealthFull)
            {
                usedThisRound = true;
                healingActiveUntil = Time.time + 3.0f;
                healingPanel.gameObject.SetActive(false);
                Robot.Audio.AudioManager.Instance?.PlayAmbulanceHeal(health.transform.position);
            }

            bool isCurrentlyHealing = Time.time < healingActiveUntil && health.CurrentHealth < health.MaxHealth;
            if (isCurrentlyHealing)
            {
                health.Heal(40f * Time.deltaTime);
                IsHealing = true;
            }
            else
            {
                IsHealing = false;
            }

            float ratio = health.CurrentHealth / Mathf.Max(1, health.MaxHealth);
            fill.fillAmount = ratio;
            fill.sprite = IsHealing ? greenBar : yellowBar;
            fill.color = ratio > .3f || IsHealing ? Color.white : new Color(1, .25f, .2f);
            trackRect.anchoredPosition = trackPosition + (IsHealing
                ? new Vector2(Mathf.Sin(Time.time * 14f) * 0.6f, Mathf.Cos(Time.time * 18f) * 0.6f) : Vector2.zero);
            healthText.text = "HEALTH";
        }
    }
}
