using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RobotHunt.Generator
{
    public enum AssetCategory
    {
        Toy
    }

    [Serializable]
    public class AssetCategoryConfig
    {
        public string categoryName;
        public AssetCategory category;
        public List<GameObject> prefabs = new List<GameObject>();
        
        [Tooltip("How many instances to spawn for EACH prefab in this category's pool (default = 1).")]
        public int instancesPerPrefab = 1;
        
        [Tooltip("If true, this category MUST strictly spawn on Grass surfaces/zones only. If false, spawns OUTSIDE grass surfaces.")]
        public bool requireGrassZone = false;
        
        public float minSpacing = 1.5f;
        public Vector2 scaleRange = new Vector2(0.95f, 1.05f);
        public Vector2 yRotationRange = new Vector2(0f, 360f);

        public AssetCategoryConfig(AssetCategory cat, bool grassRequired, int defaultInstancesPerPrefab, float spacing)
        {
            category = cat;
            categoryName = cat.ToString();
            requireGrassZone = grassRequired;
            instancesPerPrefab = defaultInstancesPerPrefab;
            minSpacing = spacing;
            prefabs = new List<GameObject>();
            scaleRange = new Vector2(0.95f, 1.05f);
            yRotationRange = new Vector2(0f, 360f);
        }
    }

    public class RandomAssetGenerator : MonoBehaviour
    {
        [Header("Generator Settings")]
        [Tooltip("Seed for random generator. Use -1 for random seed on every Play.")]
        public int randomSeed = -1;

        [Tooltip("If true, automatically spawns assets on Start().")]
        public bool generateOnStart = true;

        [Tooltip("Maximum placement attempts per object before skipping.")]
        public int maxPlacementAttempts = 200;

        [Header("Placement & Zone Constraints")]
        [Tooltip("List of colliders defining Grass / Park zones.")]
        public List<Collider> grassZones = new List<Collider>();

        [Tooltip("Layer mask for valid ground surface.")]
        public LayerMask groundLayerMask = ~0;

        [Tooltip("Layer mask for obstacles, walls, and buildings to avoid overlapping.")]
        public LayerMask obstacleLayerMask = ~0;

        [Tooltip("Maximum Y coordinate for valid street-level ground surface. Prevents spawning on roofs, balconies, canopies, or high building ledges.")]
        public float maxWalkableGroundHeight = 0.8f;

        [Tooltip("Raycast starting height above ground.")]
        public float sampleRaycastHeight = 50.0f;

        [Tooltip("Transform parent to hold all spawned objects.")]
        public Transform spawnedObjectsParent;

        [Header("Asset Categories")]
        public List<AssetCategoryConfig> categories = new List<AssetCategoryConfig>();

        private struct PlacedObjectInfo
        {
            public Vector3 position;
            public float radius;
        }

        private void Reset()
        {
            SetupDefaultCategories();
            AutoPopulateCategoriesIfEmpty();
            AutoFindGrassZonesInScene();
        }

        public void SetupDefaultCategories()
        {
            categories = new List<AssetCategoryConfig>
            {
                new AssetCategoryConfig(AssetCategory.Toy, false, 1, 1.5f)
            };
        }

        public void AutoFindGrassZonesInScene()
        {
            if (grassZones == null) grassZones = new List<Collider>();
            grassZones.RemoveAll(z => z == null || !z);

            Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            foreach (var col in allColliders)
            {
                if (col == null || !col.enabled || col.isTrigger) continue;
                
                string goName = col.gameObject.name.ToLower();
                string tagStr = col.gameObject.tag.ToLower();

                if (goName.Contains("grass") || goName.Contains("clearing") || goName.Contains("park") || goName.Contains("lawn") ||
                    tagStr.Contains("grass") || tagStr.Contains("park"))
                {
                    if (!grassZones.Contains(col))
                    {
                        grassZones.Add(col);
                    }
                }
            }
        }

        public void AutoPopulateCategoriesIfEmpty()
        {
            if (categories == null || categories.Count == 0)
            {
                SetupDefaultCategories();
            }

            categories.RemoveAll(c => c == null || c.category != AssetCategory.Toy);

            if (categories.Count == 0)
            {
                categories.Add(new AssetCategoryConfig(AssetCategory.Toy, false, 1, 1.5f));
            }

#if UNITY_EDITOR
            string selectedBasePath = "Assets/ThirdParty/Selected";
            if (Directory.Exists(selectedBasePath))
            {
                int totalLoaded = 0;
                foreach (var catConfig in categories)
                {
                    if (catConfig.prefabs == null) catConfig.prefabs = new List<GameObject>();
                    catConfig.prefabs.RemoveAll(p => p == null || !p);

                    string subFolderName = catConfig.category.ToString().ToLower();
                    string folderPath = Path.Combine(selectedBasePath, subFolderName);
                    if (Directory.Exists(folderPath))
                    {
                        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
                        foreach (string guid in guids)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                            if (prefab != null && !catConfig.prefabs.Contains(prefab))
                            {
                                catConfig.prefabs.Add(prefab);
                                totalLoaded++;
                            }
                        }
                    }
                }
                if (totalLoaded > 0)
                {
                    Debug.Log($"[RandomAssetGenerator] Auto-populated {totalLoaded} Toy prefabs from Assets/ThirdParty/Selected/toy.");
                }
            }
#endif
        }

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateWorldAssets();
            }
        }

        [ContextMenu("Generate World Assets")]
        public void GenerateWorldAssets()
        {
            AutoPopulateCategoriesIfEmpty();
            AutoFindGrassZonesInScene();

            InitializeRNG();
            ClearGeneratedAssets();

            // Create standalone root container GameObject if not assigned
            if (spawnedObjectsParent == null || spawnedObjectsParent.gameObject == null)
            {
                GameObject rootContainer = GameObject.Find("★_SPAWNED_ASSETS_CONTAINER_★");
                if (rootContainer == null)
                {
                    rootContainer = new GameObject("★_SPAWNED_ASSETS_CONTAINER_★");
                }
                spawnedObjectsParent = rootContainer.transform;
            }

            List<PlacedObjectInfo> placedObjects = new List<PlacedObjectInfo>();

            int totalSpawned = 0;
            int totalAttempted = 0;

            if (categories == null || categories.Count == 0)
            {
                Debug.LogError("[RandomAssetGenerator] Categories list is empty!");
                return;
            }

            foreach (var catConfig in categories)
            {
                if (catConfig == null || catConfig.prefabs == null)
                {
                    continue;
                }

                catConfig.requireGrassZone = false;

                if (catConfig.instancesPerPrefab <= 0)
                {
                    catConfig.instancesPerPrefab = 1;
                }

                // Sanitize prefabs list
                catConfig.prefabs.RemoveAll(p => p == null || !p || !(p is GameObject));

                if (catConfig.prefabs.Count == 0)
                {
                    Debug.LogWarning($"[RandomAssetGenerator] Category '{catConfig.categoryName}' has 0 prefabs loaded.");
                    continue;
                }

                int catSpawned = 0;

                // Take snapshot array
                GameObject[] prefabsSnapshot = catConfig.prefabs.ToArray();

                foreach (GameObject prefab in prefabsSnapshot)
                {
                    if (prefab == null || !prefab) continue;

                    int targetInstances = Mathf.Max(1, catConfig.instancesPerPrefab);
                    for (int inst = 0; inst < targetInstances; inst++)
                    {
                        totalAttempted++;
                        bool placed = TryPlaceSpecificPrefab(prefab, catConfig, placedObjects);
                        if (placed)
                        {
                            catSpawned++;
                            totalSpawned++;
                        }
                        else
                        {
                            Debug.LogWarning($"[RandomAssetGenerator] Skipping '{prefab.name}' ({catConfig.categoryName}): No valid ground position found.");
                        }
                    }
                }

                Debug.Log($"[RandomAssetGenerator] Category '{catConfig.categoryName}': Successfully spawned {catSpawned}/{prefabsSnapshot.Length} unique prefabs.");
            }

            Debug.Log($"<color=green>[RandomAssetGenerator] GENERATION COMPLETE!</color> Total {totalSpawned} assets spawned under root container '{spawnedObjectsParent.name}'.");
        }

        [ContextMenu("Clear Generated Assets")]
        public void ClearGeneratedAssets()
        {
            if (spawnedObjectsParent != null)
            {
                int childCount = spawnedObjectsParent.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    Transform child = spawnedObjectsParent.GetChild(i);
                    if (child == null) continue;
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }

            GameObject rootContainer = GameObject.Find("★_SPAWNED_ASSETS_CONTAINER_★");
            if (rootContainer != null && rootContainer != (spawnedObjectsParent != null ? spawnedObjectsParent.gameObject : null))
            {
                if (Application.isPlaying) Destroy(rootContainer);
                else DestroyImmediate(rootContainer);
            }
        }

        private void InitializeRNG()
        {
            if (randomSeed >= 0)
            {
                UnityEngine.Random.InitState(randomSeed);
            }
            else
            {
                UnityEngine.Random.InitState((int)(System.DateTime.Now.Ticks & 0x0000FFFF));
            }
        }

        private bool TryPlaceSpecificPrefab(GameObject prefab, AssetCategoryConfig catConfig, List<PlacedObjectInfo> placedObjects)
        {
            if (prefab == null || !prefab || !(prefab is GameObject))
            {
                return false;
            }

            Bounds zoneBounds = CalculateSceneGroundBounds();
            if (zoneBounds.size == Vector3.zero)
            {
                return false;
            }

            Bounds prefabBounds = CalculatePrefabLocalBounds(prefab);
            float objectRadius = Mathf.Max(prefabBounds.extents.x, prefabBounds.extents.z);

            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                float rx = UnityEngine.Random.Range(zoneBounds.min.x, zoneBounds.max.x);
                float rz = UnityEngine.Random.Range(zoneBounds.min.z, zoneBounds.max.z);
                Vector3 samplePos = new Vector3(rx, sampleRaycastHeight, rz);

                Ray ray = new Ray(samplePos, Vector3.down);
                bool raycastHit = Physics.Raycast(ray, out RaycastHit hit, sampleRaycastHeight * 2f, groundLayerMask);
                
                if (!raycastHit) continue; // Reject empty void / outside bounds

                // Rule A: Water / Sea Exclusion
                if (IsWaterOrSeaSurface(hit)) continue;

                // Rule B: Building / Roof / Canopy / Stairs Exclusion
                if (IsBuildingOrRoofOrStairsSurface(hit)) continue;

                // Rule C: Surface Normal Check (MUST BE PERFECTLY HORIZONTAL STREET GROUND, hit.normal.y >= 0.92f)
                if (hit.normal.y < 0.92f) continue;

                // Rule D: Strict Height Check (MUST BE STREET LEVEL Y <= 0.8f! Rejects roofs, balconies, fire escapes, canopies)
                if (hit.point.y > maxWalkableGroundHeight || hit.point.y < -0.5f) continue;

                // Rule E: Toys MUST NOT be on Grass (must be open city ground / sidewalks / plazas)
                if (IsPositionOnGrassSurface(hit)) continue;

                Vector3 spawnPos = hit.point;

                // Rule F: Spacing Check with previously spawned objects
                bool distanceOk = true;
                foreach (var placed in placedObjects)
                {
                    float minDist = catConfig.minSpacing + placed.radius;
                    if (Vector3.Distance(spawnPos, placed.position) < minDist)
                    {
                        distanceOk = false;
                        break;
                    }
                }
                if (!distanceOk) continue;

                // Rule G: Strict Scene Obstacle Overlap Check
                float rotY = UnityEngine.Random.Range(catConfig.yRotationRange.x, catConfig.yRotationRange.y);
                Quaternion spawnRot = Quaternion.Euler(0f, rotY, 0f);

                Vector3 boxCenter = spawnPos + (spawnRot * prefabBounds.center);
                Vector3 boxHalfExtents = Vector3.Scale(prefabBounds.extents, new Vector3(1.35f, 1.35f, 1.35f));

                Collider[] overlaps = Physics.OverlapBox(boxCenter, boxHalfExtents, spawnRot, ~0, QueryTriggerInteraction.Ignore);
                bool hasSceneObstacleOverlap = false;

                if (overlaps != null && overlaps.Length > 0)
                {
                    foreach (var col in overlaps)
                    {
                        if (col == null || !col.enabled || col.isTrigger) continue;
                        if (col == hit.collider) continue; // Ignore ground surface hit collider itself
                        if (spawnedObjectsParent != null && col.transform.IsChildOf(spawnedObjectsParent)) continue; // Ignore previously spawned objects

                        hasSceneObstacleOverlap = true;
                        break;
                    }
                }
                if (hasSceneObstacleOverlap) continue;

                // Rule H: Check Curb Step Edge Proximity (ensures ball/toy is not on sidewalk curb transition line)
                Collider[] nearbyColliders = Physics.OverlapSphere(spawnPos, objectRadius + 0.25f, ~0, QueryTriggerInteraction.Ignore);
                int groundCount = 0;
                foreach (var c in nearbyColliders)
                {
                    if (c == null || !c.enabled || c.isTrigger) continue;
                    if (spawnedObjectsParent != null && c.transform.IsChildOf(spawnedObjectsParent)) continue;
                    if (c != hit.collider)
                    {
                        groundCount++;
                    }
                }
                if (groundCount > 0) continue;

                // Instantiate object
                GameObject instance = null;
                try
                {
                    instance = Instantiate(prefab, spawnPos, spawnRot, spawnedObjectsParent);
                }
                catch (InvalidCastException ex)
                {
                    Debug.LogWarning($"[RandomAssetGenerator] InvalidCastException on prefab '{prefab.name}': {ex.Message}.");
                    catConfig.prefabs.Remove(prefab);
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RandomAssetGenerator] Exception instantiating prefab '{prefab.name}': {ex.Message}");
                    return false;
                }

                if (instance == null) return false;

                // Set a clear name in Hierarchy
                instance.name = $"[{catConfig.categoryName}] {prefab.name}";
                
                float randomScale = UnityEngine.Random.Range(catConfig.scaleRange.x, catConfig.scaleRange.y);
                if (Mathf.Abs(randomScale - 1f) > 0.01f)
                {
                    instance.transform.localScale = prefab.transform.localScale * randomScale;
                }

                placedObjects.Add(new PlacedObjectInfo { position = spawnPos, radius = objectRadius * randomScale });
                return true;
            }

            return false;
        }

        private bool IsWaterOrSeaSurface(RaycastHit hit)
        {
            if (hit.collider == null) return false;
            
            GameObject hitGo = hit.collider.gameObject;
            string goName = hitGo.name.ToLower();
            string tagStr = hitGo.tag.ToLower();

            if (goName.Contains("water") || goName.Contains("lake") || goName.Contains("sea") || goName.Contains("ocean") ||
                tagStr.Contains("water") || tagStr.Contains("sea") || tagStr.Contains("ocean"))
            {
                return true;
            }

            Transform p = hitGo.transform.parent;
            while (p != null)
            {
                string pName = p.name.ToLower();
                if (pName.Contains("water") || pName.Contains("lake") || pName.Contains("sea") || pName.Contains("ocean"))
                {
                    return true;
                }
                p = p.parent;
            }

            return false;
        }

        private bool IsBuildingOrRoofOrStairsSurface(RaycastHit hit)
        {
            if (hit.collider == null) return false;

            GameObject hitGo = hit.collider.gameObject;
            string goName = hitGo.name.ToLower();
            string tagStr = hitGo.tag.ToLower();

            if (goName.Contains("bld") || goName.Contains("building") || goName.Contains("stairs") || goName.Contains("fireescape") ||
                goName.Contains("roof") || goName.Contains("canopy") || goName.Contains("balcony") || goName.Contains("wall") ||
                goName.Contains("glass") || goName.Contains("window") || goName.Contains("railing") || goName.Contains("ledge") ||
                goName.Contains("house") || goName.Contains("shop") || goName.Contains("apartment") || goName.Contains("plot_building"))
            {
                return true;
            }

            Transform p = hitGo.transform.parent;
            while (p != null)
            {
                string pName = p.name.ToLower();
                if (pName.Contains("bld") || pName.Contains("building") || pName.Contains("stairs") || pName.Contains("fireescape") ||
                    pName.Contains("roof") || pName.Contains("canopy") || pName.Contains("balcony") || pName.Contains("wall") ||
                    pName.Contains("house") || pName.Contains("shop") || pName.Contains("apartment"))
                {
                    return true;
                }
                p = p.parent;
            }

            return false;
        }

        private bool IsPositionOnGrassSurface(RaycastHit hit)
        {
            if (hit.collider == null) return false;

            if (grassZones != null && grassZones.Count > 0)
            {
                foreach (var gz in grassZones)
                {
                    if (gz == null || !gz || !gz.enabled) continue;

                    if (hit.collider == gz) return true;

                    Bounds b = gz.bounds;
                    if (hit.point.x >= b.min.x - 0.5f && hit.point.x <= b.max.x + 0.5f &&
                        hit.point.z >= b.min.z - 0.5f && hit.point.z <= b.max.z + 0.5f)
                    {
                        return true;
                    }
                }
            }

            GameObject hitGo = hit.collider.gameObject;
            string goName = hitGo.name.ToLower();
            string tagStr = hitGo.tag.ToLower();

            if (goName.Contains("grass") || goName.Contains("clearing") || goName.Contains("park") || goName.Contains("lawn") ||
                tagStr.Contains("grass") || tagStr.Contains("park"))
            {
                return true;
            }

            return false;
        }

        private Bounds CalculateSceneGroundBounds()
        {
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if (allRenderers != null && allRenderers.Length > 0)
            {
                Bounds b = new Bounds();
                bool hasValid = false;
                foreach (var r in allRenderers)
                {
                    if (r == null || !r || !r.enabled) continue;
                    if (r is ParticleSystemRenderer || r is CanvasRenderer) continue;
                    if (spawnedObjectsParent != null && r.transform.IsChildOf(spawnedObjectsParent)) continue;

                    string rName = r.gameObject.name.ToLower();
                    if (rName.Contains("water") || rName.Contains("lake") || rName.Contains("ocean") || rName.Contains("sea") ||
                        rName.Contains("sky") || rName.Contains("cloud") || rName.Contains("backdrop"))
                    {
                        continue;
                    }

                    if (!hasValid)
                    {
                        b = r.bounds;
                        hasValid = true;
                    }
                    else
                    {
                        b.Encapsulate(r.bounds);
                    }
                }
                if (hasValid && b.size.magnitude > 1f)
                {
                    return b;
                }
            }

            return new Bounds(Vector3.zero, new Vector3(100f, 0f, 100f));
        }

        private Bounds CalculatePrefabLocalBounds(GameObject prefab)
        {
            if (prefab == null || !prefab || !(prefab is GameObject))
            {
                return new Bounds(Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f));
            }

            try
            {
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (renderers != null && renderers.Length > 0)
                {
                    Bounds b = new Bounds(Vector3.zero, Vector3.zero);
                    bool hasValidBounds = false;

                    foreach (var r in renderers)
                    {
                        if (r != null && r)
                        {
                            if (!hasValidBounds)
                            {
                                b = r.bounds;
                                hasValidBounds = true;
                            }
                            else
                            {
                                b.Encapsulate(r.bounds);
                            }
                        }
                    }

                    if (hasValidBounds)
                    {
                        Vector3 prefabPos = (prefab.transform != null) ? prefab.transform.position : Vector3.zero;
                        Vector3 localCenter = b.center - prefabPos;
                        return new Bounds(localCenter, b.size);
                    }
                }

                Collider[] colliders = prefab.GetComponentsInChildren<Collider>(true);
                if (colliders != null && colliders.Length > 0)
                {
                    Bounds b = new Bounds(Vector3.zero, Vector3.zero);
                    bool hasValidBounds = false;

                    foreach (var c in colliders)
                    {
                        if (c != null && c)
                        {
                            if (!hasValidBounds)
                            {
                                b = c.bounds;
                                hasValidBounds = true;
                            }
                            else
                            {
                                b.Encapsulate(c.bounds);
                            }
                        }
                    }

                    if (hasValidBounds)
                    {
                        Vector3 prefabPos = (prefab.transform != null) ? prefab.transform.position : Vector3.zero;
                        Vector3 localCenter = b.center - prefabPos;
                        return new Bounds(localCenter, b.size);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RandomAssetGenerator] Failed to compute bounds for prefab '{prefab.name}': {ex.Message}");
            }

            return new Bounds(Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f));
        }

        private void OnDrawGizmosSelected()
        {
            if (grassZones != null)
            {
                Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.4f);
                foreach (var z in grassZones)
                {
                    if (z != null && z.enabled)
                    {
                        Gizmos.DrawWireCube(z.bounds.center, z.bounds.size);
                    }
                }
            }

            Bounds sceneB = CalculateSceneGroundBounds();
            Gizmos.color = new Color(0.2f, 0.5f, 1.0f, 0.3f);
            Gizmos.DrawWireCube(sceneB.center, sceneB.size);
        }
    }
}
