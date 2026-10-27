using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrapTheCat.UI
{
    public sealed class ConfirmationPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _message;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _confirmLabel;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TMP_Text _cancelLabel;

        private Action _onConfirm;

        private void OnEnable()
        {
            _confirmButton.onClick.AddListener(Confirm);
            _cancelButton.onClick.AddListener(Hide);
        }

        private void OnDisable()
        {
            _confirmButton.onClick.RemoveListener(Confirm);
            _cancelButton.onClick.RemoveListener(Hide);
        }

        public void Show(string message, string confirmText, string cancelText, Action onConfirm)
        {
            _message.text = message;
            _confirmLabel.text = confirmText;
            _cancelLabel.text = cancelText;
            _onConfirm = onConfirm;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            _onConfirm = null;
            gameObject.SetActive(false);
        }

        private void Confirm()
        {
            Action onConfirm = _onConfirm;
            Hide();
            onConfirm?.Invoke();
        }
    }
}
