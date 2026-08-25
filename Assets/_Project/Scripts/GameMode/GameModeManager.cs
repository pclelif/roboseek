using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Robot.GameMode
{
    public enum GameModeType
    {
        Singleplayer,
        Multiplayer
    }

    public static class GameModeManager
    {
        public const string SingleplayerSceneName = "Demo";
        public const string MultiplayerSceneName = "Demo2";
        public const string SingleplayerScenePath = "Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity";
        public const string MultiplayerScenePath = "Assets/_Project/Scenes/Demo2.unity";
        public const string AltMultiplayerScenePath = "Assets/_Project/Scenes/Multiplayer/DEMO-2.unity";

        public static GameModeType CurrentMode { get; private set; } = GameModeType.Singleplayer;
        public static event Action<GameModeType> ModeChanged;

        public static void LoadSingleplayer()
        {
            CurrentMode = GameModeType.Singleplayer;
            ModeChanged?.Invoke(CurrentMode);
            Debug.Log("[GameModeManager] Loading Singleplayer mode (Demo.unity)...");
            SceneManager.LoadScene(SingleplayerSceneName);
        }

        public static void LoadMultiplayer()
        {
            CurrentMode = GameModeType.Multiplayer;
            ModeChanged?.Invoke(CurrentMode);
            Debug.Log("[GameModeManager] Loading Multiplayer mode (Demo2.unity)...");
            if (Application.CanStreamedLevelBeLoaded(MultiplayerSceneName))
            {
                SceneManager.LoadScene(MultiplayerSceneName);
            }
            else if (Application.CanStreamedLevelBeLoaded("DEMO-2"))
            {
                SceneManager.LoadScene("DEMO-2");
            }
            else
            {
                SceneManager.LoadScene(MultiplayerSceneName);
            }
        }
    }
}
