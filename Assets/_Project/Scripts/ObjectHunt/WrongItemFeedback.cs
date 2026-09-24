using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.ObjectHunt
{
    // Visual copies recoil while the original solid collision geometry stays fixed.
    public sealed class WrongItemFeedback : MonoBehaviour
    {
        private Coroutine routine;
        private readonly List<Renderer> originals = new List<Renderer>();
        private GameObject visual;
        public void Play()
        {
            if (routine != null) return;
            routine = StartCoroutine(Animate());
        }
        private IEnumerator Animate()
        {
            visual = new GameObject("Rejection visual");
            visual.transform.SetParent(transform, false);
            foreach (var source in GetComponentsInChildren<MeshRenderer>())
            {
                if (!source.enabled || source.transform.IsChildOf(visual.transform)) continue;
                var filter = source.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var copy = new GameObject("Visual", typeof(MeshFilter), typeof(MeshRenderer));
                copy.transform.SetParent(source.transform, false);
                copy.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                copy.GetComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
                copy.transform.SetParent(visual.transform, true);
                originals.Add(source);
                source.enabled = false;
            }
            for (float t = 0; t < .4f; t += Time.deltaTime)
            {
                float fade = 1f - t / .4f;
                visual.transform.localPosition = new Vector3(Mathf.Sin(t * 65f) * .09f * fade, 0, -Mathf.Sin(t / .4f * Mathf.PI) * .1f);
                yield return null;
            }
            Restore();
        }
        private void Restore()
        {
            foreach (var original in originals) if (original != null) original.enabled = true;
            originals.Clear();
            if (visual != null) Destroy(visual);
            routine = null;
        }
        private void OnDisable() { StopAllCoroutines(); Restore(); }
    }
}
