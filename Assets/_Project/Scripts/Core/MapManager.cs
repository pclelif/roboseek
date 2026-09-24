using System;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.Core
{
    public static class MapManager
    {
        private static readonly List<MapDefinition> maps = new List<MapDefinition>();
        private static int selectedIndex = 0;

        public static event Action<MapDefinition> MapChanged;

        static MapManager()
        {
            InitializeMaps();
        }

        private static void InitializeMaps()
        {
            maps.Clear();

            // 1. CITY (Yellow Theme)
            maps.Add(new MapDefinition(
                MapType.City,
                "CITY",
                new Color(0.94f, 0.72f, 0.10f, 1f), // Yellow #F0B81A
                "Assets/_Project/Scenes/Maps/RoboSeek_City.unity",
                "RoboSeek_City",
                enableCityMechanics: true,
                playerSpawnPosition: new Vector3(0f, 0.1f, -2.5f),
                playerSpawnRotation: Vector3.zero,
                fallbackToyPositions: new List<Vector3>
                {
                    new Vector3(-6f, 0.05f, -12f),
                    new Vector3(8f, 0.05f, -8f),
                    new Vector3(-12f, 0.05f, 6f),
                    new Vector3(10f, 0.05f, 12f),
                    new Vector3(-15f, 0.05f, -18f),
                    new Vector3(14f, 0.05f, -15f),
                    new Vector3(-20f, 0.05f, 15f),
                    new Vector3(18f, 0.05f, 20f),
                    new Vector3(-5f, 0.05f, 22f),
                    new Vector3(22f, 0.05f, -4f),
                    new Vector3(-25f, 0.05f, -8f),
                    new Vector3(6f, 0.05f, 28f)
                }
            ));

            // 2. ADVENTURE (Green Theme)
            maps.Add(new MapDefinition(
                MapType.Adventure,
                "ADVENTURE",
                new Color(0.16f, 0.64f, 0.16f, 1f), // Green #29A329
                "Assets/_Project/Scenes/Maps/RoboSeek_Adventure.unity",
                "RoboSeek_Adventure",
                enableCityMechanics: false,
                playerSpawnPosition: new Vector3(0f, 0.1f, 0f),
                playerSpawnRotation: Vector3.zero,
                fallbackToyPositions: new List<Vector3>
                {
                    new Vector3(-10f, 0.1f, -10f),
                    new Vector3(12f, 0.1f, 15f),
                    new Vector3(-15f, 0.1f, 8f),
                    new Vector3(10f, 0.1f, -12f),
                    new Vector3(-22f, 0.1f, -18f),
                    new Vector3(20f, 0.1f, 22f),
                    new Vector3(-18f, 0.1f, 25f),
                    new Vector3(25f, 0.1f, -16f),
                    new Vector3(0f, 0.1f, 30f),
                    new Vector3(0f, 0.1f, -28f),
                    new Vector3(-28f, 0.1f, 5f),
                    new Vector3(28f, 0.1f, -5f)
                }
            ));

            // 3. TOWN (Red Theme)
            maps.Add(new MapDefinition(
                MapType.Polygon,
                "CASTLE",
                new Color(0.74f, 0.12f, 0.12f, 1f), // Red #BC1F1F
                "Assets/_Project/Scenes/Maps/RoboSeek_Polygon.unity",
                "RoboSeek_Polygon",
                enableCityMechanics: false,
                playerSpawnPosition: new Vector3(0f, 0.1f, 0f),
                playerSpawnRotation: Vector3.zero,
                fallbackToyPositions: new List<Vector3>
                {
                    new Vector3(-8f, 0.1f, -8f),
                    new Vector3(10f, 0.1f, 10f),
                    new Vector3(-12f, 0.1f, 6f),
                    new Vector3(14f, 0.1f, -10f),
                    new Vector3(-18f, 0.1f, -14f),
                    new Vector3(16f, 0.1f, 18f),
                    new Vector3(-20f, 0.1f, 20f),
                    new Vector3(22f, 0.1f, -18f),
                    new Vector3(-5f, 0.1f, 25f),
                    new Vector3(24f, 0.1f, 2f),
                    new Vector3(-24f, 0.1f, -2f),
                    new Vector3(8f, 0.1f, -26f)
                }
            ));

            // 4. ARENA (Blue Theme)
            maps.Add(new MapDefinition(
                MapType.PolygonStarter,
                "POLYGON",
                new Color(0.15f, 0.42f, 0.78f, 1f), // Blue #266BC7
                "Assets/_Project/Scenes/Maps/RoboSeek_PolygonStarter.unity",
                "RoboSeek_PolygonStarter",
                enableCityMechanics: false,
                playerSpawnPosition: new Vector3(0f, 0.1f, 0f),
                playerSpawnRotation: Vector3.zero,
                fallbackToyPositions: new List<Vector3>
                {
                    new Vector3(-10f, 0.1f, -5f),
                    new Vector3(8f, 0.1f, 12f),
                    new Vector3(-14f, 0.1f, 10f),
                    new Vector3(12f, 0.1f, -8f),
                    new Vector3(-20f, 0.1f, -15f),
                    new Vector3(18f, 0.1f, 20f),
                    new Vector3(-16f, 0.1f, 22f),
                    new Vector3(22f, 0.1f, -14f),
                    new Vector3(0f, 0.1f, 25f),
                    new Vector3(0f, 0.1f, -25f),
                    new Vector3(-25f, 0.1f, 0f),
                    new Vector3(25f, 0.1f, 0f)
                }
            ));
        }

        public static IReadOnlyList<MapDefinition> AllMaps => maps;

        public static MapDefinition SelectedMap
        {
            get
            {
                if (maps.Count == 0) InitializeMaps();
                if (selectedIndex < 0 || selectedIndex >= maps.Count) selectedIndex = 0;
                return maps[selectedIndex];
            }
        }

        public static bool SelectMapForScene(string sceneName)
        {
            for (int i = 0; i < maps.Count; i++)
                if (maps[i].sceneName == sceneName)
                {
                    if (selectedIndex != i) SelectMap(i);
                    return true;
                }
            return false;
        }

        public static int SelectedIndex => selectedIndex;

        public static void ResetToDefaultMap()
        {
            if (maps.Count == 0) InitializeMaps();
            selectedIndex = 0;
            MapChanged?.Invoke(maps[0]);
        }

        public static void SelectMap(int index)
        {
            if (maps.Count == 0) InitializeMaps();
            if (index < 0 || index >= maps.Count) index = 0;
            selectedIndex = index;
            MapDefinition current = SelectedMap;
            Debug.Log($"[MapManager] Selected Map changed to: {current.displayName} ({current.sceneName})");
            MapChanged?.Invoke(current);
        }

        public static MapDefinition SelectNextMap()
        {
            if (maps.Count == 0) InitializeMaps();
            selectedIndex = (selectedIndex + 1) % maps.Count;
            MapDefinition current = SelectedMap;
            Debug.Log($"[MapManager] Selected Map changed to: {current.displayName} ({current.sceneName})");
            MapChanged?.Invoke(current);
            return current;
        }

        public static void SetSelectedMap(MapType mapType)
        {
            if (maps.Count == 0) InitializeMaps();
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].mapId == mapType)
                {
                    selectedIndex = i;
                    MapChanged?.Invoke(maps[i]);
                    return;
                }
            }
        }

        public static MapDefinition GetMapDefinition(MapType mapType)
        {
            if (maps.Count == 0) InitializeMaps();
            foreach (var map in maps)
            {
                if (map.mapId == mapType) return map;
            }
            return SelectedMap;
        }
    }
}
