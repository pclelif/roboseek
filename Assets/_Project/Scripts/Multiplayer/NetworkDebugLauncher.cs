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
            if (!showDebugControls || manager == null || manager.IsListening) return;
            GUILayout.BeginArea(new Rect(12, 12, 180, 145), GUI.skin.box);
            GUILayout.Label("FAZ 8 Network Test");
            if (GUILayout.Button("Start Host")) manager.StartHost();
            if (GUILayout.Button("Start Client")) manager.StartClient();
            if (GUILayout.Button("Start Server")) manager.StartServer();
            GUILayout.EndArea();
        }
    }
}
