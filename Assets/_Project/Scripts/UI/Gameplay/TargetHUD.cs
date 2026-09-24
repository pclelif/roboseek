using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Robot.ObjectHunt;
namespace Robot.UI.Production
{
    public sealed class TargetHUD : MonoBehaviour
    {
        public ObjectHuntRoundManager hunt;
        public TargetVisualLibrary visuals;
        public RawImage[] icons;
        public Text[] names;
        public Text[] states;
        public RectTransform[] checks;
        public FeedbackMessageUI feedback;
        private Text timer;
        private void Start() => ApplyLayout();
        public void ApplyLayout()
        {
            if (feedback != null) feedback.ApplyStyle();
            if (icons.Length == 0) return;
            var panel = icons[0].transform.parent.parent as RectTransform;
            panel.sizeDelta = new Vector2(420,182);
            panel.GetComponent<Image>().color = new Color(.035f,.035f,.035f,.94f);
            var heading = panel.Find("Heading").GetComponent<Text>();
            heading.fontSize = 21; heading.rectTransform.sizeDelta = new Vector2(380,32); heading.rectTransform.anchoredPosition = new Vector2(0,65);
            heading.font = UIView.FontFor(heading.name, heading.text);
            for (int i=0;i<icons.Length;i++)
            {
                var slot=icons[i].transform.parent as RectTransform;
                slot.sizeDelta=new Vector2(124,120); slot.anchoredPosition=new Vector2((i-1)*132,-13);
                icons[i].rectTransform.sizeDelta=new Vector2(88,78); icons[i].rectTransform.anchoredPosition=new Vector2(0,20);
                names[i].fontSize=14; names[i].rectTransform.sizeDelta=new Vector2(124,38); names[i].rectTransform.anchoredPosition=new Vector2(0,-36);
                foreach (var stroke in checks[i].GetComponentsInChildren<Image>(true)) { stroke.sprite = null; stroke.color = UIView.AccentYellow; }
                states[i].fontSize=14; states[i].font = UIView.FontFor(states[i].name, states[i].text); states[i].fontStyle = FontStyle.Normal; states[i].rectTransform.anchoredPosition=new Vector2(0,-66); checks[i].anchoredPosition=new Vector2(0,-66);
            }
            var existingClock = panel.parent.Find("RoundTimer");
            if (existingClock != null) { timer = existingClock.Find("TimeRemaining").GetComponent<Text>(); timer.color = UIView.AccentYellow; timer.font = UIView.FontFor(timer.name, timer.text); timer.fontStyle = FontStyle.Normal; timer.supportRichText = false; return; }
            var clock=UIView.Panel("RoundTimer",panel.parent,new Vector2(218,94),new Color(.035f,.035f,.035f,.94f));
            clock.raycastTarget=false;
            clock.rectTransform.anchorMin=clock.rectTransform.anchorMax=clock.rectTransform.pivot=new Vector2(0,1);
            clock.rectTransform.anchoredPosition=new Vector2(40,-40);
            UIView.Label("Caption",clock.transform,"TIME",16,new Vector2(190,24),new Vector2(0,26),Color.white);
            timer=UIView.Label("TimeRemaining",clock.transform,"10:00",38,new Vector2(190,52),new Vector2(0,-13),UIView.AccentYellow);
            timer.font = UIView.FontFor(timer.name, timer.text);
            timer.fontStyle = FontStyle.Normal;
            timer.supportRichText = false;
        }
        private void Update()
        {
            if(timer==null || hunt==null)return;
            int seconds=Mathf.CeilToInt(hunt.TimeRemaining);
            timer.text=$"{seconds/60:00}:{seconds%60:00}";
        }
        private void OnEnable()
        {
            hunt.RoundStarted += SetTargets; hunt.TargetCollected += OnCollected;
            SetTargets(hunt.SelectedTargets);
        }
        private void OnDisable() { hunt.RoundStarted -= SetTargets; hunt.TargetCollected -= OnCollected; StopAllCoroutines(); }
        public void SetTargets(IReadOnlyList<TargetDefinition> targets)
        {
            StopAllCoroutines(); feedback.Clear(); visuals.Clear();
            for (int i = 0; i < icons.Length; i++)
            {
                bool exists = i < targets.Count; icons[i].transform.parent.gameObject.SetActive(exists);
                checks[i].gameObject.SetActive(false); states[i].text = "0"; states[i].color = Color.white; states[i].font = UIView.FontFor(states[i].name, states[i].text);
                icons[i].color = Color.white; icons[i].rectTransform.localScale = Vector3.one;
                if (!exists) continue;
                names[i].text = ToyNames.Display(targets[i].displayName); visuals.Assign(icons[i], targets[i]);
                foreach (var found in hunt.CollectedTargets)
                    if (found.objectId == targets[i].objectId) SetFound(i);
            }
        }
        private void SetFound(int index)
        {
            states[index].text = ""; checks[index].gameObject.SetActive(true);
            icons[index].color = new Color(.7f, .85f, .75f, .7f);
            states[index].font = UIView.FontFor(states[index].name, states[index].text);
        }
        private void OnCollected(TargetDefinition target, int count)
        {
            for (int i = 0; i < hunt.SelectedTargets.Count && i < icons.Length; i++)
                if (hunt.SelectedTargets[i].objectId == target.objectId) { SetFound(i); StartCoroutine(Pulse(icons[i].rectTransform)); }
            feedback.Show(ToyNames.Display(target.displayName));
        }
        private IEnumerator Pulse(RectTransform rect)
        {
            for (float t = 0; t < .3f; t += Time.unscaledDeltaTime)
            {
                rect.localScale = Vector3.one * (1 + .1f * Mathf.Sin(t / .3f * Mathf.PI)); yield return null;
            }
            rect.localScale = Vector3.one;
        }
    }
}
