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
        }

        private void OnDestroy()
        {
            if (manager == null) return;
            manager.ConnectionApprovalCallback = null;
            manager.OnClientConnectedCallback -= HandleConnected;
            manager.OnClientDisconnectCallback -= HandleDisconnected;
        }

        private static void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Pending = false;
            if (SpawnPointManager.Instance != null && SpawnPointManager.Instance.TryReserve(request.ClientNetworkId, out Pose pose))
            {
                response.Position = pose.position;
                response.Rotation = pose.rotation;
            }
            else
            {
                response.Approved = false;
                response.Reason = "No safe spawn point is available";
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
