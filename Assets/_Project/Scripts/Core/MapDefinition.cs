using System;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.Core
{
    public enum MapType
    {
        City,
        Adventure,
        Polygon,
        PolygonStarter
    }

    public enum MapTraversal { Standard, Jetpack, DoubleJump, Dash }

    [Serializable]
    public class MapDefinition
    {
        public MapType mapId;
        // Preserve serialized map IDs/scene paths: Polygon is the medieval town,
        // PolygonStarter is the blue vertical arena.
        public MapTraversal Traversal => mapId == MapType.PolygonStarter ? MapTraversal.Jetpack :
            mapId == MapType.Adventure ? MapTraversal.DoubleJump :
            mapId == MapType.Polygon ? MapTraversal.Dash : MapTraversal.Standard;
        public string displayName;
        public Color themeColor;
        public string scenePath;
        public string sceneName;
        public bool enableCityMechanics;
        public Vector3 playerSpawnPosition;
        public Vector3 playerSpawnRotation;
        public List<Vector3> fallbackToyPositions = new List<Vector3>();

        public MapDefinition(
            MapType mapId,
            string displayName,
            Color themeColor,
            string scenePath,
            string sceneName,
            bool enableCityMechanics,
            Vector3 playerSpawnPosition,
            Vector3 playerSpawnRotation,
            List<Vector3> fallbackToyPositions)
        {
            this.mapId = mapId;
            this.displayName = displayName;
            this.themeColor = themeColor;
            this.scenePath = scenePath;
            this.sceneName = sceneName;
            this.enableCityMechanics = enableCityMechanics;
            this.playerSpawnPosition = playerSpawnPosition;
            this.playerSpawnRotation = playerSpawnRotation;
            this.fallbackToyPositions = fallbackToyPositions ?? new List<Vector3>();
        }
    }
}
