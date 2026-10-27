using TMPro;
using TrapTheCat.Core;
using UnityEngine;

namespace TrapTheCat.UI
{
    public class TutorialPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _banner;
        [SerializeField] private TMP_Text _messageText;

        private LevelData _level;
        private int _playerMoves;

        public void Begin(LevelData level)
        {
            _level = level;
            _playerMoves = 0;
            UpdateMessage();
        }

        public void OnPlayerMovePlaced()
        {
            _playerMoves++;
            UpdateMessage();
        }

        public void Hide()
        {
            _level = null;
            _banner.SetActive(false);
        }

        private void UpdateMessage()
        {
            if (_level == null || _level.tutorialSteps == null)
            {
                _banner.SetActive(false);
                return;
            }

            TutorialStep activeStep = null;
            foreach (TutorialStep step in _level.tutorialSteps)
            {
                if (step.afterPlayerMoves <= _playerMoves &&
                    (activeStep == null || step.afterPlayerMoves >= activeStep.afterPlayerMoves))
                    activeStep = step;
            }

            if (activeStep == null || string.IsNullOrWhiteSpace(activeStep.message))
            {
                _banner.SetActive(false);
                return;
            }

            _messageText.text = activeStep.message;
            _banner.SetActive(true);
        }
    }
}
