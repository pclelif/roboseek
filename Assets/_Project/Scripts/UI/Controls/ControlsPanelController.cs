using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Robot.Input;

namespace Robot.UI.Production
{
    public sealed class ControlsPanelController : MonoBehaviour
    {
        public PlayerInputReader input;
        public Text[] keys;
        public Text[] settingBindings;

        private static readonly string[] Actions = { "Move", "Jump", "Look", "Interact", "Attack", "Heal", "Radar", "SpeedBoost", "Pause" };
        private static readonly string[] Defaults = { "W A S D / ARROWS", "SPACE", "MOUSE", "E", "Q", "H", "R", "T", "ESC" };
        private static readonly string[] DisplayLabels = { "MOVE", "JUMP", "CAMERA", "SCAN & RETRIEVE", "ATTACK", "HEAL", "RADAR", "TURBO", "PAUSE" };

        public void EnsureHealingRow()
        {
            if (keys == null || keys.Length == 9) return;
            var panel = keys[0] != null ? keys[0].transform.parent.parent.parent : null;
            if (panel == null) return;

            // Clear old dynamic elements and rebuild all 9 rows cleanly
            System.Array.Resize(ref keys, 9);
            for (int i = 0; i < 9; i++)
            {
                float y = 195 - i * 48;
                var actionLabel = panel.Find("Action" + i)?.GetComponent<Text>();
                if (actionLabel == null)
                {
                    actionLabel = UIView.Label("Action" + i, panel, DisplayLabels[i], 17, new Vector2(260, 40), new Vector2(135, y));
                }
                else
                {
                    actionLabel.text = DisplayLabels[i];
                    actionLabel.rectTransform.anchoredPosition = new Vector2(135, y);
                }

                if (keys[i] == null)
                {
                    keys[i] = UIView.Keycap(panel, Defaults[i], new Vector2(-135, y), new Vector2(225, 40));
                }
                else
                {
                    keys[i].rectTransform.parent.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(-135, y);
                }
            }

            if (settingBindings != null)
            {
                System.Array.Resize(ref settingBindings, 9);
                for (int i = 0; i < 9; i++)
                {
                    if (settingBindings[i] == null && settingBindings[0] != null)
                    {
                        settingBindings[i] = UIView.Label("Binding" + i, settingBindings[0].transform.parent, "", 14, new Vector2(300, 28));
                    }
                    if (settingBindings[i] != null)
                    {
                        settingBindings[i].rectTransform.anchoredPosition = new Vector2(i % 2 == 0 ? -160 : 160, 48 - (i / 2) * 36);
                    }
                }
            }
        }

        public void Refresh()
        {
            EnsureHealingRow();
            var map = input != null && input.Actions != null ? input.Actions.FindActionMap("Player", false) : null;
            for (int i = 0; i < keys.Length && i < Defaults.Length; i++)
            {
                var action = map?.FindAction(Actions[i], false);
                string value = Defaults[i];
                if (i > 0 && i < 5 && action != null)
                {
                    foreach (var binding in action.bindings)
                    {
                        if (binding.isComposite || binding.isPartOfComposite || string.IsNullOrEmpty(binding.effectivePath)) continue;
                        if (!binding.effectivePath.StartsWith("<Keyboard>") && !binding.effectivePath.StartsWith("<Mouse>")) continue;
                        value = i == 2 ? "MOUSE" : InputControlPath.ToHumanReadableString(binding.effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice).ToUpperInvariant();
                        break;
                    }
                }
                string label = DisplayLabels[i];
                bool visible = true;
                var traversal = Robot.Core.MapManager.SelectedMap.Traversal;
                if (!Robot.Core.MapManager.SelectedMap.enableCityMechanics)
                {
                    if (i == 1) label = traversal == Robot.Core.MapTraversal.DoubleJump ? "JUMP / DOUBLE JUMP" : "JUMP";
                    if (i == 5) { value = "SHIFT"; label = "SPRINT"; }
                    if (i == 6)
                    {
                        value = traversal == Robot.Core.MapTraversal.Dash ? "F" : "SPACE";
                        label = traversal == Robot.Core.MapTraversal.Dash ? "DASH" : traversal == Robot.Core.MapTraversal.Jetpack ? "HOLD: JETPACK / RELEASE: GLIDE" : "PRESS AGAIN: AIR JUMP";
                    }
                    if (i == 7) visible = false;
                }
                if (keys[i] != null)
                {
                    keys[i].text = value;
                    var keycap = keys[i].transform.parent.parent;
                    // Keycap() returns Key inside Face inside the cap rectangle.
                    keycap.gameObject.SetActive(visible);
                    var panel = keycap.parent;
                    var actionLabel = panel.Find("Action" + i)?.GetComponent<Text>();
                    if (actionLabel != null) { actionLabel.text = label; actionLabel.gameObject.SetActive(visible); }
                }
                if (settingBindings != null && i < settingBindings.Length && settingBindings[i] != null)
                {
                    settingBindings[i].text = value + "   " + label;
                    settingBindings[i].gameObject.SetActive(visible);
                }
            }
        }
    }
}
