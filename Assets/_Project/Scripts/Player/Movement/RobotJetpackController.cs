using UnityEngine;

namespace Robot.Player.Movement
{
    /// <summary>Fuel/thrust is integrated by locomotion, before its single CharacterController.Move.</summary>
    [RequireComponent(typeof(RobotMovementController))]
    public sealed class RobotJetpackController : MonoBehaviour
    {
        [SerializeField, Min(.5f)] private float maxFuelDuration = 4.5f;
        [SerializeField, Min(0f)] private float fuelRechargeRate = 2.25f;
        [SerializeField] private float maxUpwardVelocity = 9f;
        [SerializeField] private float thrustAcceleration = 38f;
        [SerializeField] private float glideFallSpeed = 4.4f;
        private RobotMovementController movement;
        private ParticleSystem particles;
        private bool fuelLocked;
        public float CurrentFuel { get; private set; }
        public float FuelNormalized => CurrentFuel / Mathf.Max(.01f, maxFuelDuration);
        public bool IsFlying { get; private set; }
        public bool IsGliding { get; private set; }

        private void Awake()
        {
            movement = GetComponent<RobotMovementController>();
            Refuel();
            var exhaust = new GameObject("JetpackExhaust");
            exhaust.transform.SetParent(transform, false);
            exhaust.transform.localPosition = new Vector3(0, .65f, -.25f);
            exhaust.transform.localRotation = Quaternion.Euler(90, 0, 0);
            particles = exhaust.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.startLifetime = .22f;
            main.startSpeed = 3.5f;
            main.startSize = .13f;
            main.startColor = new Color(.1f, .75f, 1f);
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission; emission.rateOverTime = 45;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 10; shape.radius = .08f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) renderer.material = new Material(shader);
        }

        public void Simulate(float dt, bool grounded, bool jumpHeld, ref float verticalVelocity)
        {
            if (!movement.CanUseTraversal || dt <= 0) { StopThrust(); return; }
            if (grounded)
            {
                CurrentFuel = Mathf.Min(maxFuelDuration, CurrentFuel + fuelRechargeRate * dt);
                if (CurrentFuel >= .6f) fuelLocked = false;
            }
            bool thrust = jumpHeld && !fuelLocked && CurrentFuel > 0;
            IsFlying = thrust;
            IsGliding = !grounded && !thrust;
            if (thrust)
            {
                float poweredDt = Mathf.Min(dt, CurrentFuel);
                CurrentFuel = Mathf.Max(0, CurrentFuel - dt);
                verticalVelocity = Mathf.MoveTowards(verticalVelocity, maxUpwardVelocity, thrustAcceleration * poweredDt);
                if (CurrentFuel <= 0) fuelLocked = true;
                if (particles != null && !particles.isPlaying) particles.Play();
            }
            else
            {
                if (IsGliding) verticalVelocity = Mathf.Max(verticalVelocity, -glideFallSpeed);
                if (particles != null && particles.isPlaying) particles.Stop();
            }
        }

        public void Refuel() { CurrentFuel = maxFuelDuration; fuelLocked = false; StopThrust(); }
        public void StopThrust()
        {
            IsFlying = IsGliding = false;
            if (particles != null && particles.isPlaying) particles.Stop();
        }
        public void ApplyRingLaunch(float force, Vector3 direction)
        {
            if (movement.Launch(Vector3.up * force)) Refuel();
        }
        private void OnDisable() => StopThrust();
        private void OnDestroy()
        {
            if (particles != null) Destroy(particles.GetComponent<ParticleSystemRenderer>().sharedMaterial);
        }
    }
}
