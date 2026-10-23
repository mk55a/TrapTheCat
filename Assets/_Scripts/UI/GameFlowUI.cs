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
    [SerializeField] private TMP_Text _difficultyLabel;
    [SerializeField] private GameObject _unlockConfirmationPanel;
    [SerializeField] private Button _cancelUnlockButton;
    [SerializeField] private Button _confirmUnlockButton;
    [SerializeField] private GameObject _overwriteConfirmationPanel;
    [SerializeField] private Button _cancelOverwriteButton;
    [SerializeField] private Button _confirmOverwriteButton;
    [SerializeField] private TMP_Text _overwriteMessage;
    [Tooltip("The editor's side panel; the board is framed in the screen space left of it.")]
    [SerializeField] private RectTransform _editorSidebar;

    private const float DefaultEditorSidebarFraction = 0.32f;
    private const float EditorSidebarGap = 0.02f;

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
    private GameObject _ringRoot;
    private Image _ringFill;
    private bool _draftValidated;
    private bool _busy;
    private Coroutine _job;
    private readonly Stack<EditAction> _history = new Stack<EditAction>();

    private struct EditAction
    {
        public bool IsCat;
        public Vector2Int Cell;
        public bool HadCat;
        public Vector2Int PreviousCat;
    }

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

        if (_editorSidebar == null)
            _editorSidebar = _editorPanel.transform.Find("Board Editor") as RectTransform;

        BindSceneControls();
        CreateGameplayBackButton();
        ShowHome();
        return true;
    }

    public void ShowHome()
    {
        _isEditing = false;
        _gridManager.SetReservedRightFraction(0f);
        _gameManager.SetEditorMode(false);
        _gridManager.DestroyGrid();
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
        _gridManager.SetReservedRightFraction(0f);
        _gameManager.SetEditorMode(false);
        _gridManager.DestroyGrid();
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
        _gridManager.SetReservedRightFraction(0f);
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
        CancelJob();
        _history.Clear();
        _sizeSlider.SetValueWithoutNotify(_selectedSize);
        _gridManager.SetReservedRightFraction(EditorSidebarFraction());
        RefreshEditorBoard(true);
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
        EditAction action = new EditAction
        {
            Cell = coordinate,
            HadCat = _draft.hasCatStart,
            PreviousCat = _draft.catStart
        };
        if (_editorTool == EditorTool.Cat)
        {
            if (_gridManager.IsPerimeterCell(x, y) || _draftBlocks.Contains(coordinate))
                return;

            action.IsCat = true;
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

        _history.Push(action);
        _draft.blockedCells = new List<Vector2Int>(_draftBlocks);
        RefreshEditorBoard();
    }

    private void UndoEdit()
    {
        if (_busy || _history.Count == 0)
            return;

        EditAction action = _history.Pop();
        if (action.IsCat)
        {
            _draft.hasCatStart = action.HadCat;
            _draft.catStart = action.PreviousCat;
        }
        else if (!_draftBlocks.Add(action.Cell))
        {
            _draftBlocks.Remove(action.Cell);
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
        BindEditorControls();
        BindButton(_undoButton, UndoEdit, nameof(_undoButton));

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

    // Reuses the Levels screen's back button so gameplay gets a matching one without scene edits.
    private void CreateGameplayBackButton()
    {
        if (_levelsBackButton == null)
            return;

        Button back = Instantiate(_levelsBackButton, _gameplayPanel.transform, false);
        back.name = "GameplayBackButton";
        back.onClick = new Button.ButtonClickedEvent();
        back.onClick.AddListener(_gameManager.ContinueToLevels);
    }

    private float EditorSidebarFraction()
    {
        if (_editorSidebar == null)
            return DefaultEditorSidebarFraction;

        Vector3[] corners = new Vector3[4];
        _editorSidebar.GetWorldCorners(corners);
        Canvas canvas = _editorSidebar.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        float left = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]).x;
        return Mathf.Clamp01(1f - left / Screen.width + EditorSidebarGap);
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
        if (_boardLocked || _busy)
            return;

        _selectedSize = Mathf.RoundToInt(value);
        ResetLayout();
        RefreshEditorBoard(true);
        UpdateEditorLabels();
    }

    private void ReturnToHomeFromEditor()
    {
        CancelJob();
        _isEditing = false;
        _gameManager.ReturnToHome();
    }

    private void BindEditorControls()
    {
        CreateLoadingRing();
        BindButton(_validateButton, ValidateDraft, nameof(_validateButton));
        BindButton(_generateButton, GenerateLevel, nameof(_generateButton));
        BindButton(_clearButton, ClearDraft, nameof(_clearButton));
        if (_statusLabel == null)
            Debug.LogError("Assign the _statusLabel TMP_Text reference.", this);
        else
            _statusLabel.text = string.Empty;
    }

    private void CreateLoadingRing()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(Mathf.Min(distance - 44f, 62f - distance));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        Sprite ring = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));

        GameObject root = new GameObject("LoadingRing", typeof(RectTransform));
        root.transform.SetParent(_editorPanel.transform, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(150f, 150f);

        Image track = new GameObject("Track", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        track.transform.SetParent(rect, false);
        track.sprite = ring;
        track.color = new Color(1f, 1f, 1f, 0.2f);
        track.raycastTarget = false;
        ((RectTransform)track.transform).anchorMin = Vector2.zero;
        ((RectTransform)track.transform).anchorMax = Vector2.one;
        ((RectTransform)track.transform).offsetMin = ((RectTransform)track.transform).offsetMax = Vector2.zero;

        _ringFill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        _ringFill.transform.SetParent(rect, false);
        _ringFill.sprite = ring;
        _ringFill.color = new Color(1f, 0.82f, 0.25f, 1f);
        _ringFill.type = Image.Type.Filled;
        _ringFill.fillMethod = Image.FillMethod.Radial360;
        _ringFill.fillOrigin = (int)Image.Origin360.Top;
        _ringFill.fillClockwise = true;
        _ringFill.raycastTarget = false;
        ((RectTransform)_ringFill.transform).anchorMin = Vector2.zero;
        ((RectTransform)_ringFill.transform).anchorMax = Vector2.one;
        ((RectTransform)_ringFill.transform).offsetMin = ((RectTransform)_ringFill.transform).offsetMax = Vector2.zero;

        _ringRoot = root;
        _ringRoot.SetActive(false);
    }

    private void SetProgress(float value)
    {
        if (_ringRoot == null)
            return;

        if (!_ringRoot.activeSelf)
        {
            _ringRoot.SetActive(true);
            _ringRoot.transform.SetAsLastSibling();
        }
        _ringFill.fillAmount = Mathf.Clamp01(value);
    }

    private void HideProgress()
    {
        if (_ringRoot != null)
            _ringRoot.SetActive(false);
    }

    private System.Collections.IEnumerator FinishProgress()
    {
        float start = _ringFill != null ? _ringFill.fillAmount : 0f;
        const float seconds = 0.35f;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            SetProgress(Mathf.Lerp(start, 1f, t / seconds));
            yield return null;
        }
        SetProgress(1f);
        yield return new WaitForSecondsRealtime(0.15f);
    }

    private System.Collections.IEnumerator SweepProgress(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            SetProgress(t / seconds);
            yield return null;
        }
        SetProgress(1f);
        yield return new WaitForSecondsRealtime(0.15f);
    }

    private void ClearDraft()
    {
        if (_busy)
            return;

        _job = StartCoroutine(ClearRoutine());
    }

    private System.Collections.IEnumerator ClearRoutine()
    {
        _busy = true;
        RefreshEditorButtons();
        SetStatus("Clearing...");
        yield return SweepProgress(0.3f);

        _draftBlocks.Clear();
        _history.Clear();
        _draft.blockedCells = new List<Vector2Int>();
        _draft.hasCatStart = false;
        _busy = false;
        _job = null;
        RefreshEditorBoard();
        HideProgress();
        SetStatus("Level cleared.");
        RefreshEditorButtons();
    }

    private void SetStatus(string message)
    {
        if (_statusLabel != null)
            _statusLabel.text = message;
        else
            Debug.Log(message, this);
    }

    private void InvalidateValidation()
    {
        _draftValidated = false;
        if (!_busy)
            SetStatus(string.Empty);
        RefreshEditorButtons();
    }

    private void RefreshEditorButtons()
    {
        if (_saveLevelButton != null)
            _saveLevelButton.interactable = _draftValidated && !_busy;
        if (_validateButton != null)
            _validateButton.interactable = _boardLocked && !_busy;
        if (_generateButton != null)
            _generateButton.interactable = !_busy;
        if (_clearButton != null)
            _clearButton.interactable = !_busy;
        if (_undoButton != null)
            _undoButton.interactable = !_busy && _history.Count > 0;
    }

    private void CancelJob()
    {
        if (_job != null)
            StopCoroutine(_job);
        _job = null;
        _busy = false;
        HideProgress();
    }

    private void ValidateDraft()
    {
        if (_busy)
            return;

        _job = StartCoroutine(ValidateRoutine());
    }

    private System.Collections.IEnumerator ValidateRoutine()
    {
        _busy = true;
        RefreshEditorButtons();
        SetStatus("Validating...");
        yield return SweepProgress(0.4f);

        _draft.blockedCells = new List<Vector2Int>(_draftBlocks);
        LevelValidationResult result = LevelValidator.ValidateWinnable(_draft);
        _busy = false;
        _job = null;
        HideProgress();
        _draftValidated = result.IsValid;
        SetStatus((result.IsValid ? "VALID: " : "INVALID: ") + result.Message);
        RefreshEditorButtons();
    }

    private void GenerateLevel()
    {
        if (_busy)
            return;

        _job = StartCoroutine(GenerateRoutine());
    }

    // Scatters blocks around a central cat, from sparse to dense, and keeps the first layout
    // the solver proves winnable for the selected difficulty.
    private System.Collections.IEnumerator GenerateRoutine()
    {
        const int attempts = 60;
        _busy = true;
        RefreshEditorButtons();
        SetStatus("Generating...");
        SetProgress(0f);
        yield return null;

        // Start fresh on a randomly chosen shape, then lock so the layout can be edited.
        int shapeCount = Enum.GetValues(typeof(BoardShape)).Length;
        _selectedShape = (BoardShape)UnityEngine.Random.Range(0, shapeCount);
        _boardLocked = false;
        _draftBlocks.Clear();
        _history.Clear();
        _draft.blockedCells = new List<Vector2Int>();
        _draft.hasCatStart = false;
        RefreshEditorBoard();
        _boardLocked = true;
        UpdateEditorLabels();
        yield return null;

        List<Vector2Int> cells = new List<Vector2Int>();
        Vector2Int cat = Vector2Int.zero;
        float bestDistance = float.MaxValue;
        Vector2 center = new Vector2((_draft.width - 1) * 0.5f, (_draft.height - 1) * 0.5f);
        bool hasCat = false;
        for (int x = 0; x < _draft.width; x++)
        {
            for (int y = 0; y < _draft.height; y++)
            {
                if (!_gridManager.IsActive(x, y))
                    continue;

                cells.Add(new Vector2Int(x, y));
                if (_gridManager.IsPerimeterCell(x, y))
                    continue;

                float distance = (new Vector2(x, y) - center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    cat = new Vector2Int(x, y);
                    hasCat = true;
                }
            }
        }

        if (!hasCat)
        {
            _busy = false;
            _job = null;
            SetStatus("This board is too small to generate a level.");
            RefreshEditorButtons();
            yield break;
        }

        cells.Remove(cat);
        _draft.hasCatStart = true;
        _draft.catStart = cat;
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            float density = Mathf.Lerp(0.08f, 0.45f, attempt / (float)(attempts - 1));
            int count = Mathf.RoundToInt(cells.Count * density);
            for (int i = 0; i < count; i++)
            {
                int pick = UnityEngine.Random.Range(i, cells.Count);
                Vector2Int swap = cells[i];
                cells[i] = cells[pick];
                cells[pick] = swap;
            }

            _draft.blockedCells = cells.GetRange(0, count);
            LevelValidationResult result = LevelValidator.ValidateWinnable(_draft, 100000, 250);
            if (result.IsValid)
            {
                yield return FinishProgress();
                _draftBlocks = new HashSet<Vector2Int>(_draft.blockedCells);
                RefreshEditorBoard();
                _busy = false;
                _job = null;
                _draftValidated = true;
                HideProgress();
                SetStatus("GENERATED. " + result.Message);
                RefreshEditorButtons();
                yield break;
            }

            SetProgress((attempt + 1) / (float)attempts);
            if (attempt % 3 == 2)
            {
                SetStatus($"Generating... ({attempt + 1}/{attempts})");
                yield return null;
            }
        }

        yield return FinishProgress();
        _busy = false;
        _job = null;
        HideProgress();
        _draft.blockedCells = new List<Vector2Int>(_draftBlocks);
        _draft.hasCatStart = false;
        RefreshEditorBoard();
        SetStatus("Couldn't find a winnable layout. Try again or change the difficulty.");
        RefreshEditorButtons();
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
            // The hex buttons show just the level number; the full name goes in the hover tooltip.
            int number = isCustom ? level.customLevelNumber : level.progressionOrder;
            string label = number.ToString();
            string tooltip = isCustom || string.IsNullOrWhiteSpace(level.displayName)
                ? $"LEVEL {number}"
                : $"LEVEL {number}  -  {level.displayName}";
            if (!isUnlocked)
                tooltip += "  -  LOCKED";
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
            feedback.Initialize(_cursorTooltip, tooltip.ToUpperInvariant());
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
        if (_boardLocked || _busy)
            return;

        int count = Enum.GetValues(typeof(BoardShape)).Length;
        int current = (int)_selectedShape;
        _selectedShape = (BoardShape)((current + direction + count) % count);
        ResetLayout();
        RefreshEditorBoard(true);
        UpdateEditorLabels();
    }

    // A new shape or size invalidates every placed block and the cat, so they are dropped silently.
    private void ResetLayout()
    {
        _draftBlocks.Clear();
        _history.Clear();
        _draft.blockedCells = new List<Vector2Int>();
        _draft.hasCatStart = false;
    }

    private void ToggleBoardLock()
    {
        _boardLocked = !_boardLocked;
        InvalidateValidation();
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
        InvalidateValidation();
        UpdateEditorLabels();
    }

    private void RefreshEditorBoard(bool animate = false)
    {
        if (_gridManager == null)
            return;

        InvalidateValidation();
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
        _gridManager.InitiateGrid(width, height, _selectedShape, animate);
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
        RefreshEditorButtons();
    }

    private void SaveDraft()
    {
        if (_busy)
            return;

        if (!_draftValidated)
        {
            SetStatus("Validate the level first.");
            return;
        }

        if (!int.TryParse(_levelNumberInput.text, out int levelNumber) ||
            levelNumber < 1 || levelNumber > 999)
        {
            SetStatus("Enter a custom level number from 1 to 999.");
            return;
        }

        _draft.customLevelNumber = levelNumber;
        _draft.levelId = $"custom-level-{levelNumber}";
        _draft.displayName = $"Level {levelNumber}";
        _draft.blockedCells = new List<Vector2Int>(_draftBlocks);
        LevelValidationResult validation = LevelValidator.ValidateWinnable(_draft);
        if (!validation.IsValid)
        {
            SetStatus(validation.Message);
            InvalidateValidation();
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
