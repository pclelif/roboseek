using Unity.Netcode;
using UnityEngine;
using Unity.Multiplayer.Playmode;

namespace Robot.Multiplayer
{
    public sealed class NetworkDebugLauncher : MonoBehaviour
    {
        [SerializeField] private bool showDebugControls = true;
        [SerializeField] private bool autoStartFromPlayerTags = true;

        private void Start()
        {
            if (!autoStartFromPlayerTags || NetworkManager.Singleton == null) return;
            string[] tags = CurrentPlayer.ReadOnlyTags();
            foreach (string tag in tags)
            {
                if (string.Equals(tag, "Host", System.StringComparison.OrdinalIgnoreCase)) { NetworkManager.Singleton.StartHost(); return; }
                if (string.Equals(tag, "Client", System.StringComparison.OrdinalIgnoreCase)) { NetworkManager.Singleton.StartClient(); return; }
            }
        }
        private void OnGUI()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (!showDebugControls || manager == null) return;

            if (!manager.IsListening)
            {
                GUILayout.BeginArea(new Rect(16, 16, 190, 150), GUI.skin.box);
                GUILayout.Label("<b>🌐 MULTIPLAYER (Demo2)</b>");
                GUILayout.Space(6);
                if (GUILayout.Button("Host Olarak Başlat (1)", GUILayout.Height(32))) manager.StartHost();
                if (GUILayout.Button("Client Olarak Katıl (2)", GUILayout.Height(32))) manager.StartClient();
                if (GUILayout.Button("Server Başlat", GUILayout.Height(24))) manager.StartServer();
                GUILayout.EndArea();
            }
            else
            {
                GUILayout.BeginArea(new Rect(16, 16, 210, 85), GUI.skin.box);
                string role = manager.IsHost ? "HOST (Server + Client)" : manager.IsServer ? "DEDICATED SERVER" : "CLIENT";
                GUILayout.Label($"<b>🌐 Durum:</b> {role}");
                GUILayout.Label($"<b>Bağlı Oyuncular:</b> {manager.ConnectedClientsList.Count} / 4");
                if (GUILayout.Button("Bağlantıyı Kes", GUILayout.Height(24))) manager.Shutdown();
                GUILayout.EndArea();
            }
        }
    }
}
