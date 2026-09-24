using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Robot.ObjectHunt;
namespace Robot.UI.Production
{
    public sealed class RoundIntroController : MonoBehaviour
    {
        public RoundGameLoop loop;
        public TargetVisualLibrary visuals;
        public GameObject reveal;
        public CanvasGroup[] cards;
        public RawImage[] icons;
        public Text[] names;
        public Text countdown;
        private void OnEnable() { if (loop != null) loop.CountdownChanged += OnCountdown; }
        private void OnDisable() { if (loop != null) loop.CountdownChanged -= OnCountdown; StopAllCoroutines(); }
        public void Begin() { StopAllCoroutines(); if (countdown != null) countdown.text = ""; if (reveal != null) reveal.SetActive(false); }
        public void ShowRoundIntro(IReadOnlyList<TargetDefinition> targets)
        {
            StopAllCoroutines(); if (countdown != null) countdown.text = ""; if (reveal != null) reveal.SetActive(true);
            if (targets.Count != 3) { Debug.LogError("Round intro requires three selected targets from the existing round manager.", this); return; }
            for (int i = 0; i < 3; i++)
            {
                names[i].text = ToyNames.Display(targets[i].displayName); visuals.Assign(icons[i], targets[i]); cards[i].alpha = 0;
            }
            StartCoroutine(RevealCards());
        }
        private IEnumerator RevealCards()
        {
            foreach (var card in cards)
            {
                for (float t = 0; t < .25f; t += Time.unscaledDeltaTime)
                {
                    float v = Mathf.SmoothStep(0, 1, t / .25f); card.alpha = v;
                    card.transform.localScale = Vector3.one * Mathf.Lerp(.92f, 1, v); yield return null;
                }
                card.alpha = 1; card.transform.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(.12f);
            }
        }
        public void BeginCountdown() { StopAllCoroutines(); if (reveal != null) reveal.SetActive(false); }
        private void OnCountdown(int value)
        {
            if (countdown == null) return;
            countdown.text = value > 0 ? value.ToString() : "GO!";
            countdown.fontSize = value > 0 ? 120 : 144;
            countdown.color = value > 0 ? Color.white : UIView.AccentYellow;
            StopAllCoroutines(); StartCoroutine(Pulse());
        }
        private IEnumerator Pulse()
        {
            for (float t = 0; t < .2f; t += Time.unscaledDeltaTime)
            {
                countdown.transform.localScale = Vector3.one * Mathf.Lerp(1.12f, 1, t / .2f); yield return null;
            }
            countdown.transform.localScale = Vector3.one;
        }
        public void Hide() { StopAllCoroutines(); if (countdown != null) countdown.text = ""; if (reveal != null) reveal.SetActive(false); }
    }
}
