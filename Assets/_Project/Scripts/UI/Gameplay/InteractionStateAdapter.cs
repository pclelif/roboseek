using System;
using UnityEngine;
using Robot.ObjectHunt;
namespace Robot.UI.Production
{
    public sealed class InteractionStateAdapter : MonoBehaviour
    {
        public ObjectHuntRoundManager hunt;
        public UIStateManager state;
        public event Action<CollectibleTarget> Changed;
        public event Action Retrieved;
        public CollectibleTarget Current { get; private set; }
        private int successFrame = -1;
        private void OnEnable() { hunt.InteractionSucceeded += OnSuccess; state.Changed += OnState; }
        private void OnDisable() { hunt.InteractionSucceeded -= OnSuccess; state.Changed -= OnState; Set(null); }
        private void OnState() { if (!state.IsGameplay) Set(null); }
        private void LateUpdate()
        {
            // Adapt the existing query; no UI raycast, overlap, range or pickup implementation.
            var health = state.inputGate.movement.GetComponent<Robot.Combat.CombatHealth>();
            Set((health == null || !health.IsKnockedOut) && state.IsGameplay && hunt.IsRoundActive && hunt.InteractionEnabled && successFrame != Time.frameCount
                ? hunt.GetNearestInteractable() : null);
        }
        private void Set(CollectibleTarget target)
        {
            if (Current == target) return;
            Current = target; Changed?.Invoke(target);
        }
        private void OnSuccess() { successFrame = Time.frameCount; Set(null); Retrieved?.Invoke(); }
    }
}
