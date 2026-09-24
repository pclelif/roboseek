using UnityEngine;
namespace Robot.UI.Production
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        public UIStateManager state;
        public ConfirmDialogController confirm;
        public void Resume() => state.Resume();
        public void Settings() => state.Open(UIScreen.Settings);
        public void Controls() => state.Open(UIScreen.Controls);
        public void Restart() => confirm.Show("RESTART ROUND?", "Your current progress will be lost.", "RESTART", state.RestartRound);
        public void QuitToLobby() => state.ReturnToHub();
    }
}
