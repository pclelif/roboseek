using System;
using UnityEngine;
using UnityEngine.UI;
namespace Robot.UI.Production
{
    public sealed class ConfirmDialogController : MonoBehaviour
    {
        public UIStateManager state;
        public Text title, message, confirmLabel;
        private Action confirmed;
        public void Show(string heading, string body, string action, Action callback)
        {
            title.text = UILocalization.Translate(heading); message.text = UILocalization.Translate(body); confirmLabel.text = UILocalization.Translate(action); confirmed = callback;
            state.Open(UIScreen.Confirm);
        }
        public void Confirm() { var action = confirmed; confirmed = null; action?.Invoke(); }
        public void Cancel() { confirmed = null; state.Back(); }
    }
}
