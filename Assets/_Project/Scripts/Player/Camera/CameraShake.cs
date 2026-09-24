using UnityEngine;

namespace Robot.Player.CameraControl
{
    /// <summary>
    /// Camera shake disabled per user request.
    /// </summary>
    public sealed class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null || Instance == this) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Trigger(float duration = 0.35f, float intensity = 0.18f)
        {
            // Completely disabled
        }

        public void Play(float duration, float intensity)
        {
            // Completely disabled
        }

        private void LateUpdate()
        {
            // Completely disabled
        }
    }
}
