using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrapTheCat.UI
{
    public sealed class GameScreen : UIScreen
    {
        [SerializeField] private Button _backButton;
        [Header("Level end")]
        [SerializeField] private GameObject _levelEndPanel;
        [SerializeField] private TMP_Text _levelEndText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _levelsButton;

        public event Action RetryClicked;
        public event Action LevelListClicked;

        private void OnEnable()
        {
            _backButton.onClick.AddListener(RaiseLevelListClicked);
            _continueButton.onClick.AddListener(RaiseLevelListClicked);
            _levelsButton.onClick.AddListener(RaiseLevelListClicked);
            _retryButton.onClick.AddListener(RaiseRetryClicked);
        }

        private void OnDisable()
        {
            _backButton.onClick.RemoveListener(RaiseLevelListClicked);
            _continueButton.onClick.RemoveListener(RaiseLevelListClicked);
            _levelsButton.onClick.RemoveListener(RaiseLevelListClicked);
            _retryButton.onClick.RemoveListener(RaiseRetryClicked);
        }

        public void ShowLevelEnd(string message, bool won)
        {
            _levelEndText.text = message;
            _retryButton.gameObject.SetActive(!won);
            _continueButton.gameObject.SetActive(won);
            _levelsButton.gameObject.SetActive(!won);
            _levelEndPanel.SetActive(true);
        }

        public void HideLevelEnd()
        {
            _levelEndPanel.SetActive(false);
        }

        private void RaiseRetryClicked()
        {
            RetryClicked?.Invoke();
        }

        private void RaiseLevelListClicked()
        {
            LevelListClicked?.Invoke();
        }
    }
}
