using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace Robot.UI.Production
{
    public sealed class FeedbackMessageUI : MonoBehaviour
    {
        public CanvasGroup group;
        public Text targetName;
        private Coroutine routine;
        private void Awake() => ApplyStyle();
        public void ApplyStyle()
        {
            if (group == null) return;
            var heading = group.transform.Find("Heading")?.GetComponent<Text>();
            if (heading != null) heading.color = UIView.AccentYellow;
        }
        public void Show(string value)
        {
            ApplyStyle();
            Clear(); targetName.text = value.ToUpperInvariant();
            routine = StartCoroutine(Present());
        }
        public void Clear() { if (routine != null) StopCoroutine(routine); routine = null; group.alpha = 0; }
        private IEnumerator Present()
        {
            for (float t = 0; t < 1.35f; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Min(Mathf.Clamp01(t / .12f), Mathf.Clamp01((1.35f - t) / .3f));
                yield return null;
            }
            Clear();
        }
        private void OnDisable() => Clear();
    }
}
