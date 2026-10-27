using System;
using TMPro;
using TrapTheCat.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrapTheCat.UI
{
    public sealed class LevelButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _numberLabel;
        [SerializeField] private ButtonHoverFeedback _feedback;

        private LevelData _level;

        public event Action<LevelData> Clicked;

        private void OnEnable()
        {
            _button.onClick.AddListener(RaiseClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(RaiseClicked);
        }

        public void Show(LevelData level, int number, bool unlocked, CursorTooltip tooltip, string tooltipText)
        {
            _level = level;
            _numberLabel.text = number.ToString();
            _button.interactable = unlocked;
            _feedback.SetTooltip(tooltip, tooltipText);
        }

        private void RaiseClicked()
        {
            Clicked?.Invoke(_level);
        }
    }
}
