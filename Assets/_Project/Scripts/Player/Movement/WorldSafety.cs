using UnityEngine;
namespace Robot.Player.Movement
{
    // Serialized, scene-authored limits; walls block normal movement and this handles
    // teleports / tunnelling without allowing an endless fall.
    public sealed class WorldSafety : MonoBehaviour
    {
        public Bounds limits;
        public float waterHeight = -999f;
        [Min(.1f)] public float drowningDuration = 5f;
        public float DrowningTimeRemaining { get; private set; }
        public bool IsDrowning { get; private set; }
        private Robot.ObjectHunt.ObjectHuntRoundManager hunt;
        private CharacterController controller;
        private RobotMovementController movement;
        private Camera view;
        private Color background;
        private CameraClearFlags clearFlags;
        private bool underwater;
        private bool originalFog;
        private Color originalFogColor;
        private FogMode originalFogMode;
        private float originalFogDensity;
        private AudioSource drowningAudioSource;

        private void Start()
        {
            hunt = FindFirstObjectByType<Robot.ObjectHunt.ObjectHuntRoundManager>();
            DrowningTimeRemaining = drowningDuration;
            controller = GetComponent<CharacterController>();
            movement = GetComponent<RobotMovementController>();
            view = Camera.main;
            originalFog = RenderSettings.fog; originalFogColor = RenderSettings.fogColor;
            originalFogMode = RenderSettings.fogMode; originalFogDensity = RenderSettings.fogDensity;
            if (view != null) { background = view.backgroundColor; clearFlags = view.clearFlags; }

            drowningAudioSource = gameObject.AddComponent<AudioSource>();
            drowningAudioSource.loop = true;
            drowningAudioSource.playOnAwake = false;
            drowningAudioSource.spatialBlend = 0f;
        }

        private void OnDisable()
        {
            StopDrowningAudio();
        }

        private void LateUpdate()
        {
            UpdateDrowning();
            if (controller == null || !controller.enabled) return;
            if (limits.extents.sqrMagnitude > 10f && limits.min.x < limits.max.x - 2f)
            {
                Vector3 p = transform.position;
                Vector3 safe = new Vector3(Mathf.Clamp(p.x, limits.min.x + 1, limits.max.x - 1), Mathf.Max(p.y, limits.min.y + 1), Mathf.Clamp(p.z, limits.min.z + 1, limits.max.z - 1));
                if ((safe - p).sqrMagnitude > .00001f)
                {
                    controller.enabled = false; transform.position = safe; controller.enabled = true;
                    movement.ResetGroundedMotion();
                }
            }
            if (view == null) return;
            bool hasWater = waterHeight > -500f;
            bool submerged = hasWater && (view.transform.position.y < waterHeight || transform.position.y < waterHeight + .25f);
            if (submerged == underwater) return;
            underwater = submerged;
            view.clearFlags = underwater ? CameraClearFlags.SolidColor : clearFlags;
            view.backgroundColor = underwater ? new Color(.025f,.30f,.48f) : background;
            RenderSettings.fog = underwater || originalFog;
            RenderSettings.fogColor = underwater ? new Color(.025f,.30f,.48f) : originalFogColor;
            RenderSettings.fogMode = underwater ? FogMode.ExponentialSquared : originalFogMode;
            RenderSettings.fogDensity = underwater ? .12f : originalFogDensity;
        }

        private void UpdateDrowning()
        {
            // Only the robot entering water starts the timer, never the camera.
            bool hasWater = waterHeight > -500f;
            bool inWater = hasWater && (transform.position.y < waterHeight + .25f);
            if (hunt == null || !hunt.IsRoundActive || !inWater)
            {
                IsDrowning = false;
                DrowningTimeRemaining = drowningDuration;
                StopDrowningAudio();
                return;
            }

            IsDrowning = true;
            DrowningTimeRemaining = Mathf.Max(0, DrowningTimeRemaining - Time.deltaTime);

            if (DrowningTimeRemaining > 0f)
            {
                PlayDrowningAudio();
            }
            else
            {
                IsDrowning = false;
                StopDrowningAudio(); // Cut off immediately after exactly 5 seconds!
                hunt.FailFromDrowning();
            }
        }

        private void PlayDrowningAudio()
        {
            if (drowningAudioSource == null) return;
            if (!drowningAudioSource.isPlaying)
            {
                AudioClip clip = Robot.Audio.AudioManager.Instance?.WaterDrowningClip;
                if (clip != null)
                {
                    drowningAudioSource.clip = clip;
                    drowningAudioSource.volume = 0.8f;
                    drowningAudioSource.Play();
                }
            }
        }

        private void StopDrowningAudio()
        {
            if (drowningAudioSource != null && drowningAudioSource.isPlaying)
            {
                drowningAudioSource.Stop();
                drowningAudioSource.clip = null;
            }
        }

        public static void AddMissingSolidColliders(GameObject root)
        {
            foreach (var mesh in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mesh.sharedMesh == null || mesh.GetComponentInParent<CharacterController>() != null) continue;
                bool covered = false;
                foreach (var existing in mesh.GetComponentsInParent<Collider>())
                    if (existing.enabled && !existing.isTrigger) { covered = true; break; }
                if (covered) continue;
                var collider = mesh.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh.sharedMesh;
                collider.convex = mesh.GetComponentInParent<Rigidbody>() != null;
                collider.isTrigger = false;
            }
            foreach (var mesh in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (mesh.GetComponentInParent<CharacterController>() != null || mesh.GetComponentInParent<Collider>() != null) continue;
                var collider = mesh.gameObject.AddComponent<BoxCollider>();
                collider.center = mesh.localBounds.center;
                collider.size = mesh.localBounds.size;
            }
        }
    }
}
