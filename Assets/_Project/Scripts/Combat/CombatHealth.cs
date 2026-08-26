using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Robot.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatHealth : MonoBehaviour, IDamageable
    {
        [Header("Identity")]
        [SerializeField] private CombatTeam team = CombatTeam.Neutral;

        [Header("Health / Knockout")]
        [SerializeField, Min(1f)] private float maxHealth = 200f;
        [SerializeField, Min(0f)] private float knockoutDuration = 60f;
        [SerializeField] private bool recoverAtFullHealth = true;
        [Tooltip("Disable for NPCs that remain permanently knocked out.")]
        [SerializeField] private bool recoverAfterKnockout = true;

        [Header("Optional callbacks")]
        [SerializeField] private UnityEvent onKnockout;
        [SerializeField] private UnityEvent onRecovered;

        private Coroutine recoveryRoutine;

        public event Action<CombatHealth, float, GameObject> Damaged;
        public event Action<CombatHealth> KnockedOut;
        public event Action<CombatHealth> Recovered;

        public CombatTeam Team => team;
        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float KnockoutDuration => knockoutDuration;
        public bool IsKnockedOut { get; private set; }
        public float KnockoutTimeRemaining { get; private set; }
        public bool RecoverAfterKnockout => recoverAfterKnockout;
        public Transform TargetTransform => transform;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public bool TakeDamage(float amount, GameObject instigator)
        {
            if (amount <= 0f || IsKnockedOut) return false;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            Damaged?.Invoke(this, amount, instigator);
            if (CurrentHealth <= 0f) BeginKnockout();
            return true;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsKnockedOut) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        public void SetTeam(CombatTeam value) => team = value;

        private void BeginKnockout()
        {
            if (IsKnockedOut) return;
            IsKnockedOut = true;
            KnockedOut?.Invoke(this);
            onKnockout?.Invoke();
            if (recoveryRoutine != null) StopCoroutine(recoveryRoutine);
            if (recoverAfterKnockout) recoveryRoutine = StartCoroutine(RecoverRoutine());
        }

        private IEnumerator RecoverRoutine()
        {
            KnockoutTimeRemaining = knockoutDuration;
            while (KnockoutTimeRemaining > 0f)
            {
                KnockoutTimeRemaining = Mathf.Max(0f, KnockoutTimeRemaining - Time.deltaTime);
                yield return null;
            }
            CurrentHealth = recoverAtFullHealth ? maxHealth : Mathf.Max(1f, maxHealth * 0.5f);
            IsKnockedOut = false;
            recoveryRoutine = null;
            Recovered?.Invoke(this);
            onRecovered?.Invoke();
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            knockoutDuration = Mathf.Max(0f, knockoutDuration);
            if (!Application.isPlaying) CurrentHealth = maxHealth;
        }
    }
}
