using System;
using System.Collections;
using TMPro;
using TrapTheCat.Core;
using TrapTheCat.Presentation;
using TrapTheCat.Services;
using UnityEngine;
using UnityEngine.UI;

namespace TrapTheCat.UI
{
    public sealed class LevelEditorScreen : UIScreen
    {
        private const float EditorSidebarGap = 0.02f;
        private const int MaxLevelNumber = 999;

        [SerializeField] private Button _backButton;
        [SerializeField] private Button _previousShapeButton;
        [SerializeField] private Button _nextShapeButton;
        [SerializeField] private Button _lockButton;
        [SerializeField] private TMP_Text _lockButtonLabel;
        [SerializeField] private Button _placeBlocksButton;
        [SerializeField] private Button _setupCatButton;
        [SerializeField] private Button _changeDifficultyButton;
        [SerializeField] private TMP_Text _difficultyText;
        [SerializeField] private Button _saveLevelButton;
        [SerializeField] private Button _validateButton;
        [SerializeField] private Button _generateButton;
        [SerializeField] private Button _clearButton;
        [SerializeField] private Button _undoButton;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private TMP_InputField _levelNumberInput;
        [SerializeField] private Slider _sizeSlider;
        [SerializeField] private TMP_Text _shapeLabel;
        [SerializeField] private TMP_Text _sizeLabel;
        [SerializeField] private TMP_Text _lockLabel;
        [Tooltip("The editor's side panel; the board is framed in the screen space left of it.")]
        [SerializeField] private RectTransform _editorSidebar;
        [SerializeField] private LoadingRing _loadingRing;

        private GridManager _gridManager;
        private BoardCameraFramer _cameraFramer;
        private LevelCatalog _levelCatalog;
        private ConfirmationPopup _popup;
        private LevelEditorSession _session;
        private bool _busy;
        private Coroutine _job;

        public event Action BackClicked;
        public event Action LevelSaved;
        public event Action<bool> CatVisibilityChanged;

        public bool CanEditTiles
        {
            get { return _session.IsLocked && !_busy; }
        }

        public void Initialize(
            GridManager gridManager,
            BoardCameraFramer cameraFramer,
            LevelCatalog levelCatalog,
            BoardShapeList boardShapes,
            ConfirmationPopup popup)
        {
            _gridManager = gridManager;
            _cameraFramer = cameraFramer;
            _levelCatalog = levelCatalog;
            _popup = popup;
            _session = new LevelEditorSession(boardShapes.Shapes, new System.Random());
            _session.BoardRebuilt += ShowBoard;
            _session.Changed += UpdateLabels;

            _levelNumberInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _levelNumberInput.characterLimit = 3;
            _levelNumberInput.onValidateInput += ValidateLevelNumberCharacter;
            _sizeSlider.minValue = LevelEditorSession.MinSize;
            _sizeSlider.maxValue = LevelEditorSession.MaxSize;
            _sizeSlider.wholeNumbers = true;
            _statusLabel.text = string.Empty;
            _loadingRing.Hide();
        }

        private void OnDestroy()
        {
            if (_session == null)
                return;

            _session.BoardRebuilt -= ShowBoard;
            _session.Changed -= UpdateLabels;
        }

        private void OnEnable()
        {
            _backButton.onClick.AddListener(GoBack);
            _previousShapeButton.onClick.AddListener(SelectPreviousShape);
            _nextShapeButton.onClick.AddListener(SelectNextShape);
            _lockButton.onClick.AddListener(ToggleBoardLock);
            _placeBlocksButton.onClick.AddListener(UseBlockTool);
            _setupCatButton.onClick.AddListener(UseCatTool);
            _changeDifficultyButton.onClick.AddListener(CycleDifficulty);
            _saveLevelButton.onClick.AddListener(SaveDraft);
            _validateButton.onClick.AddListener(ValidateDraft);
            _generateButton.onClick.AddListener(GenerateLevel);
            _clearButton.onClick.AddListener(ClearDraft);
            _undoButton.onClick.AddListener(UndoEdit);
            _sizeSlider.onValueChanged.AddListener(OnSizeChanged);
        }

