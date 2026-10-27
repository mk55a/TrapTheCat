using System;
using UnityEngine;
using UnityEngine.UI;

namespace TrapTheCat.UI
{
    public sealed class HomeScreen : UIScreen
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _levelEditorButton;

        public event Action PlayClicked;
        public event Action LevelEditorClicked;

        private void OnEnable()
        {
            _playButton.onClick.AddListener(RaisePlayClicked);
            _levelEditorButton.onClick.AddListener(RaiseLevelEditorClicked);
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveListener(RaisePlayClicked);
            _levelEditorButton.onClick.RemoveListener(RaiseLevelEditorClicked);
        }

        private void RaisePlayClicked()
        {
            PlayClicked?.Invoke();
        }

        private void RaiseLevelEditorClicked()
        {
            LevelEditorClicked?.Invoke();
        }
    }
}
