using UnityEngine;
using Robot.Robots.Customization;
using Robot.Player;
using Robot.Player.Movement;

namespace Robot.UI.HUD
{
    /// <summary>
    /// Temporary Play Mode controls. Color selection calls the input-independent customizer API.
    /// </summary>
    public class RobotShowcaseUI : MonoBehaviour
    {
        [SerializeField] private RobotColorCustomizer colorCustomizer;
        [SerializeField] private RobotAnimator robotAnimator;
        [SerializeField] private KeyCode changeColorKey = KeyCode.C;

        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;
        private GUIStyle panelStyle;
        private Texture2D panelTexture;
        private Texture2D buttonNormalTexture;
        private Texture2D buttonHoverTexture;
        private Texture2D buttonActiveTexture;

        private void Start()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (colorCustomizer == null)
            {
                colorCustomizer = GetComponent<RobotColorCustomizer>();
                if (colorCustomizer == null)
                {
                    colorCustomizer = FindFirstObjectByType<RobotColorCustomizer>();
                }
            }

            if (robotAnimator == null)
            {
                robotAnimator = GetComponent<RobotAnimator>();
                if (robotAnimator == null)
                {
                    robotAnimator = FindFirstObjectByType<RobotAnimator>();
                }
            }
        }

        private void Update()
        {
            if (!ShouldHandleUI()) return;

            if (IsChangeColorKeyPressed())
            {
                TriggerNextColor();
            }
        }

        private bool ShouldHandleUI()
        {
            var netObj = GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null)
            {
                if (!netObj.IsSpawned || !netObj.IsOwner) return false;
            }
            return true;
        }

        public void TriggerNextColor()
        {
            EnsureReferences();
            if (colorCustomizer != null)
            {
                colorCustomizer.NextTheme();
            }
            else
            {
                Debug.LogWarning("[RobotShowcaseUI] RobotColorCustomizer component not found!");
            }
        }

        private bool IsChangeColorKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null && keyboard.cKey.wasPressedThisFrame) return true;
            }
            catch { }
#endif

            try
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.C)) return true;
            }
            catch { }

            return false;
        }

        private void OnGUI()
        {
            if (!ShouldHandleUI()) return;

            // On-Screen Control & Status Box
            GUILayout.BeginArea(new Rect(20, 20, 340, 280), GetPanelStyle());
            
            GUILayout.Label("<b>🤖 ROBOT TEST</b>", GetHeaderStyle());
            GUILayout.Space(8);

            GUILayout.Label("<b>WASD / Ok Tuşları:</b> Yürü", GetBodyStyle());
            GUILayout.Label("<b>Shift:</b> Koş", GetBodyStyle());
            GUILayout.Label("<b>Space:</b> Zıpla", GetBodyStyle());
            GUILayout.Label("<b>Fare (Sağ Tık / Sürükle):</b> Kamera Döndür", GetBodyStyle());
            GUILayout.Label("<b>C:</b> Renk Değiştir", GetBodyStyle());
            GUILayout.Label("<b>Q:</b> Saldır", GetBodyStyle());
            GUILayout.Space(10);

            string activeThemeName = colorCustomizer != null ? colorCustomizer.ActiveThemeName : "Yükleniyor...";
            string activeThemeHex = colorCustomizer != null
                ? ColorUtility.ToHtmlStringRGB(colorCustomizer.ActiveThemeColor)
                : "FFFFFF";
            GUILayout.Label($"<b>Aktif Renk Teması:</b> <color=#{activeThemeHex}>{activeThemeName}</color>", GetBodyStyle());
            GUILayout.Space(10);

            if (GUILayout.Button("Renk Değiştir", GetButtonStyle(), GUILayout.Height(40)))
            {
                TriggerNextColor();
            }

            GUILayout.EndArea();
        }

        private GUIStyle GetHeaderStyle()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
            }
            return headerStyle;
        }

        private GUIStyle GetBodyStyle()
        {
            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    richText = true
                };
            }
            return bodyStyle;
        }

        private GUIStyle GetButtonStyle()
        {
            if (buttonStyle == null)
            {
                buttonNormalTexture = CreateSolidTexture("HUD Button Normal", RobotHudTheme.ControlColor);
                buttonHoverTexture = CreateSolidTexture("HUD Button Hover", RobotHudTheme.ControlHoverColor);
                buttonActiveTexture = CreateSolidTexture("HUD Button Active", RobotHudTheme.ControlActiveColor);
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    richText = true
                };
                buttonStyle.normal.background = buttonNormalTexture;
                buttonStyle.hover.background = buttonHoverTexture;
                buttonStyle.active.background = buttonActiveTexture;
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.hover.textColor = Color.white;
                buttonStyle.active.textColor = Color.white;
            }
            return buttonStyle;
        }

        private GUIStyle GetPanelStyle()
        {
            if (panelStyle == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Robot HUD Panel Background" };
                panelTexture.SetPixel(0, 0, RobotHudTheme.PanelColor);
                panelTexture.Apply();
                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
            }
            return panelStyle;
        }

        private void OnDestroy()
        {
            if (panelTexture != null) Destroy(panelTexture);
            if (buttonNormalTexture != null) Destroy(buttonNormalTexture);
            if (buttonHoverTexture != null) Destroy(buttonHoverTexture);
            if (buttonActiveTexture != null) Destroy(buttonActiveTexture);
        }

        private static Texture2D CreateSolidTexture(string textureName, Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = textureName };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
