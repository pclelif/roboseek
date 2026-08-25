using UnityEngine;
using UnityEngine.AI;
using Robot.Combat;
using Robot.Player;
using Robot.Input;

namespace Robot.NPC
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(CombatHealth), typeof(CombatAttack))]
    public sealed class NpcRobotController : MonoBehaviour
    {
        private enum State { Idle, Wander, React, Chase, Attack, Knockout }

        [Header("Wander")]
        [SerializeField, Min(0f)] private float wanderRadius = 18f;
        [SerializeField] private Vector2 idleTimeRange = new Vector2(1.5f, 4f);
        [SerializeField, Min(0.1f)] private float walkSpeed = 2.6f;

        [Header("Aggression")]
        [SerializeField, Min(0f)] private float aggressionRadius = 30f;
        [SerializeField, Range(30f, 360f)] private float fieldOfViewAngle = 160f;
        [Tooltip("Close-range 360 degree awareness so approaching directly behind an NPC is not risk-free.")]
        [SerializeField, Min(0f)] private float closeAwarenessRadius = 30f;
        [SerializeField, Min(0f)] private float eyeHeight = 1.35f;
        [SerializeField] private LayerMask sightBlockingLayers = ~0;
        [SerializeField, Min(0f)] private float loseTargetDistance = 24f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 5.8f;
        [Tooltip("Used while the target player is sprinting. Must exceed player sprint speed so a chase eventually closes distance.")]
        [SerializeField, Min(0.1f)] private float sprintChaseSpeed = 8.2f;
        [SerializeField] private Vector2 reactionTimeRange = new Vector2(0.05f, 0.12f);
        [SerializeField] private AggressionZone[] aggressionZones;

        private NavMeshAgent agent;
        private CombatHealth health;
        private CombatAttack attack;
        private RobotAnimator animationDriver;
        private Transform target;
        private CombatHealth targetHealth;
        private PlayerInputReader targetInput;
        private AttackSlotCoordinator slots;
        private Vector3 home;
        private State state;
        private float stateUntil;
        private bool ownsAttackSlot;
        private readonly RaycastHit[] sightHits = new RaycastHit[24];

        public void Configure(Transform player, AttackSlotCoordinator coordinator, AggressionZone[] zones)
        {
            target = player;
            targetHealth = player != null ? player.GetComponent<CombatHealth>() : null;
            targetInput = player != null ? player.GetComponent<PlayerInputReader>() : null;
            slots = coordinator;
            aggressionZones = zones;
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<CombatHealth>();
            attack = GetComponent<CombatAttack>();
            animationDriver = GetComponent<RobotAnimator>();
            home = transform.position;
            agent.speed = walkSpeed;
            agent.stoppingDistance = Mathf.Max(0.1f, attack.AttackRange * 0.8f);
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        private void OnEnable()
        {
            if (health == null) health = GetComponent<CombatHealth>();
            health.KnockedOut += HandleKnockout;
            health.Recovered += HandleRecovery;
        }

        private void Start()
        {
            ResolveTargetIfMissing();
            EnterIdle();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.KnockedOut -= HandleKnockout;
                health.Recovered -= HandleRecovery;
            }
            ReleaseSlot();
        }

        private void Update()
        {
            if (state == State.Knockout) return;
            if (target == null) ResolveTargetIfMissing();
            animationDriver?.SetLocomotion(agent.velocity.magnitude / Mathf.Max(0.01f, chaseSpeed));

            bool detectsTarget = CanDetectTarget();
            if ((state == State.Idle || state == State.Wander) && detectsTarget) EnterReaction();

            switch (state)
            {
                case State.Idle: UpdateIdle(); break;
                case State.Wander: UpdateWander(); break;
                case State.React: UpdateReaction(); break;
                case State.Chase: UpdateChase(); break;
                case State.Attack: UpdateAttack(); break;
            }
        }

        private void ResolveTargetIfMissing()
        {
            if (target != null) return;
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null) return;
            target = playerObject.transform;
            targetHealth = playerObject.GetComponent<CombatHealth>();
            targetInput = playerObject.GetComponent<PlayerInputReader>();
        }

        private void UpdateIdle()
        {
            if (Time.time >= stateUntil) PickWanderPoint();
        }

        private void UpdateWander()
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.15f) EnterIdle();
        }

        private void UpdateReaction()
        {
            if (target == null || targetHealth == null || targetHealth.IsKnockedOut ||
                FlatDistance(target.position) > loseTargetDistance || (!HasLineOfSight() && !HasCloseAwareness()))
            {
                ReturnToWander();
                return;
            }
            FaceTarget();
            if (Time.time >= stateUntil)
            {
                if (!TryEnterChase()) ReturnToWander();
            }
        }

        private void UpdateChase()
        {
            if (ShouldGiveUp()) { ReturnToWander(); return; }
            agent.speed = targetInput != null && targetInput.RunHeld ? sprintChaseSpeed : chaseSpeed;
            float distance = FlatDistance(target.position);
            if (distance <= attack.AttackRange)
            {
                agent.isStopped = true;
                if (slots == null || slots.TryAcquire(this))
                {
                    ownsAttackSlot = slots != null;
                    state = State.Attack;
                }
                return;
            }
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }

        private void UpdateAttack()
        {
            if (ShouldGiveUp()) { ReturnToWander(); return; }
            float distance = FlatDistance(target.position);
            if (distance > attack.AttackRange)
            {
                ReleaseSlot();
                EnterChase();
                return;
            }
            FaceTarget();
            attack.TryAttack(target);
        }

        private bool CanDetectTarget()
        {
            if (target == null || targetHealth == null || targetHealth.IsKnockedOut) return false;
            float targetDistance = FlatDistance(target.position);
            if (targetDistance > aggressionRadius) return false;
            if (targetDistance <= closeAwarenessRadius && HasCloseAwareness()) return true;
            if (HasLineOfSight() && IsInsideFieldOfView()) return true;
            if (aggressionZones != null && aggressionZones.Length > 0)
            {
                foreach (AggressionZone zone in aggressionZones)
                    if (zone != null && zone.Contains(target.position) && zone.Contains(transform.position) && HasLineOfSight()) return true;
            }
            return false;
        }

        private bool IsInsideFieldOfView()
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.001f) return true;
            return Vector3.Angle(transform.forward, toTarget) <= fieldOfViewAngle * 0.5f;
        }

        private bool HasLineOfSight()
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 destination = target.position + Vector3.up;
            Vector3 direction = destination - origin;
            int count = Physics.RaycastNonAlloc(origin, direction.normalized, sightHits, direction.magnitude,
                sightBlockingLayers, QueryTriggerInteraction.Ignore);
            RaycastHit nearest = default;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Transform hitTransform = sightHits[i].transform;
                if (hitTransform == null || hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
                if (sightHits[i].distance >= nearestDistance) continue;
                nearest = sightHits[i];
                nearestDistance = sightHits[i].distance;
            }
            if (nearestDistance == float.MaxValue) return true;
            return nearest.transform == target || nearest.transform.IsChildOf(target);
        }

        private bool HasCloseAwareness()
        {
            if (target == null || FlatDistance(target.position) > closeAwarenessRadius || !agent.isOnNavMesh || !HasLineOfSight()) return false;
            if (!NavMesh.SamplePosition(target.position, out NavMeshHit targetHit, 2.5f, NavMesh.AllAreas)) return false;
            var path = new NavMeshPath();
            if (!agent.CalculatePath(targetHit.position, path) || path.status != NavMeshPathStatus.PathComplete) return false;
            float pathLength = 0f;
            for (int i = 1; i < path.corners.Length; i++) pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return pathLength <= closeAwarenessRadius * 1.35f;
        }

        private bool ShouldGiveUp()
        {
            if (target == null || targetHealth == null || targetHealth.IsKnockedOut) return true;
            return !CanDetectTarget() && FlatDistance(target.position) > loseTargetDistance;
        }

        private void PickWanderPoint()
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 circle = Random.insideUnitCircle * wanderRadius;
                Vector3 candidate = home + new Vector3(circle.x, 0f, circle.y);
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas)) continue;
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.SetDestination(hit.position);
                state = State.Wander;
                return;
            }
            EnterIdle();
        }

        private void EnterIdle()
        {
            state = State.Idle;
            agent.isStopped = true;
            stateUntil = Time.time + Random.Range(idleTimeRange.x, idleTimeRange.y);
        }

        private void EnterChase()
        {
            state = State.Chase;
            agent.speed = chaseSpeed;
            agent.isStopped = false;
        }

        private bool TryEnterChase()
        {
            EnterChase();
            return true;
        }

        private void EnterReaction()
        {
            state = State.React;
            agent.isStopped = true;
            stateUntil = Time.time + Random.Range(reactionTimeRange.x, reactionTimeRange.y);
        }

        private void ReturnToWander()
        {
            ReleaseSlot();
            agent.speed = walkSpeed;
            EnterIdle();
        }

        private void HandleKnockout(CombatHealth _)
        {
            state = State.Knockout;
            ReleaseSlot();
            if (agent.isOnNavMesh) agent.isStopped = true;
            if (!health.RecoverAfterKnockout && agent.enabled) agent.enabled = false;
        }

        private void HandleRecovery(CombatHealth _)
        {
            if (!agent.enabled) agent.enabled = true;
            if (agent.isOnNavMesh) agent.isStopped = false;
            EnterIdle();
        }

        private void ReleaseSlot()
        {
            if (ownsAttackSlot && slots != null) slots.Release(this);
            ownsAttackSlot = false;
        }

        private float FlatDistance(Vector3 point)
        {
            Vector3 difference = point - transform.position;
            difference.y = 0f;
            return difference.magnitude;
        }

        private void FaceTarget()
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 540f * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, aggressionRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Application.isPlaying ? home : transform.position, wanderRadius);
        }
    }
}
