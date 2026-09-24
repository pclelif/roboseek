using UnityEngine;
using UnityEngine.UI;
using Robot.Combat;
namespace Robot.UI.Production
{
    public sealed class KnockoutNotice : MonoBehaviour
    {
        private UIRootController root;
        private CombatHealth health;
        private Robot.Player.Movement.WorldSafety water;
        private Image panel;
        private Text message;
        private bool isInitialized;
        private bool EnsureInitialized()
        {
            if (isInitialized) return true;
            root = GetComponent<UIRootController>();
            if (root == null || root.state == null || root.state.gameplayHUD == null || root.state.inputGate == null || root.state.inputGate.movement == null) return false;

            health = root.state.inputGate.movement.GetComponent<CombatHealth>();
            if (health == null) return false;

            water = health.GetComponent<Robot.Player.Movement.WorldSafety>();
            panel = UIView.Panel("KnockoutNotice", root.state.gameplayHUD.transform, new Vector2(650,125), new Color(.025f,.025f,.025f,.95f), new Vector2(-100,120));
            panel.sprite = null; panel.raycastTarget = false;
            message = UIView.Label("RecoveryCountdown", panel.transform, "", 25, new Vector2(620,110), Vector2.zero, Color.white);
            panel.gameObject.SetActive(false);

            isInitialized = true;
            return true;
        }

        private void Start() => EnsureInitialized();

        private void Update()
        {
            if (!EnsureInitialized()) return;
            if (panel == null) return;
            bool drowning = water != null && water.IsDrowning;
            bool visible = (drowning || (health != null && health.IsKnockedOut)) && root.state.IsGameplay;
            panel.gameObject.SetActive(visible);
            if (!visible) return;
            if (drowning)
            {
                string drowningCaption = UILocalization.IsTurkish ? "ROBOT SUYUN İÇİNDE!" : "ROBOT DROWNING!";
                string gameOver = UILocalization.IsTurkish ? "Oyun bitti" : "GAME OVER";
                string redHex = "FF3B30";
                message.text = drowningCaption + "\n" + gameOver + " IN <color=#" + redHex + ">" + Mathf.CeilToInt(water.DrowningTimeRemaining) + "</color> s";
                return;
            }
            string caption = UILocalization.IsTurkish ? "DARBE ALDIN — ROBOT DEVRE DIŞI" : "YOU WERE HIT — ROBOT KNOCKED OUT";
            string recovery = UILocalization.IsTurkish ? "Yeniden başlatılıyor" : "Rebooting";
            message.text = caption + "\n" + recovery + " <color=#" + ColorUtility.ToHtmlStringRGB(UIView.AccentYellow) + ">" + Mathf.CeilToInt(health.KnockoutTimeRemaining) + "</color> s";
        }
    }
}
