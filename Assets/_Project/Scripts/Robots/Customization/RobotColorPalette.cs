using System;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.Robots.Customization
{
    [CreateAssetMenu(menuName = "Robot Hunt/Robot Color Palette", fileName = "RobotColorPalette")]
    public sealed class RobotColorPalette : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public string displayName;
            public Color bodyColor;
            public Color jointColor;
        }

        [SerializeField] private List<Entry> colors = new List<Entry>();
        public IReadOnlyList<Entry> Colors => colors;
        public int Count => colors.Count;

        public bool TryGet(int index, out Entry entry)
        {
            if (index >= 0 && index < colors.Count) { entry = colors[index]; return true; }
            entry = default;
            return false;
        }

        public int IndexOf(string id)
        {
            return colors.FindIndex(item => string.Equals(item.id, id, StringComparison.OrdinalIgnoreCase));
        }

        public void SetEntries(IEnumerable<Entry> entries)
        {
            colors = new List<Entry>(entries);
        }
    }
}
