using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Robot.ObjectHunt;
using Robot.Score;

namespace Robot.Multiplayer
{
    public enum NetworkRoundState
    {
        WaitingForPlayers,
        Preparing,
        Playing,
        RoundComplete,
        Results
    }

    [DisallowMultipleComponent]
    public sealed class NetworkRoundManager : NetworkBehaviour
    {
        public static NetworkRoundManager Instance { get; private set; }

        [SerializeField] private float roundDuration = 600f;
        [SerializeField] private bool autoStartWhenTwoPlayers = true;

        public NetworkVariable<NetworkRoundState> CurrentState = new NetworkVariable<NetworkRoundState>(
            NetworkRoundState.WaitingForPlayers,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public NetworkVariable<float> TimeRemaining = new NetworkVariable<float>(
            600f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public NetworkVariable<int> SyncedRoundNumber = new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public NetworkVariable<int> SyncedCollectedCount = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly Dictionary<ulong, PlayerScoreData> playerScores = new Dictionary<ulong, PlayerScoreData>();
        private readonly HashSet<string> firstFoundTargets = new HashSet<string>();
        private readonly HashSet<string> collectedTargetIds = new HashSet<string>();
        private readonly Dictionary<(ulong, ulong), float> combatHitCooldowns = new Dictionary<(ulong, ulong), float>();

        private ObjectHuntRoundManager localHuntManager;

        public event Action<NetworkRoundState> StateChanged;
        public event Action<string, ulong, int> TargetCollectedOnNetwork;
        public event Action<List<PlayerScoreData>> ScoreboardUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            localHuntManager = GetComponent<ObjectHuntRoundManager>();
            if (localHuntManager == null) localHuntManager = FindFirstObjectByType<ObjectHuntRoundManager>();
        }

        public override void OnNetworkSpawn()
        {
            CurrentState.OnValueChanged += HandleStateChanged;
            if (IsServer)
            {
                TimeRemaining.Value = roundDuration;
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                    NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            CurrentState.OnValueChanged -= HandleStateChanged;
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            if (CurrentState.Value == NetworkRoundState.Playing)
            {
                TimeRemaining.Value = Mathf.Max(0f, TimeRemaining.Value - Time.deltaTime);
                if (TimeRemaining.Value <= 0f)
                {
                    FinishRound(false);
                }
            }
        }

        public void HostStartGame()
        {
            if (!IsServer) return;
            StartCoroutine(PrepareAndStartRoundRoutine());
        }

        private IEnumerator PrepareAndStartRoundRoutine()
        {
            CurrentState.Value = NetworkRoundState.Preparing;
            collectedTargetIds.Clear();
            firstFoundTargets.Clear();
            combatHitCooldowns.Clear();
            SyncedCollectedCount.Value = 0;
            TimeRemaining.Value = roundDuration;

            // Reset round score for all players while keeping TotalScore
            var clientKeys = playerScores.Keys.ToList();
            foreach (var key in clientKeys)
            {
                var score = playerScores[key];
                score.ResetRound();
                playerScores[key] = score;
            }
            BroadcastScoreboard();

            if (localHuntManager != null)
            {
                localHuntManager.PrepareRound();
            }

            yield return new WaitForSeconds(2f);
            CurrentState.Value = NetworkRoundState.Playing;
            if (localHuntManager != null) localHuntManager.StartSearch();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestPickupTargetServerRpc(string targetId, ulong clientNetworkId)
        {
            if (!IsServer || CurrentState.Value != NetworkRoundState.Playing) return;

            if (collectedTargetIds.Contains(targetId))
            {
                Debug.LogWarning($"[NetworkRoundManager] Target {targetId} already collected; ignoring duplicate request from Client {clientNetworkId}.");
                return;
            }

            collectedTargetIds.Add(targetId);
            SyncedCollectedCount.Value++;

            bool isFirstFinder = !firstFoundTargets.Contains(targetId);
            if (isFirstFinder) firstFoundTargets.Add(targetId);

            EnsurePlayerScoreExists(clientNetworkId);
            var score = playerScores[clientNetworkId];
            score.objectsFound++;
            if (isFirstFinder) score.firstFinderCount++;
            playerScores[clientNetworkId] = score;

            Debug.Log($"[NetworkRoundManager] Target '{targetId}' collected by Client {clientNetworkId}! (FirstFinder: {isFirstFinder}). RoundScore: {score.RoundScore} | Total: {score.TotalScore}");

            TargetCollectedClientRpc(targetId, clientNetworkId, score.RoundScore);
            BroadcastScoreboard();

            if (SyncedCollectedCount.Value >= 3)
            {
                FinishRound(true);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReportCombatHitServerRpc(ulong attackerClientId, ulong victimClientId)
        {
            if (!IsServer || CurrentState.Value != NetworkRoundState.Playing) return;
            if (attackerClientId == victimClientId) return;

            float now = Time.time;
            var key = (attackerClientId, victimClientId);
            if (combatHitCooldowns.TryGetValue(key, out float lastTime) && now - lastTime < 2.5f)
            {
                return; // Cooldown active, avoid spam
            }

            combatHitCooldowns[key] = now;
            EnsurePlayerScoreExists(attackerClientId);
            var score = playerScores[attackerClientId];
            score.combatHits++;
            playerScores[attackerClientId] = score;

            Debug.Log($"[NetworkRoundManager] Client {attackerClientId} hit Client {victimClientId}! Combat score: +10 (Total Combat: {score.CombatScore})");
            BroadcastScoreboard();
        }

        private void EnsurePlayerScoreExists(ulong clientId)
        {
            if (!playerScores.ContainsKey(clientId))
            {
                playerScores[clientId] = new PlayerScoreData
                {
                    clientId = clientId,
                    playerName = $"Robot {clientId + 1}",
                    objectsFound = 0,
                    firstFinderCount = 0,
                    combatHits = 0,
                    accumulatedScore = 0
                };
            }
        }

        private void BroadcastScoreboard()
        {
            if (!IsServer) return;
            var list = playerScores.Values.OrderByDescending(p => p.TotalScore).ToArray();
            SyncScoreboardClientRpc(list);
        }

        [ClientRpc]
        private void SyncScoreboardClientRpc(PlayerScoreData[] scores)
        {
            playerScores.Clear();
            foreach (var s in scores) playerScores[s.clientId] = s;
            ScoreboardUpdated?.Invoke(scores.ToList());
        }

        [ClientRpc]
        private void TargetCollectedClientRpc(string targetId, ulong clientNetworkId, int totalScore)
        {
            TargetCollectedOnNetwork?.Invoke(targetId, clientNetworkId, totalScore);

            var collectibles = FindObjectsByType<CollectibleTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var col in collectibles)
            {
                if (col != null && col.Definition != null && col.Definition.objectId == targetId)
                {
                    col.gameObject.SetActive(false);
                    break;
                }
            }
        }

        private void FinishRound(bool success)
        {
            if (!IsServer) return;
            CurrentState.Value = NetworkRoundState.RoundComplete;
            if (localHuntManager != null) localHuntManager.StopSearch();
            StartCoroutine(ShowResultsRoutine());
        }

        private IEnumerator ShowResultsRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            CurrentState.Value = NetworkRoundState.Results;
            BroadcastScoreboard();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestNextRoundServerRpc()
        {
            if (!IsServer) return;
            SyncedRoundNumber.Value++;
            StartCoroutine(PrepareAndStartRoundRoutine());
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestResetMatchServerRpc()
        {
            if (!IsServer) return;
            SyncedRoundNumber.Value = 1;
            var clientKeys = playerScores.Keys.ToList();
            foreach (var key in clientKeys)
            {
                var score = playerScores[key];
                score.ResetMatch();
                playerScores[key] = score;
            }
            StartCoroutine(PrepareAndStartRoundRoutine());
        }

        private void HandleClientConnected(ulong clientId)
        {
            EnsurePlayerScoreExists(clientId);
            BroadcastScoreboard();
            Debug.Log($"[NetworkRoundManager] Registered Client {clientId} to scoreboard.");
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            Debug.Log($"[NetworkRoundManager] Client {clientId} disconnected.");
            BroadcastScoreboard();
        }

        private void HandleStateChanged(NetworkRoundState oldState, NetworkRoundState newState)
        {
            Debug.Log($"[NetworkRoundManager] State changed: {oldState} -> {newState}");
            StateChanged?.Invoke(newState);
        }

        public PlayerScoreData GetPlayerScore(ulong clientId) => playerScores.TryGetValue(clientId, out var data) ? data : default;
        public List<PlayerScoreData> GetLeaderboard() => playerScores.Values.OrderByDescending(p => p.TotalScore).ToList();
    }
}
