using System;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.Robots.Customization
{
    [DisallowMultipleComponent]
    public sealed class RobotColorService : MonoBehaviour
    {
        public const string PlayerPreferenceKey = "RobotHunt.PlayerColor";
        [SerializeField] private RobotColorPalette palette;
        private readonly Dictionary<ulong, int> reservations = new Dictionary<ulong, int>();

        public static RobotColorService Instance { get; private set; }
        public RobotColorPalette Palette => palette;
        public event Action ReservationsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Configure(RobotColorPalette value) => palette = value;

        public bool TryReserve(ulong ownerId, int requestedIndex, out int assignedIndex, out string message)
        {
            Release(ownerId);
            if (IsValidAndFree(requestedIndex))
            {
                reservations[ownerId] = requestedIndex;
                assignedIndex = requestedIndex;
                message = string.Empty;
                ReservationsChanged?.Invoke();
                return true;
            }

            string requestedName = GetDisplayName(requestedIndex);
            assignedIndex = FindFirstFree();
            if (assignedIndex >= 0)
            {
                reservations[ownerId] = assignedIndex;
                message = requestedIndex >= 0 ? $"{requestedName} is already taken" : "Requested color is unavailable";
                ReservationsChanged?.Invoke();
                return false;
            }

            message = "No robot colors are available";
            return false;
        }

        public void Release(ulong ownerId)
        {
            if (reservations.Remove(ownerId)) ReservationsChanged?.Invoke();
        }

        public bool IsTaken(int index) => reservations.ContainsValue(index);
        public int GetReservation(ulong ownerId) => reservations.TryGetValue(ownerId, out int index) ? index : -1;
        public int LoadSinglePlayerSelection() => palette != null ? Mathf.Clamp(PlayerPrefs.GetInt(PlayerPreferenceKey, 0), 0, Mathf.Max(0, palette.Count - 1)) : 0;

        public void SaveSinglePlayerSelection(int index)
        {
            if (palette == null || index < 0 || index >= palette.Count) return;
            PlayerPrefs.SetInt(PlayerPreferenceKey, index);
            PlayerPrefs.Save();
        }

        public IReadOnlyList<int> GetAvailableIndices(int excludedIndex = -1)
        {
            var result = new List<int>();
            if (palette == null) return result;
            for (int i = 0; i < palette.Count; i++) if (i != excludedIndex && !IsTaken(i)) result.Add(i);
            return result;
        }

        private bool IsValidAndFree(int index) => palette != null && index >= 0 && index < palette.Count && !IsTaken(index);
        private int FindFirstFree()
        {
            if (palette == null) return -1;
            for (int i = 0; i < palette.Count; i++) if (!IsTaken(i)) return i;
            return -1;
        }
        private string GetDisplayName(int index) => palette != null && palette.TryGet(index, out RobotColorPalette.Entry entry) ? entry.displayName : "Requested color";
    }
}
