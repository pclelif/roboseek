using System;
using UnityEngine;

namespace Robot.ObjectHunt
{
    public enum TargetCategory { Ball, TeddyBear, ToyCar }

    [Serializable]
    public sealed class TargetDefinition
    {
        public string objectId;
        public TargetCategory category;
        public string displayName;
        public GameObject prefab;
        public Sprite icon;
        [Min(0f)] public float groundOffset;
        [Min(0.1f)] public float worldScale = 1f;
        [Min(0.1f)] public float interactionRange = 2.2f;
    }
}
