using UnityEngine;
using UnityEngine.UI;
using Robot.UI.Settings;

namespace Robot.UI.Production
{
    [DefaultExecutionOrder(100)]
    public sealed class UIRootController : MonoBehaviour
    {
        public UIStateManager state;
        public MainMenuController menu;
        public RobotHubController hub;
        public PauseMenuController pause;
        public ConfirmDialogController confirm;
        public SettingsPanelController settings;
        public GraphicsSettingsController graphics;
        public AudioSettingsController audioSettings;
        public ControlsPanelController controls;
        public UILocalization localization;

        public Button menuPlay, menuSettings, menuControls, menuColor, menuQuit;
        public Button hubSolo, hubMultiplayer, hubBack;
        public Button startGame, hubSettings, hubControls, nextColor, language;
        public Button resume, pauseSettings, pauseControls, restart, quit;
        public Button confirmAction, cancel, nextRound, returnToHub;
        public Button settingsBack, controlsBack, settingsControls;
        public Button[] categories;

        private void Awake()
        {
            EnsureComponents();
            if (menu == null) menu = gameObject.AddComponent<MainMenuController>();
            if (state != null) menu.state = state;
            if (state != null && state.screens != null && state.loop == null && (int)UIScreen.Settings < state.screens.Length && state.screens[(int)UIScreen.Settings] == null)
            {
                SettingsScreenBuilder.Build(this, null, null);
                state.screens[(int)UIScreen.Settings].SetActive(false);
                state.screens[(int)UIScreen.Controls].SetActive(false);
            }
            if (state != null && state.inputGate != null)
            {
                if (GetComponent<PlayerVitalsUI>() == null) gameObject.AddComponent<PlayerVitalsUI>();
                if (Robot.Core.MapManager.SelectedMap.enableCityMechanics)
                {
                    if (GetComponent<PoliceRadarUI>() == null) gameObject.AddComponent<PoliceRadarUI>();
                    if (GetComponent<TaxiSpeedUI>() == null) gameObject.AddComponent<TaxiSpeedUI>();
                    if (GetComponent<VehicleCountdownHUD>() == null) gameObject.AddComponent<VehicleCountdownHUD>();
                }
                else if (GetComponent<MovementAbilityHUD>() == null) gameObject.AddComponent<MovementAbilityHUD>();
                if (GetComponent<KnockoutNotice>() == null) gameObject.AddComponent<KnockoutNotice>();
            }
            RefineGameplayUI();
            ApplyTypography();
            if (state != null && state.screens != null)
            {
                foreach (var screen in new[] { UIScreen.MainMenu, UIScreen.Pause, UIScreen.Settings, UIScreen.Controls, UIScreen.Confirm, UIScreen.Result })
                {
                    if ((int)screen >= state.screens.Length) continue;
                    var panel = state.screens[(int)screen];
                    if (panel == null) continue;
                    foreach (var button in panel.GetComponentsInChildren<Button>(true))
                        UIView.StyleButton(button, UIView.AccentYellow, true);
                }
            }
            foreach (var slider in GetComponentsInChildren<Slider>(true)) UIView.StyleSlider(slider);
        }

        public void RefineGameplayUI()
        {
            foreach (string name in new[] { "InteractionPrompt", "TutorialHint", "AmbulancePrompt", "PolicePrompt", "TaxiPrompt" })
            {
                var hint = transform.Find("GameplayHUD/" + name) as RectTransform;
                if (hint == null) continue;
                hint.sizeDelta = new Vector2(400, 62);
                hint.anchoredPosition = new Vector2(0, 145);
                var background = hint.GetComponent<Image>() ?? hint.gameObject.AddComponent<Image>();
                background.color = new Color(.035f,.035f,.035f,.94f);
                background.raycastTarget = false;
                var keycap = hint.Find("Keycap") as RectTransform;
                if (keycap != null) keycap.anchoredPosition = new Vector2(-140, 0);
                var label = hint.Find(name == "InteractionPrompt" ? "Action" : (name == "TutorialHint" ? "Hint" : (name.EndsWith("Prompt") ? name.Replace("Prompt","Hint") : "Label")) )?.GetComponent<Text>();
                if (label != null)
                {
                    label.rectTransform.anchoredPosition = new Vector2(52, 0);
                    label.rectTransform.sizeDelta = new Vector2(278, 44);
                }
            }
            if (resume != null)
            {
                resume.GetComponentInChildren<Text>().text = "RETURN TO GAME";
                Button[] ordered = { resume, restart, pauseSettings, pauseControls, quit };
                for (int i = 0; i < ordered.Length; i++)
                    if (ordered[i] != null) ordered[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 113 - i * 66);
            }
            if (state == null || state.intro == null) return;
            foreach (var card in state.intro.cards)
                if (card != null && card.TryGetComponent<Image>(out var image)) image.color = new Color(.055f,.055f,.055f,.98f);
        }

