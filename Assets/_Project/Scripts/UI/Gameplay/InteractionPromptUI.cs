using System.Collections;
using UnityEngine;
using Robot.ObjectHunt;
namespace Robot.UI.Production
{
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        public CanvasGroup group;
        public InteractionStateAdapter source;
        private Coroutine fade;
        private void OnEnable() { source.Changed += SetInteractable; source.Retrieved += HideImmediately; SetInteractable(source.Current); }
        private void OnDisable() { source.Changed -= SetInteractable; source.Retrieved -= HideImmediately; HideImmediately(); }
        public void SetInteractable(CollectibleTarget target) { if (target != null) Show(); else Hide(); }
        public void Show() => FadeTo(1);
        public void Hide() => FadeTo(0);
        public void HideImmediately() { if (fade != null) StopCoroutine(fade); fade = null; group.alpha = 0; }
        private void FadeTo(float alpha)
        {
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Fade(alpha));
        }
        private IEnumerator Fade(float alpha)
        {
            while (!Mathf.Approximately(group.alpha, alpha))
            {
                group.alpha = Mathf.MoveTowards(group.alpha, alpha, Time.unscaledDeltaTime / .16f);
                yield return null;
            }
            fade = null;
        }
    }
}
