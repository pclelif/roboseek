using UnityEngine;
using Robot.Player.Movement;

namespace Robot.Environment
{
    /// <summary>
    /// Interactive Sky Ring component. Refuels the player's Jetpack fuel,
    /// applies a speed and upward thrust boost, and triggers visual ring pulse effects.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class SkyRingBoost : MonoBehaviour
    {
        [Header("Boost Settings")]
        [SerializeField] private float speedMultiplier = 1.45f;
        [SerializeField] private float boostDuration = 3.0f;
        [SerializeField] private float upwardThrust = 12.0f;

        private Collider ringCollider;
        private Material ringMaterial;
        private Color originalColor = Color.cyan;
        private float cooldownUntil;

        private void Awake()
        {
            ringCollider = GetComponent<Collider>();
            if (ringCollider != null) ringCollider.isTrigger = true;

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                ringMaterial = renderer.material;
                if (ringMaterial.HasProperty("_Color")) originalColor = ringMaterial.color;
                else if (ringMaterial.HasProperty("_BaseColor")) originalColor = ringMaterial.GetColor("_BaseColor");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time < cooldownUntil) return;
            if (!other.CompareTag("Player") && other.GetComponentInParent<RobotMovementController>() == null) return;

            GameObject playerRoot = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
            var jetpack = playerRoot.GetComponent<RobotJetpackController>();
            var movement = playerRoot.GetComponent<RobotMovementController>();

            if (jetpack == null && movement == null)
            {
                jetpack = playerRoot.GetComponentInParent<RobotJetpackController>();
                movement = playerRoot.GetComponentInParent<RobotMovementController>();
            }

            if (movement == null) return;

            cooldownUntil = Time.time + 0.8f;

            // 1. Refuel Jetpack
            if (jetpack != null)
            {
                jetpack.Refuel();
            }

            // 2. Apply Speed Boost & Upward Launch Velocity
            movement.ApplySpeedBoost(speedMultiplier, boostDuration);
            if (jetpack != null)
            {
                jetpack.ApplyRingLaunch(upwardThrust, transform.forward);
            }

            // 3. Visual Ring Pulse
            if (ringMaterial != null)
            {
                StartCoroutine(PulseRingEffect());
            }

            // 4. Procedural particle flash
            CreateFlashEffect(transform.position);
        }

        private System.Collections.IEnumerator PulseRingEffect()
        {
            Color flashColor = new Color(0.2f, 0.95f, 1.0f, 1.0f);
            if (ringMaterial.HasProperty("_Color")) ringMaterial.color = flashColor;
            else if (ringMaterial.HasProperty("_BaseColor")) ringMaterial.SetColor("_BaseColor", flashColor);

            yield return new WaitForSeconds(0.4f);

            if (ringMaterial != null)
            {
                if (ringMaterial.HasProperty("_Color")) ringMaterial.color = originalColor;
                else if (ringMaterial.HasProperty("_BaseColor")) ringMaterial.SetColor("_BaseColor", originalColor);
            }
        }

        private static void CreateFlashEffect(Vector3 pos)
        {
            GameObject flash = new GameObject("RingFlash");
            flash.transform.position = pos;
            var light = flash.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.2f, 0.9f, 1f);
            light.range = 10f;
            light.intensity = 5f;
            Destroy(flash, 0.35f);
        }
    }
}
