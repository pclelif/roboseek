using UnityEngine;
using Robot.Player.CameraControl;
namespace Robot.UI.Production
{
    public sealed class CursorStateController : MonoBehaviour
    {
        public ThirdPersonCameraController gameplayCamera;
        private bool gameplay;
        public bool WantsLockedCursor => gameplay;
        public void SetGameplay(bool value)
        {
            gameplay = value;
            gameplayCamera.SetUIInputBlocked(!value);
            Apply();
        }
        private void Apply()
        {
            Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !gameplay;
        }
        private void OnApplicationFocus(bool focused) { if (focused) Apply(); }
        private void OnDisable()
        {
            if (gameplayCamera != null) gameplayCamera.ReleaseUICursorOwnership();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
