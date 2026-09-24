using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Robot.ObjectHunt;

namespace Robot.UI.Production
{
    public enum UIScreen { MainMenu, Hub, Intro, Countdown, Gameplay, Pause, Settings, Controls, Confirm, Result }

    [DefaultExecutionOrder(-100)]
    public sealed class UIStateManager : MonoBehaviour
    {
        // Runtime scripts cannot reference editor-only inspection helpers.
        private const string GameplayScenePath = "Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity";

        public RoundGameLoop loop;
        public GameplayInputGate inputGate;
        public CursorStateController cursor;
        public GameObject[] screens;
        public CanvasGroup gameplayHUD;
        public InteractionPromptUI prompt;
        public RoundIntroController intro;
        public ResultScreenController result;
        public TutorialHintUI tutorial;

        public UIScreen Current { get; private set; } = UIScreen.MainMenu;
        public bool IsGameplay => Current == UIScreen.Gameplay && (loop == null || loop.CurrentPhase == RoundPhase.Search);
        public event Action Changed;

        private readonly Stack<UIScreen> history = new Stack<UIScreen>();
        private float previousTimeScale = 1f;
        private bool ownsTime;
        private bool enteredHub;
        private InputAction escape;
        // The lobby is a separate showcase scene.  This hand-off prevents the gameplay
        // scene's legacy main menu from flashing after the player pressed START GAME.
        private static bool launchGameplayFromDedicatedLobby;

        private void EnsureDependencies()
        {
            if (loop == null) loop = UnityEngine.Object.FindFirstObjectByType<RoundGameLoop>();
        }

        private void OnEnable()
        {
            EnsureDependencies();
            if (loop != null)
            {
                loop.PhaseChanged -= OnPhase;
                loop.PhaseChanged += OnPhase;
                loop.ResultReady -= OnResult;
                loop.ResultReady += OnResult;
            }
            escape = new InputAction("UI Escape", InputActionType.Button, "<Keyboard>/escape");
            escape.performed += OnEscape;
            escape.Enable();
        }

        private void OnEscape(InputAction.CallbackContext context) => Back();

        private void Start()
        {
            previousTimeScale = Time.timeScale > 0 ? Time.timeScale : 1f;
            ownsTime = true;
            EnsureDependencies();

            // Older saved UI scenes predate localization and the themed lobby setup.
            // Upgrade them at runtime too, so users are never required to rebuild a
            // scene just to see a UI update.
            if (GetComponent<UILocalization>() == null) gameObject.AddComponent<UILocalization>();
            bool isDedicatedLobby = !HasScreen(UIScreen.MainMenu) && HasScreen(UIScreen.Hub);
            if (isDedicatedLobby && GetComponent<LobbyRuntimeTheme>() == null) gameObject.AddComponent<LobbyRuntimeTheme>();

            if (launchGameplayFromDedicatedLobby)
            {
                launchGameplayFromDedicatedLobby = false;
                enteredHub = true;
                if (loop != null) loop.StartGame();
                Open(UIScreen.Gameplay);
                return;
            }

            // The dedicated lobby only supplies a Hub screen. Do not select the
            // absent MainMenu screen and hide the entire lobby interface.
            if (!enteredHub && !HasScreen(UIScreen.MainMenu) && HasScreen(UIScreen.Hub))
                enteredHub = true;

            if (loop != null)
            {
                if (loop.CurrentPhase == RoundPhase.Lobby && !enteredHub) Set(UIScreen.MainMenu);
                else OnPhase(loop.CurrentPhase);
            }
            else
            {
                Set(enteredHub ? UIScreen.Hub : UIScreen.MainMenu);
            }
        }

        private void OnDisable()
        {
            if (loop != null)
            {
                loop.PhaseChanged -= OnPhase;
                loop.ResultReady -= OnResult;
            }
            escape?.Dispose();
            escape = null;
            if (ownsTime) Time.timeScale = previousTimeScale;
        }

        public void Open(UIScreen screen)
        {
            if (screen == Current) return;
            history.Push(Current);
            Set(screen);
        }

        public void EnterHubFromMainMenu()
        {
            enteredHub = true;
            history.Clear();
            Set(UIScreen.Hub);
        }

