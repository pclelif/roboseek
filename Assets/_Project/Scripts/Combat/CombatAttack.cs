using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatHealth))]
    public sealed class CombatAttack : MonoBehaviour
    {
        [Header("Attack")]
        [SerializeField, Min(0f)] private float damage = 30f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.65f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.9f;
        [SerializeField, Min(0f)] private float hitWindowDelay = 0.28f;
        [SerializeField, Min(0.01f)] private float hitRadius = 0.65f;
        [SerializeField] private Vector3 hitCenter = new Vector3(0f, 0.9f, 0.9f);
        [SerializeField] private LayerMask hittableLayers = ~0;
        [SerializeField] private bool allowFriendlyFire;

        private readonly Collider[] hitBuffer = new Collider[24];
        private readonly HashSet<IDamageable> damagedThisSwing = new HashSet<IDamageable>();
        private CombatHealth health;
        private Robot.Player.RobotAnimator animationDriver;
        private float nextAttackTime;
        private bool isAttacking;

        public float Damage => damage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public bool IsAttacking => isAttacking;
        public bool CanAttack => !isAttacking && !health.IsKnockedOut && Time.time >= nextAttackTime;

        private void Awake()
        {
            health = GetComponent<CombatHealth>();
            animationDriver = GetComponent<Robot.Player.RobotAnimator>();
        }

        public bool TryAttack(Transform intendedTarget = null)
        {
            if (!CanAttack) return false;
            if (intendedTarget != null && HorizontalDistance(intendedTarget.position) > attackRange) return false;
            StartCoroutine(AttackRoutine(intendedTarget));
            return true;
        }

        private IEnumerator AttackRoutine(Transform intendedTarget)
        {
            isAttacking = true;
            nextAttackTime = Time.time + attackCooldown;
            animationDriver?.PlayAttack();
            yield return new WaitForSeconds(hitWindowDelay);
            if (!health.IsKnockedOut) ApplyHit(intendedTarget);
            yield return new WaitForSeconds(Mathf.Max(0f, attackCooldown - hitWindowDelay));
            isAttacking = false;
        }

        private void ApplyHit(Transform intendedTarget)
        {
            damagedThisSwing.Clear();
            Vector3 worldCenter = transform.TransformPoint(hitCenter);
            int count = Physics.OverlapSphereNonAlloc(worldCenter, hitRadius, hitBuffer, hittableLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                IDamageable target = FindDamageable(hitBuffer[i]);
                if (target == null || target.TargetTransform == transform || target.IsKnockedOut || damagedThisSwing.Contains(target)) continue;
                if (!allowFriendlyFire && target.Team == health.Team) continue;
                if (intendedTarget != null && target.TargetTransform != intendedTarget && !target.TargetTransform.IsChildOf(intendedTarget)) continue;
                if (HorizontalDistance(target.TargetTransform.position) > attackRange + hitRadius) continue;
                if (target.TakeDamage(damage, gameObject)) damagedThisSwing.Add(target);
            }
        }

        private static IDamageable FindDamageable(Collider source)
        {
            MonoBehaviour[] candidates = source.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour candidate in candidates)
                if (candidate is IDamageable damageable) return damageable;
            return null;
        }

        private float HorizontalDistance(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.TransformPoint(hitCenter), hitRadius);
        }
    }
}
