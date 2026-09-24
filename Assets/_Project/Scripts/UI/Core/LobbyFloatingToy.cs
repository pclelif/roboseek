using UnityEngine;

namespace Robot.UI.Production
{
    [ExecuteAlways]
    public class LobbyFloatingToy : MonoBehaviour
    {
        public Vector3 basePosition;
        public Vector3 baseRotationEuler;
        public float bobAmplitude = 0.05f;
        public float bobSpeed = 1.4f;
        public float tiltAmplitude = 3.0f;
        public float tiltSpeed = 1.0f;
        public float phaseOffset;

        [SerializeField] private bool initialized;

        private void Awake()
        {
            EnsureAnchor();
        }

        private void OnEnable()
        {
            EnsureAnchor();
        }

        private void Start()
        {
            EnsureAnchor();
        }

        public void EnsureAnchor()
        {
            if (!initialized || basePosition == Vector3.zero)
            {
                basePosition = transform.position;
                baseRotationEuler = transform.eulerAngles;
                initialized = true;
            }
        }

        public void SetAnchor(Vector3 pos, Vector3 rotEuler)
        {
            basePosition = pos;
            baseRotationEuler = rotEuler;
            initialized = true;
        }

        private void Update()
        {
            EnsureAnchor();

            float time = Application.isPlaying ? Time.unscaledTime : 
#if UNITY_EDITOR
                (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
                Time.unscaledTime;
#endif
            time += phaseOffset;
            float yOffset = Mathf.Sin(time * bobSpeed) * bobAmplitude;
            float zTilt = Mathf.Sin(time * tiltSpeed) * tiltAmplitude;
            float xTilt = Mathf.Cos(time * (tiltSpeed * 0.85f)) * (tiltAmplitude * 0.6f);

            transform.position = basePosition + new Vector3(0f, yOffset, 0f);
            transform.rotation = Quaternion.Euler(baseRotationEuler.x + xTilt, baseRotationEuler.y, baseRotationEuler.z + zTilt);
        }
    }
}
