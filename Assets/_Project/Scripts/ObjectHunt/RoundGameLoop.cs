using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.ObjectHunt
{
    public enum RoundPhase { Lobby, Intro, Targets, Countdown, Search, RoundComplete, Result }

    [Serializable]
    public sealed class RoundResultData
    {
        public int roundNumber;
        public bool completed;
        public int foundCount;
        public float elapsedTime;
        public float remainingTime;
        public int roundScore;
        public int totalScore;
        public List<string> foundObjectIds = new List<string>();
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(ObjectHuntRoundManager))]
    public sealed class RoundGameLoop : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private bool autoStart = true;
        [SerializeField, Min(0f)] private float introDuration = 0.6f;
        [SerializeField, Min(0f)] private float targetPreviewDuration = 2.2f;
        [SerializeField, Range(1, 10)] private int countdownFrom = 3;
        [SerializeField, Min(0f)] private float roundCompleteDuration = 1.4f;

        [SerializeField] private bool useUnscaledPresentation;

        [Header("Score")]
        [SerializeField, Min(0)] private int scorePerObject = 500;
        [SerializeField, Min(0)] private int completionBonus = 1500;
        [SerializeField, Min(0f)] private float scorePerRemainingSecond = 10f;

        private ObjectHuntRoundManager hunt;
        private Coroutine flowRoutine;

        public event Action<RoundPhase> PhaseChanged;
        public event Action<int> CountdownChanged;
        public event Action<RoundResultData> ResultReady;

        public RoundPhase CurrentPhase { get; private set; } = RoundPhase.Lobby;
        public int RoundNumber { get; private set; }
        public int TotalScore { get; private set; }
        public RoundResultData LastResult { get; private set; }

        // UI presentation intentionally pauses world input during the intro.
        // Keep the intro/countdown clock independent from Time.timeScale so a
        // lobby-to-map handoff can always reach the playable Search phase.
        public void UseRealtimePresentation() => useUnscaledPresentation = true;

        private void Awake() => hunt = GetComponent<ObjectHuntRoundManager>();

        private void OnEnable()
        {
            hunt.RoundCompleted += HandleRoundCompleted;
            hunt.RoundFailed += HandleRoundFailed;
        }

        private void Start()
        {
            if (CurrentPhase != RoundPhase.Lobby) return;
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName == "RoboSeek_Lobby" || sceneName == "UI_System_Demo")
            {
                SetPhase(RoundPhase.Lobby);
            }
            else
            {
                Robot.Audio.AudioManager.Instance?.StopMusic();
            }
            if (autoStart) StartGame();
        }

        private void OnDisable()
        {
            hunt.RoundCompleted -= HandleRoundCompleted;
            hunt.RoundFailed -= HandleRoundFailed;
        }

        public void StartGame()
        {
            Robot.Audio.AudioManager.Instance?.StopMusic();
            if (CurrentPhase != RoundPhase.Lobby && CurrentPhase != RoundPhase.Result) return;
            StartNextRound();
        }

        private object PresentationDelay(float duration) => useUnscaledPresentation
            ? (object)new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        public void RestartRound()
        {
            hunt.EndRound();
            var player = GameObject.FindGameObjectWithTag("Player");
            ResetPlayerForRound();
            Robot.Score.ScoreManager.Instance?.DiscardRoundProgress();
            RoundNumber = Mathf.Max(0, RoundNumber - 1);
            StartNextRound();
        }

        private void ResetPlayerForRound()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            player.GetComponent<Robot.Combat.CombatAttack>()?.CancelPendingAttack();
            player.GetComponent<Robot.Combat.CombatHealth>()?.ResetForRound();

            var rmc = player.GetComponent<Robot.Player.Movement.RobotMovementController>();
            if (rmc != null) rmc.ReturnToSpawn();

            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                bool wasEnabled = controller.enabled;
                controller.enabled = false;
                Vector3 pos = player.transform.position;
                if (Physics.Raycast(pos + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5.0f, ~0, QueryTriggerInteraction.Ignore))
                {
                    player.transform.position = hit.point + Vector3.up * 0.02f;
                }
                Physics.SyncTransforms();
                controller.enabled = wasEnabled;
            }
            rmc?.ResetGroundedMotion();

            foreach (var camera in FindObjectsByType<Robot.Player.CameraControl.ThirdPersonCameraController>(FindObjectsSortMode.None))
                camera.SnapToRoundStart(player.transform);
        }

        public void ReturnToLobby()
        {
            if (flowRoutine != null) StopCoroutine(flowRoutine);
            flowRoutine = null;
            hunt.EndRound();
            Robot.Score.ScoreManager.Instance?.DiscardRoundProgress();
            SetPhase(RoundPhase.Lobby);
        }

        public void StartNextRound()
        {
            if (flowRoutine != null) StopCoroutine(flowRoutine);
            flowRoutine = StartCoroutine(RoundStartRoutine());
        }

        private IEnumerator RoundStartRoutine()
        {
            hunt.StopSearch();
            RoundNumber++;
            Robot.Score.ScoreManager.Instance?.NextRound();
            ResetPlayerForRound();
            SetPhase(RoundPhase.Intro);
            yield return PresentationDelay(introDuration);

            if (hunt == null) hunt = GetComponent<ObjectHuntRoundManager>() ?? FindFirstObjectByType<ObjectHuntRoundManager>();

            if (hunt != null)
            {
                bool prepared = hunt.PrepareRound();
                if (!prepared)
                {
                    yield return PresentationDelay(0.5f);
                    prepared = hunt.PrepareRound();
                }
                if (!prepared)
                {
                    SetPhase(RoundPhase.Lobby);
                    flowRoutine = null;
                    yield break;
                }
            }

            SetPhase(RoundPhase.Targets);
            yield return PresentationDelay(targetPreviewDuration);
            SetPhase(RoundPhase.Countdown);
            for (int value = countdownFrom; value >= 1; value--)
            {
                CountdownChanged?.Invoke(value);
                Robot.Audio.AudioManager.Instance?.PlayCountdownTick();
                yield return PresentationDelay(1f);
            }
            ResetPlayerForRound();
            CountdownChanged?.Invoke(0);
            Robot.Audio.AudioManager.Instance?.PlayCountdownGo();
            yield return PresentationDelay(0.4f);
            SetPhase(RoundPhase.Search);
            hunt.StartSearch();
            flowRoutine = null;
        }

        private void HandleRoundCompleted()
        {
            if (CurrentPhase != RoundPhase.Search) return;
            hunt.StopSearch();
            if (flowRoutine != null) StopCoroutine(flowRoutine);
            flowRoutine = StartCoroutine(CompleteRoutine());
        }

        private IEnumerator CompleteRoutine()
        {
            SetPhase(RoundPhase.RoundComplete);
            Robot.Audio.AudioManager.Instance?.PlayTargetCompleteFanfare();
            yield return PresentationDelay(roundCompleteDuration);
            BuildResult(true);
            SetPhase(RoundPhase.Result);
            ResultReady?.Invoke(LastResult);
            flowRoutine = null;
        }

        private void HandleRoundFailed()
        {
            if (CurrentPhase != RoundPhase.Search) return;
            hunt.StopSearch();
            BuildResult(false);
            SetPhase(RoundPhase.Result);
            ResultReady?.Invoke(LastResult);
        }

        private void BuildResult(bool completed)
        {
            int roundScore = hunt.CollectedCount * scorePerObject;
            if (completed)
                roundScore += completionBonus + Mathf.RoundToInt(hunt.TimeRemaining * scorePerRemainingSecond);
            TotalScore += roundScore;
            LastResult = new RoundResultData
            {
                roundNumber = RoundNumber,
                completed = completed,
                foundCount = hunt.CollectedCount,
                elapsedTime = hunt.RoundDuration - hunt.TimeRemaining,
                remainingTime = hunt.TimeRemaining,
                roundScore = roundScore,
                totalScore = TotalScore
            };
            foreach (TargetDefinition target in hunt.CollectedTargets) LastResult.foundObjectIds.Add(target.objectId);
        }

        private void SetPhase(RoundPhase phase)
        {
            CurrentPhase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