        private void OnDisable()
        {
            _backButton.onClick.RemoveListener(GoBack);
            _previousShapeButton.onClick.RemoveListener(SelectPreviousShape);
            _nextShapeButton.onClick.RemoveListener(SelectNextShape);
            _lockButton.onClick.RemoveListener(ToggleBoardLock);
            _placeBlocksButton.onClick.RemoveListener(UseBlockTool);
            _setupCatButton.onClick.RemoveListener(UseCatTool);
            _changeDifficultyButton.onClick.RemoveListener(CycleDifficulty);
            _saveLevelButton.onClick.RemoveListener(SaveDraft);
            _validateButton.onClick.RemoveListener(ValidateDraft);
            _generateButton.onClick.RemoveListener(GenerateLevel);
            _clearButton.onClick.RemoveListener(ClearDraft);
            _undoButton.onClick.RemoveListener(UndoEdit);
            _sizeSlider.onValueChanged.RemoveListener(OnSizeChanged);
        }

        public override void Open()
        {
            base.Open();
            CancelJob();
            _levelNumberInput.text = string.Empty;
            _cameraFramer.SetReservedRightFraction(EditorSidebarFraction());
            _session.StartNewDraft();
            _sizeSlider.SetValueWithoutNotify(_session.Size);
        }

        public override void Close()
        {
            CancelJob();
            _cameraFramer.SetReservedRightFraction(0f);
            base.Close();
        }

        public void HandleEditorCellClick(Vector2Int cell)
        {
            if (CanEditTiles)
                _session.EditCell(cell);
        }

        private void GoBack()
        {
            CancelJob();
            BackClicked?.Invoke();
        }

        private void UndoEdit()
        {
            if (!_busy)
                _session.Undo();
        }

        private void OnSizeChanged(float value)
        {
            if (!_busy)
                _session.SetSize(Mathf.RoundToInt(value));
        }

        private void SelectPreviousShape()
        {
            if (!_busy)
                _session.ChangeShape(-1);
        }

        private void SelectNextShape()
        {
            if (!_busy)
                _session.ChangeShape(1);
        }

        private void ToggleBoardLock()
        {
            _session.ToggleLock();
            ClearStatus();
        }

        private void UseBlockTool()
        {
            _session.Tool = EditorTool.Blocks;
        }

        private void UseCatTool()
        {
            _session.Tool = EditorTool.Cat;
        }

        private void CycleDifficulty()
        {
            _session.CycleDifficulty();
            ClearStatus();
        }

        private float EditorSidebarFraction()
        {
            Vector3[] corners = new Vector3[4];
            _editorSidebar.GetWorldCorners(corners);
            float left = RectTransformUtility.WorldToScreenPoint(null, corners[0]).x;
            return Mathf.Clamp01(1f - left / Screen.width + EditorSidebarGap);
        }

        private void ShowBoard(bool animate)
        {
            _gridManager.CreateGrid(_session.Board, animate);
            CatVisibilityChanged?.Invoke(_session.Board.HasCat);
            ClearStatus();
        }

        private void UpdateLabels()
        {
            bool locked = _session.IsLocked;
            _shapeLabel.text = _session.Shape.DisplayName.ToUpperInvariant();
            _sizeLabel.text = $"SIZE {_session.Size}";
            _lockLabel.text = locked ? "BOARD LOCKED" : "BOARD UNLOCKED";
            _lockButtonLabel.text = locked ? "UNLOCK BOARD" : "LOCK BOARD";
            _difficultyText.text = $"DIFFICULTY: {_session.Difficulty.ToString().ToUpperInvariant()}";
            _previousShapeButton.interactable = !locked;
            _nextShapeButton.interactable = !locked;
            _sizeSlider.interactable = !locked;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            _saveLevelButton.interactable = _session.IsValidated && !_busy;
            _validateButton.interactable = _session.IsLocked && !_busy;
            _generateButton.interactable = !_busy;
            _clearButton.interactable = !_busy;
            _undoButton.interactable = _session.CanUndo && !_busy;
        }

