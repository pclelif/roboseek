using UnityEngine;
using UnityEngine.UI;
using Robot.ObjectHunt;
using Robot.Score;
namespace Robot.UI.Production
{
    public sealed class ResultScreenController : MonoBehaviour
    {
        public ObjectHuntRoundManager hunt;
        public TargetVisualLibrary visuals;
        public Text title, count, time, score;
        public RawImage[] icons;
        public Text[] names;
        public Button next;
        private void Awake() => ApplyLayout();
        public void ApplyLayout()
        {
            var panel = title.transform.parent;
            var background = panel.GetComponent<Image>();
            if (background != null) { background.sprite = null; background.color = new Color(.035f,.035f,.035f,.97f); }
            var root = GetComponent<UIRootController>();
            var hero = root.hub.showcase.resultImage.rectTransform;
            hero.sizeDelta = new Vector2(310,310); hero.anchoredPosition = new Vector2(-245,30);
            if (panel.Find("ResultBrand") == null)
            {
                UIView.Label("ResultBrand", panel, "ROBOSEEK", 37, new Vector2(330,55), new Vector2(-245,208), UIView.AccentYellow);
                UIView.Label("ResultSlogan", panel, "WHERE IS MY TOY?", 16, new Vector2(330,28), new Vector2(-245,165), Color.white);
            }
        }
        public void ShowPending() { title.text = UILocalization.Translate("ROUND COMPLETE"); count.text = hunt.CollectedCount + " / " + hunt.SelectedTargets.Count; time.text = ""; score.text = ""; next.interactable = false; AssignTargets(); FixFonts(); }
        public void Show(RoundResultData data)
        {
            title.text = UILocalization.Translate(data.completed ? "ROUND COMPLETE" : hunt.EndedByDrowning ? "GAME OVER" : "ROUND OVER");
            count.text = data.foundCount + " / " + hunt.SelectedTargets.Count;
            int seconds = Mathf.FloorToInt(data.elapsedTime);
            time.text = $"{UILocalization.Translate("TIME")}\n<color=#{ColorUtility.ToHtmlStringRGB(UIView.AccentYellow)}>{seconds / 60:00}:{seconds % 60:00}</color>";
            int points = ScoreManager.Instance != null ? ScoreManager.Instance.LocalScore.RoundScore : data.roundScore;
            score.text = UILocalization.Translate("SCORE") + "\n" + points; next.interactable = true; AssignTargets(); FixFonts();
        }
        
        private void FixFonts()
        {
            if (count != null)
            {
                count.font = UIView.FontFor(count.name, count.text);
                count.fontStyle = FontStyle.Normal;
            }
            if (time != null)
            {
                time.font = UIView.FontFor(time.name, time.text);
                time.fontStyle = FontStyle.Normal;
            }
            if (score != null)
            {
                score.font = UIView.FontFor(score.name, score.text);
                score.fontStyle = FontStyle.Normal;
            }
        }
        private void AssignTargets()
        {
            for (int i = 0; i < icons.Length; i++)
            {
                bool exists = i < hunt.SelectedTargets.Count;
                icons[i].transform.parent.gameObject.SetActive(exists);
                if (!exists) continue;
                var target = hunt.SelectedTargets[i]; visuals.Assign(icons[i], target); names[i].text = ToyNames.Display(target.displayName);
                bool found = false; foreach (var collected in hunt.CollectedTargets) if (collected.objectId == target.objectId) found = true;
                var marker = icons[i].transform.parent.Find("ResultMarker");
                if (marker != null) Destroy(marker.gameObject);
                var check = UIView.Rect("ResultMarker", icons[i].transform.parent, new Vector2(22,22), new Vector2(0,-85));
                var a = UIView.Panel("ShortStroke", check, new Vector2(found ? 9 : 23,4), UIView.AccentYellow, found ? new Vector2(-5,-2) : Vector2.zero);
                var b = UIView.Panel("LongStroke", check, new Vector2(found ? 18 : 23,4), UIView.AccentYellow, found ? new Vector2(4,1) : Vector2.zero);
                a.sprite = b.sprite = null; a.raycastTarget = b.raycastTarget = false;
                a.transform.localRotation = Quaternion.Euler(0,0,-45); b.transform.localRotation = Quaternion.Euler(0,0,45);
                icons[i].color = found ? Color.white : new Color(1, 1, 1, .55f);
            }
        }
    }
}
