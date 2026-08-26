using System;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.Score
{
    [DisallowMultipleComponent]
    public sealed class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        private PlayerScoreData localScore = new PlayerScoreData
        {
            clientId = 0,
            playerName = "Local Player",
            objectsFound = 0,
            firstFinderCount = 0,
            combatHits = 0,
            accumulatedScore = 0
        };

        private readonly Dictionary<GameObject, float> combatCooldowns = new Dictionary<GameObject, float>();
        private const float CombatHitCooldown = 2.5f;

        public PlayerScoreData LocalScore => localScore;
        public event Action<PlayerScoreData> ScoreChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void AddFind(bool isFirstFinder)
        {
            localScore.objectsFound++;
            if (isFirstFinder) localScore.firstFinderCount++;
            ScoreChanged?.Invoke(localScore);
            Debug.Log($"[ScoreManager] Object found! Find: {localScore.FindScore} | FirstFinder: {localScore.FirstFinderScore} | Round: {localScore.RoundScore}");
        }

        public bool TryRecordCombatHit(GameObject victim)
        {
            if (victim == null) return false;

            float now = Time.time;
            if (combatCooldowns.TryGetValue(victim, out float lastTime) && now - lastTime < CombatHitCooldown)
            {
                // Cooldown active, hit dealt damage but does not award spam score
                return false;
            }

            combatCooldowns[victim] = now;
            localScore.combatHits++;
            ScoreChanged?.Invoke(localScore);
            Debug.Log($"[ScoreManager] Combat hit on {victim.name}! Combat Score: {localScore.CombatScore} (Hits: {localScore.combatHits})");
            return true;
        }

        public void NextRound()
        {
            localScore.ResetRound();
            combatCooldowns.Clear();
            ScoreChanged?.Invoke(localScore);
        }

        public void ResetMatch()
        {
            localScore.ResetMatch();
            combatCooldowns.Clear();
            ScoreChanged?.Invoke(localScore);
        }

        public string GetBreakdownString()
        {
            return $"Objects Found:       +{localScore.FindScore} Pts ({localScore.objectsFound} Items)\n" +
                   $"First Finder Bonus:  +{localScore.FirstFinderScore} Pts ({localScore.firstFinderCount} Bonus)\n" +
                   $"Combat Hits:         +{localScore.CombatScore} Pts ({localScore.combatHits} Hits)\n" +
                   $"--------------------------------------\n" +
                   $"ROUND SCORE:         +{localScore.RoundScore} Pts\n" +
                   $"TOTAL SCORE:         {localScore.TotalScore} Pts";
        }
    }
}
