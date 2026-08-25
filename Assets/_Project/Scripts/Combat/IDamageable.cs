using UnityEngine;

namespace Robot.Combat
{
    public interface IDamageable
    {
        CombatTeam Team { get; }
        bool IsKnockedOut { get; }
        Transform TargetTransform { get; }
        bool TakeDamage(float amount, GameObject instigator);
    }
}
