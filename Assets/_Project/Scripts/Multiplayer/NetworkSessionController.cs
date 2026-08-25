using Unity.Netcode;
using UnityEngine;
using Robot.Robots.Customization;
using Robot.Spawning;

namespace Robot.Multiplayer
{
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetworkSessionController : MonoBehaviour
    {
        private NetworkManager manager;
        public event System.Action<ulong> PlayerConnected;
        public event System.Action<ulong> PlayerDisconnected;

        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
            manager.NetworkConfig.ConnectionApproval = true;
            manager.ConnectionApprovalCallback = ApproveConnection;
            manager.OnClientConnectedCallback += HandleConnected;
            manager.OnClientDisconnectCallback += HandleDisconnected;
            manager.OnServerStarted += CleanupStaticSinglePlayers;
            manager.OnClientStarted += CleanupStaticSinglePlayers;
        }

        private void Start()
        {
            CleanupStaticSinglePlayers();
        }

        private void OnDestroy()
        {
            if (manager == null) return;
            manager.ConnectionApprovalCallback = null;
            manager.OnClientConnectedCallback -= HandleConnected;
            manager.OnClientDisconnectCallback -= HandleDisconnected;
            manager.OnServerStarted -= CleanupStaticSinglePlayers;
            manager.OnClientStarted -= CleanupStaticSinglePlayers;
        }

        private void CleanupStaticSinglePlayers()
        {
            var movements = FindObjectsByType<Robot.Player.Movement.RobotMovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var m in movements)
            {
                if (m != null && m.GetComponent<NetworkObject>() == null)
                {
                    Debug.Log($"[NetworkSessionController] Removed static single-player object '{m.gameObject.name}' to prevent duplicate spawn in multiplayer.");
                    Destroy(m.gameObject);
                }
            }
        }

        private static void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Pending = false;

            if (SpawnPointManager.Instance != null)
            {
                if (SpawnPointManager.Instance.TryReserve(request.ClientNetworkId, out Pose pose, out int slotIndex))
                {
                    response.Approved = true;
                    response.CreatePlayerObject = true;
                    response.Position = pose.position;
                    response.Rotation = pose.rotation;
                    Debug.Log($"[NetworkSessionController] Approved connection for Client {request.ClientNetworkId} at SpawnPoint index {slotIndex} ({pose.position}).");
                }
                else
                {
                    response.Approved = false;
                    response.CreatePlayerObject = false;
                    response.Reason = "Sunucu dolu: Tüm spawn noktaları kullanımda (Maksimum 4 oyuncu).";
                    Debug.LogWarning($"[NetworkSessionController] Rejected connection for Client {request.ClientNetworkId}: No available spawn points.");
                }
            }
            else
            {
                Vector3 fallback = new Vector3((request.ClientNetworkId % 4) * 3f - 4.5f, 0.1f, 0f);
                if (Physics.Raycast(fallback + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f))
                {
                    fallback = hit.point + Vector3.up * 0.05f;
                }
                response.Approved = true;
                response.CreatePlayerObject = true;
                response.Position = fallback;
                response.Rotation = Quaternion.identity;
                Debug.Log($"[NetworkSessionController] Approved connection for Client {request.ClientNetworkId} using fallback grounded position {fallback}.");
            }
        }

        private void HandleConnected(ulong clientId) => PlayerConnected?.Invoke(clientId);
        private void HandleDisconnected(ulong clientId)
        {
            if (SpawnPointManager.Instance != null) SpawnPointManager.Instance.Release(clientId);
            if (RobotColorService.Instance != null) RobotColorService.Instance.Release(clientId);
            PlayerDisconnected?.Invoke(clientId);
        }
    }
}
