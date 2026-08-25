using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RobotHunt.Generator.EditorTools
{
    [CustomEditor(typeof(RandomAssetGenerator))]
    public class RandomAssetGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            RandomAssetGenerator generator = (RandomAssetGenerator)target;

            DrawDefaultInspector();

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Utility & Setup Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Auto-Populate Prefabs from Selected Folder", GUILayout.Height(30)))
            {
                PopulatePrefabsFromSelected(generator);
            }

            if (GUILayout.Button("Auto-Find Grass Zones in Scene", GUILayout.Height(25)))
            {
                FindGrassZonesInScene(generator);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Testing Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Assets Now", GUILayout.Height(30)))
            {
                generator.GenerateWorldAssets();
                EditorUtility.SetDirty(generator);
            }

            if (GUILayout.Button("Clear Generated Assets", GUILayout.Height(30)))
            {
                generator.ClearGeneratedAssets();
                EditorUtility.SetDirty(generator);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void PopulatePrefabsFromSelected(RandomAssetGenerator generator)
        {
            Undo.RecordObject(generator, "Auto-Populate Prefabs");

            generator.SetupDefaultCategories();

            string selectedBasePath = "Assets/ThirdParty/Selected";
            if (!Directory.Exists(selectedBasePath))
            {
                Debug.LogError($"[RandomAssetGeneratorEditor] Selected directory not found at path: {selectedBasePath}");
                return;
            }

            int totalLoaded = 0;

            foreach (var catConfig in generator.categories)
            {
                catConfig.prefabs.Clear();
                string subFolderName = catConfig.category.ToString().ToLower();
                string folderPath = Path.Combine(selectedBasePath, subFolderName);

                if (!Directory.Exists(folderPath))
                {
                    Debug.LogWarning($"[RandomAssetGeneratorEditor] Subfolder '{subFolderName}' not found in {selectedBasePath}");
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab != null)
                    {
                        catConfig.prefabs.Add(prefab);
                        totalLoaded++;
                    }
                }

                Debug.Log($"[RandomAssetGeneratorEditor] Loaded {catConfig.prefabs.Count} prefabs for category '{catConfig.categoryName}'.");
            }

            EditorUtility.SetDirty(generator);
            Debug.Log($"[RandomAssetGeneratorEditor] Auto-population complete! Total prefabs loaded across categories: {totalLoaded}.");
        }

        private void FindGrassZonesInScene(RandomAssetGenerator generator)
        {
            Undo.RecordObject(generator, "Auto-Find Grass Zones");

            if (generator.grassZones == null)
            {
                generator.grassZones = new List<Collider>();
            }

            Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            int addedCount = 0;

            foreach (var col in allColliders)
            {
                string nameLower = col.gameObject.name.ToLower();
                if (nameLower.Contains("clearing") || nameLower.Contains("grass") || nameLower.Contains("park"))
                {
                    if (!generator.grassZones.Contains(col))
                    {
                        generator.grassZones.Add(col);
                        addedCount++;
                    }
                }
            }

            EditorUtility.SetDirty(generator);
            Debug.Log($"[RandomAssetGeneratorEditor] Found and added {addedCount} grass zone colliders to generator.");
        }
    }
}
