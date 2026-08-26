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

        private void Awake() => hunt = GetComponent<ObjectHuntRoundManager>();

        private void OnEnable()
        {
            hunt.RoundCompleted += HandleRoundCompleted;
            hunt.RoundFailed += HandleRoundFailed;
        }

        private void Start()
        {
            SetPhase(RoundPhase.Lobby);
            if (autoStart) StartGame();
        }

        private void OnDisable()
        {
            hunt.RoundCompleted -= HandleRoundCompleted;
            hunt.RoundFailed -= HandleRoundFailed;
        }

        public void StartGame()
        {
            if (CurrentPhase != RoundPhase.Lobby && CurrentPhase != RoundPhase.Result) return;
            StartNextRound();
        }

        public void StartNextRound()
        {
            if (flowRoutine != null) StopCoroutine(flowRoutine);
            flowRoutine = StartCoroutine(RoundStartRoutine());
        }

        private IEnumerator RoundStartRoutine()
        {
            RoundNumber++;
            SetPhase(RoundPhase.Intro);
            yield return new WaitForSeconds(introDuration);

            if (hunt == null) hunt = GetComponent<ObjectHuntRoundManager>() ?? FindFirstObjectByType<ObjectHuntRoundManager>();

            if (hunt != null)
            {
                bool prepared = hunt.PrepareRound();
                if (!prepared)
                {
                    yield return new WaitForSeconds(0.5f);
                    hunt.PrepareRound();
                }
            }

            SetPhase(RoundPhase.Targets);
            yield return new WaitForSeconds(targetPreviewDuration);
            SetPhase(RoundPhase.Countdown);
            for (int value = countdownFrom; value >= 1; value--)
            {
                CountdownChanged?.Invoke(value);
                yield return new WaitForSeconds(1f);
            }
            CountdownChanged?.Invoke(0);
            yield return new WaitForSeconds(0.4f);
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
            yield return new WaitForSeconds(roundCompleteDuration);
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
