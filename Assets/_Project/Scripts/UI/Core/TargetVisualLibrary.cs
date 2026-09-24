using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Robot.ObjectHunt;
namespace Robot.UI.Production
{
    // Mesh-only display copies: never instantiate gameplay scripts/colliders for a preview.
    public sealed class TargetVisualLibrary : MonoBehaviour
    {
        private readonly Dictionary<string, RenderTexture> textures = new Dictionary<string, RenderTexture>();
        private readonly List<GameObject> stages = new List<GameObject>();
        private readonly List<Mesh> bakedMeshes = new List<Mesh>();
        public void Assign(RawImage image, TargetDefinition definition)
        {
            image.uvRect = new Rect(0, 0, 1, 1);
            if (definition.icon != null)
            {
                var sprite = definition.icon; var rect = sprite.textureRect;
                image.texture = sprite.texture;
                image.uvRect = new Rect(rect.x / sprite.texture.width, rect.y / sprite.texture.height, rect.width / sprite.texture.width, rect.height / sprite.texture.height);
                return;
            }
            if (!textures.TryGetValue(definition.objectId, out var texture))
            {
                texture = CreatePreview(definition.prefab, stages.Count, false);
                textures.Add(definition.objectId, texture);
            }
            image.texture = texture;
        }
        public RenderTexture CreatePreview(GameObject source, int index, bool robot, GameObject materialSource = null)
        {
            if (source == null) return null;
            var stage = new GameObject(source.name + " UI Visual");
            stage.transform.position = new Vector3(10000 + index * 40, 10000, 10000); stages.Add(stage);
            var model = new GameObject("DisplayMesh"); model.transform.SetParent(stage.transform, false);
            foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeSelf) continue;
                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skin) { mesh = new Mesh(); skin.BakeMesh(mesh); bakedMeshes.Add(mesh); }
                else { var filter = renderer.GetComponent<MeshFilter>(); if (filter != null) mesh = filter.sharedMesh; }
                if (mesh == null) continue;
                var part = new GameObject(renderer.name, typeof(MeshFilter), typeof(MeshRenderer)); part.layer = 31;
                part.transform.SetParent(model.transform, false);
                part.transform.localPosition = source.transform.InverseTransformPoint(renderer.transform.position);
                part.transform.localRotation = Quaternion.Inverse(source.transform.rotation) * renderer.transform.rotation;
                Vector3 rootScale = source.transform.lossyScale, scale = renderer.transform.lossyScale;
                part.transform.localScale = new Vector3(scale.x / rootScale.x, scale.y / rootScale.y, scale.z / rootScale.z);
                part.GetComponent<MeshFilter>().sharedMesh = mesh;
                var materials = renderer.sharedMaterials;
                if (materialSource != null)
                    foreach (var live in materialSource.GetComponentsInChildren<Renderer>(true))
                        if (live.name == renderer.name) { materials = live.sharedMaterials; break; }
                part.GetComponent<MeshRenderer>().sharedMaterials = materials;
            }
            model.transform.localRotation = Quaternion.Euler(0, robot ? 160 : 210, 0);
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return null;
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var camera = new GameObject("PreviewCamera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(stage.transform, false);
            camera.transform.position = bounds.center + new Vector3(0, bounds.extents.y * .25f, -Mathf.Max(5, bounds.size.magnitude * 2));
            camera.transform.LookAt(bounds.center);
            camera.orthographic = true; camera.orthographicSize = Mathf.Max(.01f, Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.3f);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear; camera.cullingMask = 1 << 31;
            camera.nearClipPlane = .01f; camera.farClipPlane = Mathf.Max(20, bounds.size.magnitude * 4);
            camera.allowHDR = false; camera.allowMSAA = false;
            var texture = new RenderTexture(robot ? 384 : 192, robot ? 384 : 192, 24, RenderTextureFormat.ARGB32) { name = source.name + " UI Render" };
            texture.Create(); camera.targetTexture = texture;
            return texture;
        }
        public void Clear()
        {
            foreach (var stage in stages)
            {
                if (stage == null) continue;
                var camera = stage.GetComponentInChildren<Camera>();
                if (camera != null && camera.targetTexture != null) { var rt = camera.targetTexture; camera.targetTexture = null; rt.Release(); Destroy(rt); }
                stage.SetActive(false); Destroy(stage);
            }
            foreach (var mesh in bakedMeshes) Destroy(mesh);
            stages.Clear(); bakedMeshes.Clear(); textures.Clear();
        }
        private void OnDestroy() => Clear();
    }
}
