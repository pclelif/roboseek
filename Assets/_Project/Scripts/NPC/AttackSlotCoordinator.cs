using System.Collections.Generic;
using UnityEngine;

namespace Robot.NPC
{
    [DisallowMultipleComponent]
    public sealed class AttackSlotCoordinator : MonoBehaviour
    {
        [Tooltip("Maximum NPCs allowed in the attack state at the same time. Chasing does not consume a slot.")]
        [SerializeField, Min(1)] private int maxActiveAttackers = 2;
        private readonly HashSet<NpcRobotController> holders = new HashSet<NpcRobotController>();

        public int MaxActiveAttackers => maxActiveAttackers;

        public bool TryAcquire(NpcRobotController npc)
        {
            holders.RemoveWhere(item => item == null || !item.isActiveAndEnabled);
            if (holders.Contains(npc)) return true;
            if (holders.Count >= maxActiveAttackers) return false;
            holders.Add(npc);
            return true;
        }

        public void Release(NpcRobotController npc) => holders.Remove(npc);
    }
}
