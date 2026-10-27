using System;
using System.Collections.Generic;
using TrapTheCat.Core;
using TrapTheCat.Services;
using UnityEngine;
using UnityEngine.UI;

namespace TrapTheCat.UI
{
    public sealed class LevelsScreen : UIScreen
    {
        private const float FirstRowOffset = 20f;
        private const float RowPitch = 106f;
        private const float MinimumListHeight = 780f;
        private static readonly Vector2 ButtonSize = new Vector2(650f, 84f);

        [SerializeField] private Button _backButton;
        [SerializeField] private Button _clearLevelsButton;
        [SerializeField] private RectTransform _levelList;
        [SerializeField] private LevelButton _levelButtonPrefab;

        private readonly List<LevelButton> _levelButtons = new List<LevelButton>();
        private LevelCatalog _levelCatalog;
        private ProgressionService _progression;
        private ConfirmationPopup _popup;
        private CursorTooltip _tooltip;

        public event Action BackClicked;
        public event Action<LevelData> LevelClicked;

        public void Initialize(
            LevelCatalog levelCatalog,
            ProgressionService progression,
            ConfirmationPopup popup,
            CursorTooltip tooltip)
        {
            _levelCatalog = levelCatalog;
            _progression = progression;
            _popup = popup;
            _tooltip = tooltip;
        }

        private void OnEnable()
        {
            _backButton.onClick.AddListener(RaiseBackClicked);
            _clearLevelsButton.onClick.AddListener(AskToClearCustomLevels);
        }

        private void OnDisable()
        {
            _backButton.onClick.RemoveListener(RaiseBackClicked);
            _clearLevelsButton.onClick.RemoveListener(AskToClearCustomLevels);
        }

        public override void Open()
        {
            base.Open();
            ShowLevels();
        }

        private void ShowLevels()
        {
            foreach (LevelButton button in _levelButtons)
            {
                button.Clicked -= RaiseLevelClicked;
                Destroy(button.gameObject);
            }
            _levelButtons.Clear();

            IReadOnlyList<LevelData> levels = _levelCatalog.Levels;
            _clearLevelsButton.interactable = _levelCatalog.HasCustomLevels;
            _levelList.sizeDelta = new Vector2(
                _levelList.sizeDelta.x,
                Mathf.Max(MinimumListHeight, levels.Count * RowPitch));

            for (int i = 0; i < levels.Count; i++)
            {
                LevelButton button = Instantiate(_levelButtonPrefab, _levelList, false);
                button.name = $"LevelButton_{levels[i].levelId}";
                PlaceInList((RectTransform)button.transform, i);
                ShowLevel(button, levels[i]);
                button.Clicked += RaiseLevelClicked;
                _levelButtons.Add(button);
            }
        }

        private void ShowLevel(LevelButton button, LevelData level)
        {
            int number = level.IsCustom ? level.customLevelNumber : level.progressionOrder;
            bool unlocked = _progression.IsUnlocked(level);
            string tooltip = level.IsCustom || string.IsNullOrWhiteSpace(level.displayName)
                ? $"LEVEL {number}"
                : $"LEVEL {number}  -  {level.displayName}";
            if (!unlocked)
                tooltip += "  -  LOCKED";
            button.Show(level, number, unlocked, _tooltip, tooltip.ToUpperInvariant());
        }

        private static void PlaceInList(RectTransform row, int index)
        {
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -FirstRowOffset - index * RowPitch);
            row.sizeDelta = ButtonSize;
        }

        private void AskToClearCustomLevels()
        {
            _popup.Show("DELETE ALL CUSTOM LEVELS?", "YES, DELETE", "CANCEL", ClearCustomLevels);
        }

        private void ClearCustomLevels()
        {
            if (!_levelCatalog.ClearCustomLevels(out string error))
            {
                _popup.Show(error, "TRY AGAIN", "CANCEL", ClearCustomLevels);
                return;
            }

            ShowLevels();
        }

        private void RaiseBackClicked()
        {
            BackClicked?.Invoke();
        }

        private void RaiseLevelClicked(LevelData level)
        {
            LevelClicked?.Invoke(level);
        }
    }
}
