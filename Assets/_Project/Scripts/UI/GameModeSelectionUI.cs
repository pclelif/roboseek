using UnityEngine;
using UnityEngine.SceneManagement;
using Robot.GameMode;
using Robot.UI.HUD;

namespace Robot.UI
{
    /// <summary>
    /// UI component for selecting and switching between Singleplayer (Demo) and Multiplayer (Demo2).
    /// </summary>
    public sealed class GameModeSelectionUI : MonoBehaviour
    {
        [SerializeField] private bool showModeSwitcher = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.M;

        private bool isOpen = false;
        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle activeBadgeStyle;
        private Texture2D panelBackground;
        private Texture2D buttonBackground;
        private Texture2D buttonHoverBackground;

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(toggleKey))
            {
                isOpen = !isOpen;
            }
        }

        private void OnGUI()
        {
            if (!showModeSwitcher) return;

            string sceneName = SceneManager.GetActiveScene().name;
            bool isMultiplayerScene = sceneName.Contains("Demo2") || sceneName.Contains("DEMO-2") || sceneName.Contains("Multiplayer");

            // Top-right mode quick indicator button
            if (GUI.Button(new Rect(Screen.width - 170, 12, 158, 32), isMultiplayerScene ? "🌐 MULTIPLAYER (M)" : "👤 SINGLEPLAYER (M)"))
            {
                isOpen = !isOpen;
            }

            if (!isOpen) return;

            // Modal dialog
            InitStyles();
            float panelWidth = 360f;
            float panelHeight = 220f;
            Rect rect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

            GUI.Box(rect, GUIContent.none, boxStyle);
            GUILayout.BeginArea(rect);
            GUILayout.Space(12);

            GUILayout.Label("<b>🎮 OYUN MODU SEÇİMİ</b>", titleStyle);
            GUILayout.Space(12);

            // Singleplayer Button
            string spText = !isMultiplayerScene ? "<b>[AKTİF] 👤 SINGLEPLAYER (Demo)</b>" : "👤 SINGLEPLAYER'a Geç (Demo)";
            if (GUILayout.Button(spText, buttonStyle, GUILayout.Height(42)))
            {
                isOpen = false;
                if (isMultiplayerScene)
                {
                    if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
                    {
                        Unity.Netcode.NetworkManager.Singleton.Shutdown();
                    }
                    GameModeManager.LoadSingleplayer();
                }
            }

            GUILayout.Space(8);

            // Multiplayer Button
            string mpText = isMultiplayerScene ? "<b>[AKTİF] 🌐 MULTIPLAYER (Demo2)</b>" : "🌐 MULTIPLAYER'a Geç (Demo2)";
            if (GUILayout.Button(mpText, buttonStyle, GUILayout.Height(42)))
            {
                isOpen = false;
                if (!isMultiplayerScene)
                {
                    GameModeManager.LoadMultiplayer();
                }
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Kapat (M)", GUILayout.Height(28)))
            {
                isOpen = false;
            }

            GUILayout.EndArea();
        }

        private void InitStyles()
        {
            if (boxStyle == null)
            {
                panelBackground = new Texture2D(1, 1);
                panelBackground.SetPixel(0, 0, new Color(0.1f, 0.12f, 0.16f, 0.95f));
                panelBackground.Apply();

                buttonBackground = new Texture2D(1, 1);
                buttonBackground.SetPixel(0, 0, RobotHudTheme.ControlColor);
                buttonBackground.Apply();

                buttonHoverBackground = new Texture2D(1, 1);
                buttonHoverBackground.SetPixel(0, 0, RobotHudTheme.ControlHoverColor);
                buttonHoverBackground.Apply();

                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.background = panelBackground;

                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    richText = true
                };
                titleStyle.normal.textColor = Color.white;

                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
                buttonStyle.normal.background = buttonBackground;
                buttonStyle.hover.background = buttonHoverBackground;
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.hover.textColor = Color.white;
            }
        }

        private void OnDestroy()
        {
            if (panelBackground != null) Destroy(panelBackground);
            if (buttonBackground != null) Destroy(buttonBackground);
            if (buttonHoverBackground != null) Destroy(buttonHoverBackground);
        }
    }
}
