using System;
using System.Collections.Generic;
using TMPro;
using TrapTheCat.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrapTheCat.UI
{
    public sealed class LevelButton : MonoBehaviour
    {
        [Serializable]
        private struct DifficultyColor
        {
            public CatDifficulty difficulty;
            public Color color;
        }

        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _numberLabel;
        [SerializeField] private ButtonHoverFeedback _feedback;
        [SerializeField] private List<DifficultyColor> _difficultyColors = new List<DifficultyColor>();

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
            _feedback.SetBaseColor(ColorFor(level.catDifficulty));
            _feedback.SetTooltip(tooltip, tooltipText);
        }

        private Color ColorFor(CatDifficulty difficulty)
        {
            foreach (DifficultyColor entry in _difficultyColors)
            {
                if (entry.difficulty == difficulty)
                    return entry.color;
            }
            return Color.white;
        }

        private void RaiseClicked()
        {
            Clicked?.Invoke(_level);
        }
    }
}
