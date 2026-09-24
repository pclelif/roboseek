using UnityEngine;
using UnityEngine.UI;
using Robot.Robots.Customization;

namespace Robot.UI.Production
{
    public enum GameMode { None, Solo, Multiplayer }

    public sealed class RobotHubController : MonoBehaviour
    {
        public UIStateManager state;
        public RobotColorCustomizer robot;
        public Text colorName;
        public Text modeDescription;
        public RobotShowcaseController showcase;

        public Button soloButton;
        public Button multiplayerButton;
        public Button startGameButton;
        public Button mapButton;
        public Text mapNameText;

        // Solo is immediately playable when entering the lobby.
        public GameMode CurrentMode { get; private set; } = GameMode.Solo;

        private static readonly Color InactiveSurface = new Color(.14f, .16f, .20f, .95f);
        private static readonly Color DisabledSurface = new Color(0.24f, 0.24f, 0.26f, 0.7f);

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                Robot.Core.MapManager.ResetToDefaultMap();
                if (robot != null)
                {
                    robot.ApplyTheme(RobotColorCustomizer.ColorTheme.Sari);
                }
            }
            if (robot != null) robot.ThemeChanged += OnColor;
            Robot.Core.MapManager.MapChanged += OnMapChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (robot != null) robot.ThemeChanged -= OnColor;
            Robot.Core.MapManager.MapChanged -= OnMapChanged;
        }

        private void OnMapChanged(Robot.Core.MapDefinition map)
        {
            if (map != null && robot != null)
            {
                switch (map.mapId)
                {
                    case Robot.Core.MapType.City:
                        robot.SelectSinglePlayerColor((int)RobotColorCustomizer.ColorTheme.Sari);
                        break;
                    case Robot.Core.MapType.Adventure:
                        robot.SelectSinglePlayerColor((int)RobotColorCustomizer.ColorTheme.Yesil);
                        break;
                    case Robot.Core.MapType.Polygon:
                        robot.SelectSinglePlayerColor((int)RobotColorCustomizer.ColorTheme.Kirmizi);
                        break;
                    case Robot.Core.MapType.PolygonStarter:
                        robot.SelectSinglePlayerColor((int)RobotColorCustomizer.ColorTheme.Mavi);
                        break;
                }
            }
            Refresh();
        }

        public void NextColor()
        {
            if (robot != null) robot.NextTheme();
            Refresh();
        }

        public void NextMap()
        {
            Robot.Core.MapManager.SelectNextMap();
            Refresh();
        }

        public void SelectSolo()
        {
            CurrentMode = GameMode.Solo;
            Refresh();
        }

        public void SelectMultiplayer()
        {
            CurrentMode = GameMode.Multiplayer;
            Refresh();
        }

        public void StartGame()
        {
            if (CurrentMode != GameMode.Solo) return;
            Robot.Audio.AudioManager.Instance?.StopMusic();
            if (state != null) state.StartGame();
        }

        public void Refresh()
        {
            var activeMap = Robot.Core.MapManager.SelectedMap;

            if (colorName != null && robot != null)
            {
                string english = UILocalization.EnglishColor(robot.ActiveThemeName);
                colorName.text = UILocalization.IsTurkish ? UILocalization.Translate(english) : english;
            }

            if (colorName != null && robot != null)
            {
                var button = colorName.GetComponentInParent<Button>();
                if (button != null)
                {
                    StyleButton(button, robot.ActiveThemeColor, true);
                    colorName.color = robot.ActiveThemeColor.grayscale > .48f ? new Color(.06f,.06f,.06f) : Color.white;
                }
            }

            string localizedMapName = UILocalization.Translate(activeMap.displayName);
            string mapPrefix = UILocalization.Translate("MAP");

            if (mapNameText != null)
            {
                mapNameText.text = $"{mapPrefix}: {localizedMapName}";
            }

            if (mapButton != null)
            {
                StyleButton(mapButton, activeMap.themeColor, true);
                var btnLabel = mapButton.GetComponentInChildren<Text>();
                if (btnLabel != null)
                {
                    btnLabel.text = $"{mapPrefix}: {localizedMapName}";
                    btnLabel.color = activeMap.themeColor.grayscale > 0.48f ? new Color(.07f, .07f, .07f) : Color.white;
                }
            }

            if (showcase != null) showcase.Refresh();

            RefreshModeSelector();
        }

        private void RefreshModeSelector()
        {
            bool isSolo = CurrentMode == GameMode.Solo;
            bool isMulti = CurrentMode == GameMode.Multiplayer;
            bool hasMode = isSolo;
            Color mapTheme = Robot.Core.MapManager.SelectedMap.themeColor;

            if (soloButton != null)
            {
                StyleButton(soloButton, isSolo ? mapTheme : InactiveSurface, isSolo);
            }

            if (multiplayerButton != null)
            {
                StyleButton(multiplayerButton, isMulti ? mapTheme : InactiveSurface, isMulti);
            }

            if (startGameButton != null)
            {
                startGameButton.interactable = hasMode;
                StyleButton(startGameButton, hasMode ? mapTheme : DisabledSurface, hasMode);
            }

            if (modeDescription != null)
            {
                if (isSolo) modeDescription.text = UILocalization.Translate("SOLO: Search for hidden toys alone!");
                else if (isMulti) modeDescription.text = UILocalization.Translate("MULTIPLAYER: Co-op mode coming soon!");
                else modeDescription.text = UILocalization.Translate("Select a Game Mode to begin.");
            }
        }

        private static void StyleButton(Button button, Color surface, bool active)
            => UIView.StyleButton(button, surface, active);

        private void OnColor(RobotColorCustomizer.ColorTheme theme) => Refresh();
    }
}
