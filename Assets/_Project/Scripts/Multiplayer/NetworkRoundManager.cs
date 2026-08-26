using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Robot.ObjectHunt;

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
        [SerializeField] private int pointsPerTarget = 100;
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

        private readonly Dictionary<ulong, int> playerScores = new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, int> playerCollectedCounts = new Dictionary<ulong, int>();
        private readonly HashSet<string> collectedTargetIds = new HashSet<string>();

        private ObjectHuntRoundManager localHuntManager;

        public event Action<NetworkRoundState> StateChanged;
        public event Action<string, ulong, int> TargetCollectedOnNetwork;
        public event Action<Dictionary<ulong, int>> ScoresUpdated;

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

            if (CurrentState.Value == NetworkRoundState.WaitingForPlayers)
            {
                if (autoStartWhenTwoPlayers && NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList.Count >= 1)
                {
                    // Host can start or auto-prepare
                }
            }
            else if (CurrentState.Value == NetworkRoundState.Playing)
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
            SyncedCollectedCount.Value = 0;
            TimeRemaining.Value = roundDuration;

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

            if (!playerScores.ContainsKey(clientNetworkId)) playerScores[clientNetworkId] = 0;
            if (!playerCollectedCounts.ContainsKey(clientNetworkId)) playerCollectedCounts[clientNetworkId] = 0;

            playerScores[clientNetworkId] += pointsPerTarget;
            playerCollectedCounts[clientNetworkId]++;

            int newScore = playerScores[clientNetworkId];
            Debug.Log($"[NetworkRoundManager] Target '{targetId}' collected by Client {clientNetworkId}! New Score: {newScore} (Total Collected: {SyncedCollectedCount.Value}/3).");

            TargetCollectedClientRpc(targetId, clientNetworkId, newScore);

            if (SyncedCollectedCount.Value >= 3)
            {
                FinishRound(true);
            }
        }

        [ClientRpc]
        private void TargetCollectedClientRpc(string targetId, ulong clientNetworkId, int totalScore)
        {
            TargetCollectedOnNetwork?.Invoke(targetId, clientNetworkId, totalScore);

            // Find local collectible target with this id and trigger visual pickup
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
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestNextRoundServerRpc()
        {
            if (!IsServer) return;
            SyncedRoundNumber.Value++;
            StartCoroutine(PrepareAndStartRoundRoutine());
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (!playerScores.ContainsKey(clientId)) playerScores[clientId] = 0;
            if (!playerCollectedCounts.ContainsKey(clientId)) playerCollectedCounts[clientId] = 0;
            Debug.Log($"[NetworkRoundManager] Registered Client {clientId} to scoreboard.");
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            Debug.Log($"[NetworkRoundManager] Client {clientId} disconnected. Score retained.");
        }

        private void HandleStateChanged(NetworkRoundState oldState, NetworkRoundState newState)
        {
            Debug.Log($"[NetworkRoundManager] State changed: {oldState} -> {newState}");
            StateChanged?.Invoke(newState);
        }

        public int GetScore(ulong clientId) => playerScores.TryGetValue(clientId, out int score) ? score : 0;
        public int GetFoundCount(ulong clientId) => playerCollectedCounts.TryGetValue(clientId, out int count) ? count : 0;
        public IReadOnlyDictionary<ulong, int> GetAllScores() => playerScores;
    }
}