        public void ApplyTypography()
        {
            foreach (var label in GetComponentsInChildren<Text>(true)) UIView.ApplyFont(label);
        }

        private void EnsureComponents()
        {
            if (state == null) state = GetComponent<UIStateManager>();
            if (menu == null) menu = GetComponent<MainMenuController>();
            if (hub == null) hub = GetComponent<RobotHubController>();
            if (pause == null) pause = GetComponent<PauseMenuController>();
            if (confirm == null) confirm = GetComponent<ConfirmDialogController>();
            if (settings == null) settings = GetComponent<SettingsPanelController>();
            if (graphics == null) graphics = GetComponent<GraphicsSettingsController>();
            if (audioSettings == null) audioSettings = GetComponent<AudioSettingsController>();
            if (controls == null) controls = GetComponent<ControlsPanelController>();
            if (localization == null) localization = GetComponent<UILocalization>();
        }

        private void Start()
        {
            EnsureComponents();

            var manager = SettingsManager.Instance;
            if (manager == null) manager = new GameObject("SettingsManager").AddComponent<SettingsManager>();

            // Map scenes create this in their bootstrapper; the standalone lobby
            // must create it itself so music works on the very first launch.
            if (Robot.Audio.AudioManager.Instance == null)
                new GameObject("AudioManager").AddComponent<Robot.Audio.AudioManager>();
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (activeScene == "RoboSeek_Lobby" || activeScene == "UI_System_Demo")
                Robot.Audio.AudioManager.Instance?.PlayLobbyMusic();
            else
                Robot.Audio.AudioManager.Instance?.StopMusic();

            if (graphics != null) graphics.Initialize();
            if (audioSettings != null) audioSettings.Initialize(manager);
            if (settings != null) settings.Initialize(manager);

            SafeAddListener(menuPlay, () => menu?.Play());
            SafeAddListener(menuSettings, () => menu?.Settings());
            SafeAddListener(menuControls, () => menu?.Controls());
            SafeAddListener(menuColor, () => hub?.NextColor());
            SafeAddListener(menuQuit, () => menu?.Quit());

            SafeAddListener(hubSolo, () => hub?.SelectSolo());
            SafeAddListener(hubMultiplayer, () => hub?.SelectMultiplayer());
            SafeAddListener(hubBack, () => { if (state.loop == null) menu?.Quit(); else state.Back(); });

            SafeAddListener(startGame, () => hub?.StartGame());
            SafeAddListener(hubSettings, () => state?.Open(UIScreen.Settings));
            SafeAddListener(hubControls, () => state?.Open(UIScreen.Controls));
            SafeAddListener(nextColor, () => hub?.NextColor());
            SafeAddListener(language, () => hub?.NextMap());

            SafeAddListener(resume, () => pause?.Resume());
            SafeAddListener(pauseSettings, () => pause?.Settings());
            SafeAddListener(pauseControls, () => pause?.Controls());
            SafeAddListener(restart, () => pause?.Restart());
            SafeAddListener(quit, () => pause?.QuitToLobby());

            SafeAddListener(confirmAction, () => confirm?.Confirm());
            SafeAddListener(cancel, () => confirm?.Cancel());

            SafeAddListener(nextRound, () => state?.NextRound());
            SafeAddListener(returnToHub, () => state?.ReturnToHub());

            SafeAddListener(settingsBack, () => state?.Back());
            SafeAddListener(controlsBack, () => state?.Back());
            SafeAddListener(settingsControls, () => settings?.OpenControls());

            if (categories != null)
            {
                for (int i = 0; i < categories.Length; i++)
                {
                    int category = i;
                    if (categories[i] != null)
                    {
                        categories[i].onClick.RemoveAllListeners();
                        categories[i].onClick.AddListener(() => settings?.SelectCategory(category));
                    }
                }
            }

            if (state != null)
            {
                state.Changed -= OnState;
                state.Changed += OnState;
            }

            if (controls != null) controls.Refresh();
            if (hub != null) hub.Refresh();
        }

        private void SafeAddListener(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null && action != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
            }
        }

        private void OnState()
        {
            if (state == null) return;
            if ((state.Current == UIScreen.Controls || state.Current == UIScreen.Settings) && controls != null) controls.Refresh();
            if ((state.Current == UIScreen.MainMenu || state.Current == UIScreen.Hub || state.Current == UIScreen.Result) && hub != null) hub.Refresh();
        }

        private void OnDestroy()
        {
            if (state != null) state.Changed -= OnState;
        }
    }
}
