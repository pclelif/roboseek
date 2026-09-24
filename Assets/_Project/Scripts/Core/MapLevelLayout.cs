using System.Collections.Generic;
using UnityEngine;

namespace Robot.Core
{
    /// <summary>Scene-authored, validated gameplay locations. Coordinates stay with their terrain.</summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class MapLevelLayout : MonoBehaviour
    {
        public MapType mapId;
        public Transform playerSpawn;
        public Transform[] toySpawns = new Transform[12];
        public Transform[] npcSpawns = new Transform[0];
        public Bounds playableBounds;
        public float fallResetHeight = -12f;

        private void Awake()
        {
            MapManager.SetSelectedMap(mapId);
            Robot.UI.WorldThemeManager.SelectWorldByScene(gameObject.scene.name);
        }

        public List<Vector3> GetToyPositions()
        {
            var positions = new List<Vector3>(toySpawns.Length);
            foreach (var point in toySpawns)
                if (point != null) positions.Add(point.position);
            return positions;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = MapManager.GetMapDefinition(mapId).themeColor;
            Gizmos.DrawWireCube(playableBounds.center, playableBounds.size);
            foreach (var point in toySpawns)
                if (point != null) Gizmos.DrawWireSphere(point.position + Vector3.up * .5f, .5f);
            if (playerSpawn != null) Gizmos.DrawWireCube(playerSpawn.position + Vector3.up, new Vector3(1, 2, 1));
        }
#endif
    }
}
