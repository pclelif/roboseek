using UnityEngine;
namespace Robot.UI.Production
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public UIStateManager state;
        public void Play() { if (state != null) state.EnterHubFromMainMenu(); }
        public void Settings() { if (state != null) state.Open(UIScreen.Settings); }
        public void Controls() { if (state != null) state.Open(UIScreen.Controls); }
        public void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
