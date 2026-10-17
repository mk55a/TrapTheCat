using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameFlowUI : MonoBehaviour
{
    private enum EditorTool
    {
        Blocks,
        Cat
    }

    public static GameFlowUI Current { get; private set; }

    [Header("Authored screens")]
    [SerializeField] private GameObject _homePanel;
    [SerializeField] private GameObject _levelsPanel;
    [SerializeField] private GameObject _editorPanel;
    [SerializeField] private GameObject _gameplayPanel;
    [SerializeField] private GameObject _gameplayButtonGroup;

    [Header("Home and levels controls")]
    [SerializeField] private Button _openLevelsButton;
    [SerializeField] private Button _openEditorButton;
    [SerializeField] private Button _levelsBackButton;
    [SerializeField] private Button _clearLevelsButton;
    [SerializeField] private Transform _levelList;
    [SerializeField] private GameObject _clearLevelsConfirmationPanel;
    [SerializeField] private Button _cancelClearLevelsButton;
    [SerializeField] private Button _confirmClearLevelsButton;
    [SerializeField] private TMP_Text _clearLevelsMessage;

    [Header("Level editor controls")]
    [SerializeField] private Button _editorBackButton;
    [SerializeField] private Button _previousShapeButton;
    [SerializeField] private Button _nextShapeButton;
    [SerializeField] private Button _lockButton;
    [SerializeField] private Button _placeBlocksButton;
    [SerializeField] private Button _setupCatButton;
    [SerializeField] private Button _changeDifficultyButton;
    [SerializeField] private Button _saveLevelButton;
    [SerializeField] private TMP_InputField _levelNumberInput;
    [SerializeField] private Slider _sizeSlider;
    [SerializeField] private TMP_Text _shapeLabel;
    [SerializeField] private TMP_Text _sizeLabel;
    [SerializeField] private TMP_Text _lockLabel;
    [SerializeField] private TMP_Text _difficultyLabel;
    [SerializeField] private GameObject _unlockConfirmationPanel;
    [SerializeField] private Button _cancelUnlockButton;
    [SerializeField] private Button _confirmUnlockButton;
    [SerializeField] private GameObject _overwriteConfirmationPanel;
    [SerializeField] private Button _cancelOverwriteButton;
    [SerializeField] private Button _confirmOverwriteButton;
    [SerializeField] private TMP_Text _overwriteMessage;

    private GameManager _gameManager;
    private GridManager _gridManager;
    private CursorTooltip _cursorTooltip;
    private GameObject _levelButtonPrefab;
    private readonly List<GameObject> _spawnedLevelButtons = new List<GameObject>();
    private LevelData _draft;
    private LevelData _pendingLevelToSave;
    private HashSet<Vector2Int> _draftBlocks;
    private BoardShape _selectedShape;
    private int _selectedSize;
    private CatDifficulty _selectedDifficulty;
    private EditorTool _editorTool;
    private bool _boardLocked;
    private bool _isEditing;

    public bool IsEditingLevel
    {
        get { return _isEditing; }
    }

    public bool CanEditTiles
    {
        get { return _boardLocked; }
    }

    private void Awake()
    {
        Current = this;
    }

    public bool Initialize(GameManager gameManager, GridManager gridManager)
    {
        _gameManager = gameManager;
        _gridManager = gridManager;
        if (!ValidateScreenReferences())
            return false;

        _cursorTooltip = CursorTooltip.Create();
        _levelButtonPrefab = Resources.Load<GameObject>("UI/LevelButton");
        if (_levelButtonPrefab == null)
            Debug.LogError("Could not load the level button prefab from Resources/UI/LevelButton.", this);

        BindSceneControls();
        ShowHome();
        return true;
    }

    public void ShowHome()
    {
        _isEditing = false;
        _gameManager.SetEditorMode(false);
        _homePanel.SetActive(true);
        _levelsPanel.SetActive(false);
        _editorPanel.SetActive(false);
        _gameplayPanel.SetActive(false);
        if (_gameplayButtonGroup != null)
            _gameplayButtonGroup.SetActive(false);
    }

    public void ShowLevels()
    {
        _isEditing = false;
        _gameManager.SetEditorMode(false);
        _homePanel.SetActive(false);
        _levelsPanel.SetActive(true);
        _editorPanel.SetActive(false);
        _gameplayPanel.SetActive(false);
        if (_gameplayButtonGroup != null)
            _gameplayButtonGroup.SetActive(false);
        PopulateLevels();
    }

    public void ShowGameplay()
    {
        _isEditing = false;
        _homePanel.SetActive(false);
        _levelsPanel.SetActive(false);
        _editorPanel.SetActive(false);
        _gameplayPanel.SetActive(true);
        if (_gameplayButtonGroup != null)
            _gameplayButtonGroup.SetActive(true);
    }

    public void BeginEditor()
    {
        _isEditing = true;
        _boardLocked = false;
        _selectedShape = BoardShape.Rectangle;
        _selectedSize = 10;
        _selectedDifficulty = CatDifficulty.Normal;
        _editorTool = EditorTool.Blocks;
        _draftBlocks = new HashSet<Vector2Int>();
        _draft = new LevelData
        {
            levelId = "custom-level-1",
            displayName = "Level 1",
            customLevelNumber = 1,
            boardShape = _selectedShape,
            width = _selectedSize,
            height = Mathf.Max(5, _selectedSize - 2),
            hasCatStart = false,
            catDifficulty = _selectedDifficulty,
            blockedCells = new List<Vector2Int>(),
            tutorialSteps = new List<TutorialStep>()
        };
        _levelNumberInput.text = string.Empty;
        _sizeSlider.SetValueWithoutNotify(_selectedSize);
        RefreshEditorBoard();
        _homePanel.SetActive(false);
        _levelsPanel.SetActive(false);
        _editorPanel.SetActive(true);
        _gameplayPanel.SetActive(false);
        if (_gameplayButtonGroup != null)
            _gameplayButtonGroup.SetActive(false);
        _gameManager.SetEditorMode(true);
        UpdateEditorLabels();
    }

    public void HandleEditorCellClick(int x, int y)
    {
        if (!_isEditing || !_boardLocked || !_gridManager.IsActive(x, y))
            return;

        _gridManager.GetTileAt(x, y)?.PlayClickFeedback();
        Vector2Int coordinate = new Vector2Int(x, y);
        if (_editorTool == EditorTool.Cat)
        {
            if (_gridManager.IsPerimeterCell(x, y) || _draftBlocks.Contains(coordinate))
                return;

            _draft.hasCatStart = true;
            _draft.catStart = coordinate;
        }
        else
        {
            if (_draft.hasCatStart && _draft.catStart == coordinate)
                return;

            if (!_draftBlocks.Add(coordinate))
                _draftBlocks.Remove(coordinate);
        }

        _draft.blockedCells = new List<Vector2Int>(_draftBlocks);
        RefreshEditorBoard();
    }

    private bool ValidateScreenReferences()
    {
        if (_homePanel == null || _levelsPanel == null ||
            _editorPanel == null || _gameplayPanel == null)
        {
            Debug.LogError("Assign the authored Home, Levels, Level Editor, and Gameplay screens.", this);
            return false;
        }

        if (_homePanel.transform.root != transform.root ||
            _levelsPanel.transform.root != transform.root ||
            _editorPanel.transform.root != transform.root ||
            _gameplayPanel.transform.root != transform.root)
        {
            Debug.LogError("All GameFlowUI screens must belong to the same scene Canvas.", this);
            return false;
        }

        return true;
    }

    private void BindSceneControls()
    {
        BindButton(_openLevelsButton, ShowLevels, nameof(_openLevelsButton));
        BindButton(_openEditorButton, BeginEditor, nameof(_openEditorButton));
        BindButton(_levelsBackButton, ShowHome, nameof(_levelsBackButton));
        BindButton(_clearLevelsButton, ShowClearLevelsConfirmation, nameof(_clearLevelsButton));

        BindButton(_editorBackButton, ReturnToHomeFromEditor, nameof(_editorBackButton));
        BindButton(_previousShapeButton, () => ChangeShape(-1), nameof(_previousShapeButton));
        BindButton(_nextShapeButton, () => ChangeShape(1), nameof(_nextShapeButton));
        BindButton(_lockButton, ToggleBoardLock, nameof(_lockButton));
        BindButton(_placeBlocksButton, () => SetEditorTool(EditorTool.Blocks), nameof(_placeBlocksButton));
        BindButton(_setupCatButton, () => SetEditorTool(EditorTool.Cat), nameof(_setupCatButton));
        BindButton(_changeDifficultyButton, CycleDifficulty, nameof(_changeDifficultyButton));
        BindButton(_saveLevelButton, SaveDraft, nameof(_saveLevelButton));

        BindButton(_cancelUnlockButton, CancelUnlock, nameof(_cancelUnlockButton));
        BindButton(_confirmUnlockButton, ConfirmUnlock, nameof(_confirmUnlockButton));
        BindButton(_cancelOverwriteButton, CancelOverwrite, nameof(_cancelOverwriteButton));
        BindButton(_confirmOverwriteButton, ConfirmOverwrite, nameof(_confirmOverwriteButton));
        BindButton(_cancelClearLevelsButton, CancelClearLevels, nameof(_cancelClearLevelsButton));
        BindButton(_confirmClearLevelsButton, ConfirmClearLevels, nameof(_confirmClearLevelsButton));

        Require(_levelList != null, "Assign the level list content Transform.");
        Require(_levelNumberInput != null, "Level Editor screen requires a TMP_InputField.");
        Require(_sizeSlider != null, "Level Editor screen requires a board-size Slider.");
        Require(_shapeLabel != null && _sizeLabel != null,
            "Level Editor screen is missing a shape or size label.");
        Require(_previousShapeButton != null && _nextShapeButton != null && _lockButton != null,
            "Level Editor screen is missing its shape or board-lock buttons.");

        if (_levelNumberInput != null)
        {
            _levelNumberInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _levelNumberInput.characterLimit = 3;
            _levelNumberInput.onValidateInput += ValidateLevelNumberCharacter;
        }

        if (_sizeSlider != null)
        {
            _sizeSlider.minValue = 6;
            _sizeSlider.maxValue = 16;
            _sizeSlider.wholeNumbers = true;
            _sizeSlider.onValueChanged.AddListener(OnSizeChanged);
        }

        HideConfirmationPanels();
    }

    private void BindButton(Button button, UnityAction action, string referenceName)
    {
        if (button == null)
        {
            Debug.LogError($"Assign the {referenceName} Button reference.", this);
            return;
        }

        button.onClick.AddListener(action);
    }

    private void Require(bool condition, string message)
    {
        if (!condition)
            Debug.LogError(message, this);
    }

    private void HideConfirmationPanels()
    {
        if (_unlockConfirmationPanel != null)
            _unlockConfirmationPanel.SetActive(false);
        if (_overwriteConfirmationPanel != null)
            _overwriteConfirmationPanel.SetActive(false);
        if (_clearLevelsConfirmationPanel != null)
            _clearLevelsConfirmationPanel.SetActive(false);
    }

    private void OnSizeChanged(float value)
    {
        if (_boardLocked)
            return;

        _selectedSize = Mathf.RoundToInt(value);
        RefreshEditorBoard();
        UpdateEditorLabels();
    }

    private void ReturnToHomeFromEditor()
    {
        _isEditing = false;
        _gameManager.ReturnToHome();
    }

    private void PopulateLevels()
    {
        if (_levelList == null)
            return;

        foreach (GameObject row in _spawnedLevelButtons)
        {
            if (row != null)
                Destroy(row);
        }
        _spawnedLevelButtons.Clear();

        GameSaveData saveData = _gameManager.SaveData;
        HashSet<string> unlocked = new HashSet<string>(
            saveData != null && saveData.unlockedLevelIds != null
                ? saveData.unlockedLevelIds
                : new List<string>());
        List<LevelData> levels = LevelRepository.LoadAll();
        if (_clearLevelsButton != null)
            _clearLevelsButton.interactable = levels.Exists(LevelRepository.IsCustomLevel);

        if (_levelButtonPrefab == null)
        {
            Debug.LogError("Level buttons require the prefab at Resources/UI/LevelButton.", this);
            return;
        }

        RectTransform content = _levelList.GetComponent<RectTransform>();
        if (content != null)
            content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(780f, levels.Count * 106f));

        for (int i = 0; i < levels.Count; i++)
        {
            LevelData level = levels[i];
            bool isCustom = LevelRepository.IsCustomLevel(level);
            bool isUnlocked = isCustom || unlocked.Contains(level.levelId);
            string levelName = isCustom
                ? $"LEVEL {level.customLevelNumber}"
                : $"LEVEL {level.progressionOrder}  -  {level.displayName}";
            string label = isUnlocked ? levelName : levelName + "  -  LOCKED";
            GameObject rowObject = Instantiate(_levelButtonPrefab, _levelList, false);
            _spawnedLevelButtons.Add(rowObject);
            rowObject.name = $"LevelButton_{level.levelId}";
            RectTransform row = rowObject.GetComponent<RectTransform>();
            if (row != null)
            {
                row.anchorMin = new Vector2(0.5f, 1f);
                row.anchorMax = new Vector2(0.5f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.anchoredPosition = new Vector2(0f, -20f - i * 106f);
                row.sizeDelta = new Vector2(650f, 84f);
            }

            Button levelButton = rowObject.GetComponent<Button>();
            TMP_Text labelText = rowObject.GetComponentInChildren<TMP_Text>(true);
            if (levelButton == null || labelText == null)
            {
                Debug.LogError("LevelButton prefab requires a Button and a child TMP text component.",
                    rowObject);
                Destroy(rowObject);
                _spawnedLevelButtons.Remove(rowObject);
                continue;
            }

            labelText.text = label;
            levelButton.interactable = isUnlocked;
            string levelId = level.levelId;
            levelButton.onClick.AddListener(() => _gameManager.StartLevelById(levelId));
            MenuButtonFeedback feedback = rowObject.GetComponent<MenuButtonFeedback>();
            if (feedback == null)
                feedback = rowObject.AddComponent<MenuButtonFeedback>();
            feedback.Initialize(_cursorTooltip, label.ToUpperInvariant());
        }
    }

    private void ShowClearLevelsConfirmation()
    {
        if (_clearLevelsConfirmationPanel != null)
            _clearLevelsConfirmationPanel.SetActive(true);
    }

    private void CancelClearLevels()
    {
        if (_clearLevelsConfirmationPanel != null)
            _clearLevelsConfirmationPanel.SetActive(false);
    }

    private void ConfirmClearLevels()
    {
        if (!LevelRepository.ClearCustomLevels(out string error))
        {
            if (_clearLevelsMessage != null)
                _clearLevelsMessage.text = error;
            return;
        }

        CancelClearLevels();
        PopulateLevels();
    }

    private void ChangeShape(int direction)
    {
        if (_boardLocked)
            return;

        int count = Enum.GetValues(typeof(BoardShape)).Length;
        int current = (int)_selectedShape;
        _selectedShape = (BoardShape)((current + direction + count) % count);
        RefreshEditorBoard();
        UpdateEditorLabels();
    }

    private void ToggleBoardLock()
    {
        if (_boardLocked)
        {
            if (_unlockConfirmationPanel != null)
                _unlockConfirmationPanel.SetActive(true);
            return;
        }

        _boardLocked = true;
        UpdateEditorLabels();
    }

    private void CancelUnlock()
    {
        if (_unlockConfirmationPanel != null)
            _unlockConfirmationPanel.SetActive(false);
    }

    private void ConfirmUnlock()
    {
        CancelUnlock();
        _boardLocked = false;
        _draftBlocks.Clear();
        _draft.blockedCells.Clear();
        _draft.hasCatStart = false;
        RefreshEditorBoard();
        UpdateEditorLabels();
    }

    private void SetEditorTool(EditorTool tool)
    {
        _editorTool = tool;
    }

    private void CycleDifficulty()
    {
        int count = Enum.GetValues(typeof(CatDifficulty)).Length;
        _selectedDifficulty = (CatDifficulty)(((int)_selectedDifficulty + 1) % count);
        _draft.catDifficulty = _selectedDifficulty;
        UpdateEditorLabels();
    }

    private void RefreshEditorBoard()
    {
        if (_gridManager == null)
            return;

        int width = _selectedSize;
        int height = _selectedShape == BoardShape.Rectangle
            ? Mathf.Max(5, _selectedSize - 2)
            : _selectedSize;
        if (_selectedShape == BoardShape.Square)
            height = width;

        _draft.boardShape = _selectedShape;
        _draft.width = width;
        _draft.height = height;
        _draft.catDifficulty = _selectedDifficulty;
        _draft.blockedCells = new List<Vector2Int>(_draftBlocks);
        _gridManager.InitiateGrid(width, height, _selectedShape);
        foreach (Vector2Int blocked in _draftBlocks)
            _gridManager.SetCellType(blocked.x, blocked.y, GridType.Obstacle);

        if (_draft.hasCatStart && _gridManager.IsActive(_draft.catStart.x, _draft.catStart.y))
        {
            _gridManager.SetCatPosition(_draft.catStart.x, _draft.catStart.y);
            _gameManager.SetEditorCatVisible(true);
        }
        else
        {
            _draft.hasCatStart = false;
            _gameManager.SetEditorCatVisible(false);
        }
    }

    private void UpdateEditorLabels()
    {
        if (_shapeLabel == null || _sizeLabel == null || _lockButton == null)
            return;

        _shapeLabel.text = _selectedShape.ToString().ToUpperInvariant();
        _sizeLabel.text = $"SIZE {_selectedSize}";
        if (_lockLabel != null)
            _lockLabel.text = _boardLocked ? "BOARD LOCKED" : "BOARD UNLOCKED";
        if (_difficultyLabel != null)
            _difficultyLabel.text = $"CAT: {_selectedDifficulty.ToString().ToUpperInvariant()}";
        if (_previousShapeButton != null)
            _previousShapeButton.interactable = !_boardLocked;
        if (_nextShapeButton != null)
            _nextShapeButton.interactable = !_boardLocked;
        if (_sizeSlider != null)
            _sizeSlider.interactable = !_boardLocked;
        TMP_Text lockButtonLabel = _lockButton.GetComponentInChildren<TMP_Text>(true);
        if (lockButtonLabel != null)
            lockButtonLabel.text = _boardLocked ? "UNLOCK BOARD" : "LOCK BOARD";
    }

    private void SaveDraft()
    {
        if (!int.TryParse(_levelNumberInput.text, out int levelNumber) ||
            levelNumber < 1 || levelNumber > 999)
        {
            Debug.LogWarning("Enter a custom level number from 1 to 999.", this);
            return;
        }

        _draft.customLevelNumber = levelNumber;
        _draft.levelId = $"custom-level-{levelNumber}";
        _draft.displayName = $"Level {levelNumber}";
        _draft.blockedCells = new List<Vector2Int>(_draftBlocks);
        LevelValidationResult validation = LevelValidator.Validate(_draft);
        if (!validation.IsValid)
        {
            Debug.LogWarning(validation.Message, this);
            return;
        }

        if (LevelRepository.HasCustomLevelNumber(levelNumber))
        {
            _pendingLevelToSave = _draft;
            if (_overwriteMessage != null)
                _overwriteMessage.text = $"LEVEL {levelNumber} WILL BE OVERWRITTEN.";
            if (_overwriteConfirmationPanel != null)
                _overwriteConfirmationPanel.SetActive(true);
            return;
        }

        SavePendingLevel(_draft);
    }

    private void ConfirmOverwrite()
    {
        if (_overwriteConfirmationPanel != null)
            _overwriteConfirmationPanel.SetActive(false);
        SavePendingLevel(_pendingLevelToSave);
    }

    private void CancelOverwrite()
    {
        _pendingLevelToSave = null;
        if (_overwriteConfirmationPanel != null)
            _overwriteConfirmationPanel.SetActive(false);
    }

    private void SavePendingLevel(LevelData level)
    {
        if (level == null)
            return;

        if (!LevelRepository.SaveCustom(level, out string error))
        {
            Debug.LogError(error, this);
            return;
        }

        _pendingLevelToSave = null;
        _isEditing = false;
        _gameManager.SetEditorMode(false);
        ShowLevels();
    }

    private static char ValidateLevelNumberCharacter(
        string text,
        int characterIndex,
        char addedCharacter)
    {
        return char.IsDigit(addedCharacter) ? addedCharacter : '\0';
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }
}
