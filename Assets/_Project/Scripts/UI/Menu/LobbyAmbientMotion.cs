using UnityEngine;

namespace Robot.UI.Production
{
    /// <summary>Nearly imperceptible showroom hovering motion for the lobby hero robot.</summary>
    public sealed class LobbyAmbientMotion : MonoBehaviour
    {
        [Min(0f)] public float rotationSpeed = 0f;
        [Min(0f)] public float bobSpeed = 1.4f;
        [Min(0f)] public float bobAmount = 0.035f;

        private Vector3 basePosition;
        private bool baseRecorded;

        private void OnEnable() => RecordBase();

        private void RecordBase()
        {
            basePosition = transform.position;
            baseRecorded = true;
        }

        private void Update()
        {
            if (!baseRecorded) RecordBase();
            float y = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobAmount;
            transform.position = basePosition + new Vector3(0f, y, 0f);
            if (rotationSpeed > 0f)
            {
                transform.Rotate(0f, rotationSpeed * Time.unscaledDeltaTime, 0f, Space.World);
            }
        }
    }
}
