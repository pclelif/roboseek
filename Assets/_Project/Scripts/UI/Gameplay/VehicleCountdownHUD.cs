using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI.Production
{
    public sealed class VehicleCountdownHUD : MonoBehaviour
    {
        private UIRootController root;
        private Image panel;
        private Text titleText;
        private Text countText;
        private float activeUntil;
        private string currentTitle;

        private bool isInitialized;
        private bool EnsureInitialized()
        {
            if (isInitialized) return true;
            root = GetComponent<UIRootController>();
            if (root == null || root.state == null || root.state.gameplayHUD == null) return false;

            // Central HUD Overlay Panel (Below center screen)
            panel = UIView.Panel("VehicleActiveOverlay", root.state.gameplayHUD.transform, new Vector2(420, 95), new Color(.02f, .04f, .08f, .94f));
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(.5f, .5f);
            panel.rectTransform.anchoredPosition = new Vector2(0, 160);
            panel.raycastTarget = false;

            titleText = UIView.Label("AbilityTitle", panel.transform, "", 18, new Vector2(400, 32), new Vector2(0, 22));
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(.92f, .94f, .98f);

            countText = UIView.Label("CountdownText", panel.transform, "", 38, new Vector2(400, 48), new Vector2(0, -16));
            countText.alignment = TextAnchor.MiddleCenter;
            countText.color = UIView.AccentYellow;

            panel.gameObject.SetActive(false);
            isInitialized = true;
            return true;
        }

        private void Start() => EnsureInitialized();

        public void Trigger(string title, float durationSeconds)
        {
            currentTitle = title;
            activeUntil = Time.time + durationSeconds;
            if (panel != null) panel.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (panel == null || !panel.gameObject.activeSelf) return;

            float remaining = activeUntil - Time.time;
            if (remaining <= 0f || (root != null && root.state != null && !root.state.IsGameplay))
            {
                panel.gameObject.SetActive(false);
                return;
            }

            int seconds = Mathf.CeilToInt(remaining);
            titleText.text = currentTitle;
            countText.text = seconds.ToString();

            // When <= 10 seconds remaining, text turns RED!
            countText.color = seconds <= 10 ? new Color(1f, 0.23f, 0.19f) : UIView.AccentYellow;
        }
    }
}