        private void SetStatus(string message)
        {
            _statusLabel.text = message;
        }

        private void ClearStatus()
        {
            if (!_busy)
                SetStatus(string.Empty);
        }

        private void StartJob(IEnumerator routine)
        {
            if (_busy)
                return;

            _busy = true;
            UpdateButtons();
            _job = StartCoroutine(routine);
        }

        private void EndJob(string status)
        {
            _busy = false;
            _job = null;
            _loadingRing.Hide();
            SetStatus(status);
            UpdateButtons();
        }

        private void CancelJob()
        {
            if (_job != null)
                StopCoroutine(_job);
            _job = null;
            _busy = false;
            _loadingRing.Hide();
        }

        private void ClearDraft()
        {
            StartJob(ClearRoutine());
        }

        private IEnumerator ClearRoutine()
        {
            SetStatus("Clearing...");
            yield return _loadingRing.Sweep(0.3f);
            _session.Clear();
            EndJob("Level cleared.");
        }

        private void ValidateDraft()
        {
            StartJob(ValidateRoutine());
        }

        private IEnumerator ValidateRoutine()
        {
            SetStatus("Validating...");
            yield return _loadingRing.Sweep(0.4f);
            LevelValidationResult result = _session.Validate();
            EndJob((result.IsValid ? "VALID: " : "INVALID: ") + result.Message);
        }

        private void GenerateLevel()
        {
            StartJob(GenerateRoutine());
        }

        private IEnumerator GenerateRoutine()
        {
            SetStatus("Generating...");
            _loadingRing.SetProgress(0f);
            yield return null;

            LevelLayoutGenerator generator = _session.BeginGeneration();
            yield return null;
            if (generator == null)
            {
                EndJob("This board is too small to generate a level.");
                yield break;
            }

            while (generator.HasAttemptsLeft)
            {
                if (generator.TryNextAttempt())
                {
                    yield return _loadingRing.Finish();
                    _session.ApplyGeneratedLayout(generator);
                    EndJob("GENERATED. " + generator.Result.Message);
                    yield break;
                }

                _loadingRing.SetProgress(generator.AttemptsMade / (float)LevelLayoutGenerator.Attempts);
                if (generator.AttemptsMade % 3 == 0)
                {
                    SetStatus($"Generating... ({generator.AttemptsMade}/{LevelLayoutGenerator.Attempts})");
                    yield return null;
                }
            }

            yield return _loadingRing.Finish();
            _session.Clear();
            EndJob("Couldn't find a winnable layout. Try again or change the difficulty.");
        }

        private void SaveDraft()
        {
            if (_busy)
                return;

            if (!_session.IsValidated)
            {
                SetStatus("Validate the level first.");
                return;
            }

            if (!int.TryParse(_levelNumberInput.text, out int levelNumber) ||
                levelNumber < 1 || levelNumber > MaxLevelNumber)
            {
                SetStatus($"Enter a custom level number from 1 to {MaxLevelNumber}.");
                return;
            }

            LevelData level = _session.Draft;
            LevelCatalog.AssignCustomIdentity(level, levelNumber);
            LevelValidationResult validation = _session.Validate();
            if (!validation.IsValid)
            {
                SetStatus(validation.Message);
                return;
            }

            if (_levelCatalog.HasCustomLevelNumber(levelNumber))
            {
                _popup.Show($"LEVEL {levelNumber} WILL BE OVERWRITTEN.", "YES, OVERWRITE", "NO", () => SaveLevel(level));
                return;
            }

            SaveLevel(level);
        }

        private void SaveLevel(LevelData level)
        {
            if (!_levelCatalog.SaveCustomLevel(level, out string error))
            {
                SetStatus(error);
                return;
            }

            LevelSaved?.Invoke();
        }

        private static char ValidateLevelNumberCharacter(string text, int characterIndex, char addedCharacter)
        {
            return char.IsDigit(addedCharacter) ? addedCharacter : '\0';
        }
    }
}