        public void Back()
        {
            if (Current == UIScreen.Gameplay) { Open(UIScreen.Pause); return; }
            if (history.Count > 0) { Set(history.Pop()); return; }
            if (Current == UIScreen.Hub && HasScreen(UIScreen.MainMenu)) { Set(UIScreen.MainMenu); return; }
        }

        public void Resume()
        {
            if (loop != null && loop.CurrentPhase != RoundPhase.Search) return;
            history.Clear();
            Set(UIScreen.Gameplay);
        }

        public void StartGame()
        {
            Robot.Audio.AudioManager.IsGameStarting = true;
            Robot.Audio.AudioManager.Instance?.StopMusic();
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == "RoboSeek_Lobby" || loop == null)
            {
                // In standalone lobby scene: load main gameplay scene based on selected map
                launchGameplayFromDedicatedLobby = true;
                var map = Robot.Core.MapManager.SelectedMap;
                string sceneToLoad = !string.IsNullOrEmpty(map.scenePath) ? map.scenePath : map.sceneName;
                Debug.Log($"[UIStateManager] Loading map scene: {map.displayName} -> '{sceneToLoad}'");
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                if (loop != null) loop.StartGame();
                Open(UIScreen.Gameplay);
                if (inputGate != null) inputGate.SetBlocked(false);
            }
        }

        public void RestartRound() { history.Clear(); loop?.RestartRound(); }
        public void ReturnToHub()
        {
            Robot.Audio.AudioManager.IsGameStarting = false;
            history.Clear();
            launchGameplayFromDedicatedLobby = false;
            Time.timeScale = previousTimeScale;
            SceneManager.LoadScene("Assets/_Project/Scenes/RoboSeek_Lobby.unity");
        }
        public void NextRound() { history.Clear(); loop?.StartNextRound(); }

        private void OnPhase(RoundPhase phase)
        {
            history.Clear();
            switch (phase)
            {
                case RoundPhase.Lobby: inputGate?.ResetTransientInput(); intro?.Hide(); Set(enteredHub ? UIScreen.Hub : UIScreen.MainMenu); break;
                case RoundPhase.Intro: inputGate?.ResetTransientInput(); inputGate?.GroundForRoundIntro(); intro?.Begin(); Set(UIScreen.Intro); break;
                case RoundPhase.Targets: if (intro != null && inputGate?.hunt != null) intro.ShowRoundIntro(inputGate.hunt.SelectedTargets); Set(UIScreen.Intro); break;
                case RoundPhase.Countdown: intro?.BeginCountdown(); Set(UIScreen.Countdown); break;
                case RoundPhase.Search: intro?.Hide(); Set(UIScreen.Gameplay); break;
                case RoundPhase.RoundComplete: result?.ShowPending(); Set(UIScreen.Result); break;
                case RoundPhase.Result: Set(UIScreen.Result); break;
            }
        }

        private void OnResult(RoundResultData data) { result?.Show(data); }

        private bool HasScreen(UIScreen screen)
        {
            return screens != null && (int)screen < screens.Length && screens[(int)screen] != null;
        }

        private void Set(UIScreen screen)
        {
            Current = screen;
            if (screens != null)
            {
                for (int i = 0; i < screens.Length; i++)
                {
                    if (screens[i] != null) screens[i].SetActive(i == (int)screen);
                }
            }
            UIView.Visible(gameplayHUD, IsGameplay);

            bool network = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening;
            Time.timeScale = IsGameplay || network ? previousTimeScale : 0f;

            if (inputGate != null) inputGate.SetBlocked(!IsGameplay);
            if (cursor != null) cursor.SetGameplay(IsGameplay);
            if (!IsGameplay && prompt != null) prompt.HideImmediately();
            if (tutorial != null) tutorial.SetGameplay(IsGameplay);

            if (EventSystem.current != null && screens != null && (int)screen < screens.Length)
            {
                EventSystem.current.SetSelectedGameObject(null);
                var panel = screens[(int)screen];
                var button = panel != null ? panel.GetComponentInChildren<UnityEngine.UI.Button>() : null;
                if (button != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
            Changed?.Invoke();
        }
    }
}
