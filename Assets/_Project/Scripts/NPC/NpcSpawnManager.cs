using System;
using System.Collections.Generic;
using UnityEngine;
using Robot.Combat;
using Robot.Robots.Customization;

namespace Robot.NPC
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AttackSlotCoordinator))]
    public sealed class NpcSpawnManager : MonoBehaviour
    {
        [SerializeField, Min(0)] private int npcCount = 9;
        [SerializeField] private NpcRobotController npcPrefab;
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private AggressionZone[] aggressionZones;
        [SerializeField, Min(0f)] private float minimumPlayerSpawnDistance = 12f;

        private readonly List<NpcRobotController> spawned = new List<NpcRobotController>();
        private AttackSlotCoordinator slots;
        private RobotColorCustomizer playerColors;

        private void Awake() => slots = GetComponent<AttackSlotCoordinator>();

        private void Start()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) player = playerObject.transform;
            }
            if (player != null)
            {
                playerColors = player.GetComponent<RobotColorCustomizer>();
                if (playerColors != null) playerColors.ThemeChanged += HandlePlayerThemeChanged;
            }
            SpawnAll();
        }

        private void OnDestroy()
        {
            if (playerColors != null) playerColors.ThemeChanged -= HandlePlayerThemeChanged;
        }

        public void SpawnAll()
        {
            if (npcPrefab == null || player == null) return;
            List<Transform> validPoints = GetValidSpawnPoints();
            int count = Mathf.Min(npcCount, validPoints.Count);
            for (int i = 0; i < count; i++)
            {
                NpcRobotController npc = Instantiate(npcPrefab, validPoints[i].position, validPoints[i].rotation, transform);
                npc.name = $"NPC Robot {i + 1:00}";
                npc.Configure(player, slots, aggressionZones);
                CombatHealth health = npc.GetComponent<CombatHealth>();
                if (health != null) health.SetTeam(CombatTeam.Npc);
                spawned.Add(npc);
            }
            AssignUniqueNpcColors();
        }

        private List<Transform> GetValidSpawnPoints()
        {
            var result = new List<Transform>();
            if (spawnPoints == null) return result;
            foreach (Transform point in spawnPoints)
                if (point != null && Vector3.Distance(point.position, player.position) >= minimumPlayerSpawnDistance) result.Add(point);
            return result;
        }

        private void HandlePlayerThemeChanged(RobotColorCustomizer.ColorTheme _) => AssignUniqueNpcColors();

        private void AssignUniqueNpcColors()
        {
            RobotColorCustomizer.ColorTheme excluded = playerColors != null ? playerColors.ActiveTheme : RobotColorCustomizer.ColorTheme.Siyah;
            var available = new List<RobotColorCustomizer.ColorTheme>((RobotColorCustomizer.ColorTheme[])Enum.GetValues(typeof(RobotColorCustomizer.ColorTheme)));
            available.Remove(excluded);
            for (int i = available.Count - 1; i > 0; i--)
            {
                int swap = UnityEngine.Random.Range(0, i + 1);
                (available[i], available[swap]) = (available[swap], available[i]);
            }
            for (int i = 0; i < spawned.Count && i < available.Count; i++)
            {
                RobotColorCustomizer colors = spawned[i].GetComponent<RobotColorCustomizer>();
                if (colors != null) colors.ApplyTheme(available[i]);
            }
        }
    }
}
