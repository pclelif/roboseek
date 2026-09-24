using UnityEngine;
using UnityEngine.UI;
namespace Robot.UI.Production
{
    [RequireComponent(typeof(Button))]
    public sealed class MapPauseButton : MonoBehaviour
    {
        public UIStateManager state;
        private void Awake() => GetComponent<Button>().onClick.AddListener(() => state.Open(UIScreen.Pause));
    }
}
