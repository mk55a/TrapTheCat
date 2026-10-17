using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameFlowUI : MonoBehaviour
{
    private enum EditorTool
    {
        Blocks,
        Cat
    }

    public static GameFlowUI Current { get; private set; }

    private GameManager _gameManager;
    private GridManager _gridManager;
    private GameObject _homePanel;
    private GameObject _levelsPanel;
    private GameObject _editorPanel;
    private GameObject _gameplayPanel;
    private GameObject _confirmPanel;
    private GameObject _overwritePanel;
    private GameObject _clearLevelsPanel;
    private Transform _levelList;
    private TMP_Text _shapeLabel;
    private TMP_Text _sizeLabel;
    private TMP_Text _lockLabel;
    private TMP_Text _toolLabel;
    private TMP_Text _difficultyLabel;
    private TMP_Text _validationLabel;
    private TMP_Text _levelsLocationLabel;
    private TMP_Text _overwriteMessage;
    private TMP_Text _clearLevelsMessage;
    private TMP_InputField _levelNumberInput;
    private Slider _sizeSlider;
    private CursorTooltip _cursorTooltip;
    private GameObject _levelButtonPrefab;
    private Button _lockButton;
    private Button _clearLevelsButton;
    private Button _previousShapeButton;
    private Button _nextShapeButton;
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

    public void Initialize(GameManager gameManager, GridManager gridManager)
    {
        Current = this;
        _gameManager = gameManager;
        _gridManager = gridManager;
        _cursorTooltip = CursorTooltip.Create();
        _levelButtonPrefab = Resources.Load<GameObject>("UI/LevelButton");
        if (_levelButtonPrefab == null)
            Debug.LogError("Could not load the level button prefab from Resources/UI/LevelButton.", this);

        GameObject canvasObject = new GameObject("GameFlowCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        _homePanel = CreateFullPanel(canvasObject.transform, "HomeScreen");
        BuildHomePanel(_homePanel.transform);
        _levelsPanel = CreateFullPanel(canvasObject.transform, "LevelsScreen");
        BuildLevelsPanel(_levelsPanel.transform);
        BuildClearLevelsConfirmation(_levelsPanel.transform);
        _editorPanel = new GameObject("LevelEditorScreen", typeof(RectTransform), typeof(Image));
        _editorPanel.transform.SetParent(canvasObject.transform, false);
        SetFullRect(_editorPanel.GetComponent<RectTransform>());
        _editorPanel.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.13f, 0.84f);
        _editorPanel.GetComponent<Image>().raycastTarget = false;
        BuildEditorPanel(_editorPanel.transform);
        BuildConfirmationPanel(_editorPanel.transform);
        BuildOverwritePanel(_editorPanel.transform);
        _gameplayPanel = new GameObject("GameplayControls", typeof(RectTransform), typeof(Image));
        _gameplayPanel.transform.SetParent(canvasObject.transform, false);
        SetFullRect(_gameplayPanel.GetComponent<RectTransform>());
        _gameplayPanel.GetComponent<Image>().color = Color.clear;
        _gameplayPanel.GetComponent<Image>().raycastTarget = false;
        CreateButton(_gameplayPanel.transform, "HOME", new Vector2(-835f, 480f),
            new Vector2(170f, 64f), _gameManager.ReturnToHome);

        ShowHome();
    }

    public void ShowHome()
    {
        _isEditing = false;
        _gameManager.SetEditorMode(false);
        _homePanel.SetActive(true);
        _levelsPanel.SetActive(false);
        _editorPanel.SetActive(false);
        _gameplayPanel.SetActive(false);
    }

    public void ShowLevels()
    {
        _isEditing = false;
        _gameManager.SetEditorMode(false);
        _homePanel.SetActive(false);
        _levelsPanel.SetActive(true);
        _editorPanel.SetActive(false);
        _gameplayPanel.SetActive(false);
        PopulateLevels();
    }

    public void ShowGameplay()
    {
        _isEditing = false;
        _homePanel.SetActive(false);
        _levelsPanel.SetActive(false);
        _editorPanel.SetActive(false);
        _gameplayPanel.SetActive(true);
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
        _gameManager.SetEditorMode(true);
        UpdateEditorLabels();
        UpdateValidation();
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
        UpdateValidation();
    }

    private void BuildHomePanel(Transform parent)
    {
        Image panelImage = parent.GetComponent<Image>();
        panelImage.color = new Color(0.07f, 0.1f, 0.15f, 0.98f);
        CreateLabel(parent, "TRAP THE CAT", 64f, new Vector2(0f, 170f), new Vector2(800f, 100f));
        CreateButton(parent, "LEVELS", new Vector2(0f, 25f), new Vector2(380f, 92f),
            () => ShowLevels());
        CreateButton(parent, "LEVEL EDITOR", new Vector2(0f, -95f), new Vector2(380f, 92f),
            BeginEditor);
    }

    private void BuildLevelsPanel(Transform parent)
    {
        parent.GetComponent<Image>().color = new Color(0.07f, 0.1f, 0.15f, 0.98f);
        CreateLabel(parent, "SELECT A LEVEL", 50f, new Vector2(0f, 440f), new Vector2(800f, 80f));
        _levelsLocationLabel = CreateLabel(
            parent,
            string.Empty,
            16f,
            new Vector2(0f, 390f),
            new Vector2(1000f, 40f));
        CreateButton(parent, "BACK", new Vector2(-760f, 470f), new Vector2(200f, 68f),
            ShowHome);
        _clearLevelsButton = CreateButton(
            parent,
            "CLEAR LEVELS",
            new Vector2(760f, 470f),
            new Vector2(240f, 68f),
            ShowClearLevelsConfirmation,
            false);

        GameObject viewport = new GameObject(
            "LevelListViewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(Mask),
            typeof(ScrollRect));
        viewport.transform.SetParent(parent, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0.5f, 0.5f);
        viewportRect.anchorMax = new Vector2(0.5f, 0.5f);
        viewportRect.anchoredPosition = new Vector2(0f, -30f);
        viewportRect.sizeDelta = new Vector2(760f, 780f);
        viewport.GetComponent<Image>().color = Color.clear;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject listObject = new GameObject("LevelList", typeof(RectTransform));
        listObject.transform.SetParent(viewport.transform, false);
        RectTransform listRect = listObject.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 1f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.anchoredPosition = Vector2.zero;
        listRect.sizeDelta = Vector2.zero;
        ScrollRect scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = listRect;
        scroll.horizontal = false;
        _levelList = listObject.transform;
        _levelList.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
    }

    private void BuildEditorPanel(Transform parent)
    {
        CreateLabel(parent, "LEVEL EDITOR", 38f, new Vector2(0f, 500f), new Vector2(500f, 60f));
        CreateButton(parent, "BACK", new Vector2(-790f, 500f), new Vector2(190f, 64f),
            () =>
            {
                _isEditing = false;
                ShowHome();
            });

        _previousShapeButton = CreateButton(parent, "<", new Vector2(-670f, 420f),
            new Vector2(72f, 60f), () => ChangeShape(-1));
        _shapeLabel = CreateLabel(parent, "RECTANGLE", 26f, new Vector2(-500f, 420f),
            new Vector2(260f, 60f));
        _nextShapeButton = CreateButton(parent, ">", new Vector2(-330f, 420f),
            new Vector2(72f, 60f), () => ChangeShape(1));

        _sizeLabel = CreateLabel(parent, "SIZE 10", 22f, new Vector2(410f, 420f),
            new Vector2(180f, 50f));
        _sizeSlider = CreateSlider(parent, new Vector2(590f, 420f), 6, 16,
            value =>
            {
                if (_boardLocked)
                    return;
                _selectedSize = Mathf.RoundToInt(value);
                RefreshEditorBoard();
                UpdateEditorLabels();
                UpdateValidation();
            },
            330f);

        _lockLabel = CreateLabel(parent, "BOARD UNLOCKED", 18f, new Vector2(855f, 460f),
            new Vector2(210f, 34f));
        _lockButton = CreateButton(parent, "LOCK BOARD", new Vector2(855f, 420f),
            new Vector2(190f, 62f), ToggleBoardLock);

        CreateButton(parent, "PLACE BLOCKS", new Vector2(-580f, -400f), new Vector2(270f, 72f),
            () => SetEditorTool(EditorTool.Blocks));
        _toolLabel = CreateLabel(parent, "TOOL: BLOCKS", 20f, new Vector2(-300f, -400f),
            new Vector2(240f, 60f));
        CreateButton(parent, "SETUP CAT", new Vector2(-10f, -400f), new Vector2(240f, 72f),
            () => SetEditorTool(EditorTool.Cat));
        _difficultyLabel = CreateLabel(parent, "CAT: NORMAL", 20f, new Vector2(320f, -400f),
            new Vector2(240f, 60f));
        CreateButton(parent, "CHANGE", new Vector2(530f, -400f), new Vector2(170f, 72f),
            CycleDifficulty);
        _validationLabel = CreateLabel(parent, "Lock the board to place tiles.", 19f,
            new Vector2(0f, -475f), new Vector2(850f, 54f));

        CreateLabel(parent, "LEVEL NUMBER", 20f, new Vector2(710f, -205f),
            new Vector2(350f, 44f));
        _levelNumberInput = CreateInputField(parent, new Vector2(710f, -255f), new Vector2(350f, 68f));
        CreateButton(parent, "SAVE LEVEL", new Vector2(710f, -340f), new Vector2(280f, 76f),
            SaveDraft);
        CreateLabel(parent, $"Saved JSON: {LevelRepository.StorageLocation}",
            16f, new Vector2(710f, -395f), new Vector2(450f, 38f));
    }

    private void BuildConfirmationPanel(Transform parent)
    {
        _confirmPanel = CreateFullPanel(parent, "UnlockConfirmation");
        _confirmPanel.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 0.94f);
        CreateLabel(_confirmPanel.transform, "Changing the board clears its blocks and cat start.",
            28f, new Vector2(0f, 45f), new Vector2(700f, 120f));
        CreateButton(_confirmPanel.transform, "CANCEL", new Vector2(-170f, -70f),
            new Vector2(220f, 72f), () => _confirmPanel.SetActive(false));
        CreateButton(_confirmPanel.transform, "CLEAR AND UNLOCK", new Vector2(170f, -70f),
            new Vector2(300f, 72f), ConfirmUnlock);
        _confirmPanel.SetActive(false);
    }

    private void BuildOverwritePanel(Transform parent)
    {
        _overwritePanel = CreateFullPanel(parent, "OverwriteLevelConfirmation");
        _overwritePanel.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 0.96f);
        _overwriteMessage = CreateLabel(
            _overwritePanel.transform,
            "This level already exists. Overwrite it?",
            30f,
            new Vector2(0f, 55f),
            new Vector2(720f, 120f));
        CreateButton(_overwritePanel.transform, "NO", new Vector2(-170f, -70f),
            new Vector2(220f, 72f), CancelOverwrite);
        CreateButton(_overwritePanel.transform, "YES, OVERWRITE", new Vector2(170f, -70f),
            new Vector2(280f, 72f), ConfirmOverwrite);
        _overwritePanel.SetActive(false);
    }

    private void BuildClearLevelsConfirmation(Transform parent)
    {
        _clearLevelsPanel = CreateFullPanel(parent, "ClearLevelsConfirmation");
        _clearLevelsPanel.transform.SetAsLastSibling();
        _clearLevelsPanel.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 0.96f);
        _clearLevelsMessage = CreateLabel(
            _clearLevelsPanel.transform,
            "DELETE ALL CUSTOM LEVELS?\nFTUE (LEVEL 0) AND PLAYER PROGRESS WILL BE KEPT.",
            28f,
            new Vector2(0f, 55f),
            new Vector2(900f, 150f));
        CreateButton(_clearLevelsPanel.transform, "CANCEL", new Vector2(-170f, -70f),
            new Vector2(220f, 72f), CancelClearLevels);
        CreateButton(_clearLevelsPanel.transform, "YES, DELETE", new Vector2(170f, -70f),
            new Vector2(260f, 72f), ConfirmClearLevels);
        _clearLevelsPanel.SetActive(false);
    }

    private void PopulateLevels()
    {
        for (int i = _levelList.childCount - 1; i >= 0; i--)
            Destroy(_levelList.GetChild(i).gameObject);

        _levelsLocationLabel.text = $"Level data file: {LevelRepository.StorageLocation}";
        GameSaveData saveData = _gameManager.SaveData;
        HashSet<string> unlocked = new HashSet<string>(
            saveData != null && saveData.unlockedLevelIds != null
                ? saveData.unlockedLevelIds
                : new List<string>());
        List<LevelData> levels = LevelRepository.LoadAll();
        _clearLevelsButton.interactable = levels.Exists(LevelRepository.IsCustomLevel);
        if (levels.Count == 0)
        {
            _levelsLocationLabel.text += "\nNo levels found.";
            return;
        }

        if (_levelButtonPrefab == null)
        {
            _levelsLocationLabel.text += "\nLevelButton prefab is missing from Resources/UI.";
            return;
        }

        RectTransform content = _levelList.GetComponent<RectTransform>();
        content.sizeDelta = new Vector2(0f, Mathf.Max(780f, levels.Count * 106f));
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
            rowObject.name = $"LevelButton_{level.levelId}";
            RectTransform row = rowObject.GetComponent<RectTransform>();
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -20f - i * 106f);
            row.sizeDelta = new Vector2(650f, 84f);

            Button levelButton = rowObject.GetComponent<Button>();
            TMP_Text labelText = rowObject.GetComponentInChildren<TMP_Text>(true);
            if (levelButton == null || labelText == null)
            {
                Debug.LogError("LevelButton prefab requires a Button and a child TMP text component.",
                    rowObject);
                Destroy(rowObject);
                continue;
            }

            labelText.text = label;
            levelButton.interactable = isUnlocked;
            levelButton.onClick.AddListener(() => _gameManager.StartLevelById(level.levelId));
            MenuButtonFeedback feedback = rowObject.GetComponent<MenuButtonFeedback>();
            if (feedback == null)
                feedback = rowObject.AddComponent<MenuButtonFeedback>();
            feedback.Initialize(_cursorTooltip, label.ToUpperInvariant());
        }
    }

    private void ShowClearLevelsConfirmation()
    {
        _clearLevelsMessage.text =
            "DELETE ALL CUSTOM LEVELS?\nFTUE (LEVEL 0) AND PLAYER PROGRESS WILL BE KEPT.";
        _clearLevelsPanel.SetActive(true);
    }

    private void CancelClearLevels()
    {
        _clearLevelsPanel.SetActive(false);
    }

    private void ConfirmClearLevels()
    {
        if (!LevelRepository.ClearCustomLevels(out string error))
        {
            _clearLevelsMessage.text = error;
            return;
        }

        _clearLevelsPanel.SetActive(false);
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
        UpdateValidation();
    }

    private void ToggleBoardLock()
    {
        if (_boardLocked)
        {
            _confirmPanel.SetActive(true);
            return;
        }

        _boardLocked = true;
        UpdateEditorLabels();
        UpdateValidation();
    }

    private void ConfirmUnlock()
    {
        _confirmPanel.SetActive(false);
        _boardLocked = false;
        _draftBlocks.Clear();
        _draft.blockedCells.Clear();
        _draft.hasCatStart = false;
        RefreshEditorBoard();
        UpdateEditorLabels();
        UpdateValidation();
    }

    private void SetEditorTool(EditorTool tool)
    {
        _editorTool = tool;
        UpdateEditorLabels();
    }

    private void CycleDifficulty()
    {
        int count = Enum.GetValues(typeof(CatDifficulty)).Length;
        _selectedDifficulty = (CatDifficulty)(((int)_selectedDifficulty + 1) % count);
        _draft.catDifficulty = _selectedDifficulty;
        UpdateEditorLabels();
        UpdateValidation();
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
        _shapeLabel.text = _selectedShape.ToString().ToUpperInvariant();
        _sizeLabel.text = $"SIZE {_selectedSize}";
        _lockLabel.text = _boardLocked ? "BOARD LOCKED" : "BOARD UNLOCKED";
        _toolLabel.text = _editorTool == EditorTool.Blocks
            ? "TOOL: BLOCKS"
            : "TOOL: SET CAT";
        _difficultyLabel.text = $"CAT: {_selectedDifficulty.ToString().ToUpperInvariant()}";
        _previousShapeButton.interactable = !_boardLocked;
        _nextShapeButton.interactable = !_boardLocked;
        _sizeSlider.interactable = !_boardLocked;
        _lockButton.GetComponentInChildren<TMP_Text>().text =
            _boardLocked ? "UNLOCK BOARD" : "LOCK BOARD";
    }

    private void UpdateValidation()
    {
        if (_draft == null)
            return;

        LevelValidationResult result = LevelValidator.Validate(_draft);
        _validationLabel.text = result.IsValid
            ? $"{result.Message} Estimated puzzle difficulty: {result.DifficultyEstimate}."
            : result.Message;
    }

    private void SaveDraft()
    {
        if (!int.TryParse(_levelNumberInput.text, out int levelNumber) ||
            levelNumber < 1 || levelNumber > 999)
        {
            _validationLabel.text = "Enter a custom level number from 1 to 999.";
            return;
        }

        _draft.customLevelNumber = levelNumber;
        _draft.levelId = $"custom-level-{levelNumber}";
        _draft.displayName = $"Level {levelNumber}";
        _draft.blockedCells = new List<Vector2Int>(_draftBlocks);
        LevelValidationResult validation = LevelValidator.Validate(_draft);
        if (!validation.IsValid)
        {
            _validationLabel.text = validation.Message;
            return;
        }

        if (LevelRepository.HasCustomLevelNumber(levelNumber))
        {
            _pendingLevelToSave = _draft;
            _overwriteMessage.text = $"LEVEL {levelNumber} WILL BE OVERWRITTEN.";
            _overwritePanel.SetActive(true);
            return;
        }

        SavePendingLevel(_draft);
    }

    private void ConfirmOverwrite()
    {
        _overwritePanel.SetActive(false);
        SavePendingLevel(_pendingLevelToSave);
    }

    private void CancelOverwrite()
    {
        _pendingLevelToSave = null;
        _overwritePanel.SetActive(false);
    }

    private void SavePendingLevel(LevelData level)
    {
        if (level == null)
            return;

        if (!LevelRepository.SaveCustom(level, out string error))
        {
            _validationLabel.text = error;
            return;
        }

        _pendingLevelToSave = null;
        _isEditing = false;
        _gameManager.SetEditorMode(false);
        ShowLevels();
    }

    private static GameObject CreateFullPanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        SetFullRect(panel.GetComponent<RectTransform>());
        Image image = panel.GetComponent<Image>();
        image.color = Color.clear;
        return panel;
    }

    private static void SetFullRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TMP_Text CreateLabel(
        Transform parent,
        string text,
        float fontSize,
        Vector2 position,
        Vector2 size)
    {
        GameObject label = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(parent, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text component = label.GetComponent<TMP_Text>();
        component.text = text;
        component.font = GetDefaultFont();
        component.fontSize = fontSize;
        component.color = Color.white;
        component.alignment = TextAlignmentOptions.Center;
        component.enableWordWrapping = true;
        component.raycastTarget = false;
        return component;
    }

    private Button CreateButton(
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        Action onClick,
        bool interactable = true)
    {
        GameObject buttonObject = new GameObject(
            label,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.24f, 0.47f, 0.62f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.interactable = interactable;
        button.onClick.AddListener(() => onClick());
        MenuButtonFeedback feedback = buttonObject.AddComponent<MenuButtonFeedback>();
        feedback.Initialize(_cursorTooltip, label.ToUpperInvariant());

        TMP_Text text = CreateLabel(buttonObject.transform, label, 22f, Vector2.zero, size);
        return button;
    }

    private static Slider CreateSlider(
        Transform parent,
        Vector2 position,
        float minValue,
        float maxValue,
        Action<float> onChanged,
        float width)
    {
        GameObject sliderObject = new GameObject("BoardSize", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(width, 40f);

        CreateImage(sliderObject.transform, "Track", new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(width, 12f), new Color(0.18f, 0.22f, 0.27f, 1f));
        Image fill = CreateImage(sliderObject.transform, "Fill", Vector2.zero,
            Vector2.zero, new Vector2(width, 12f), new Color(0.37f, 0.73f, 0.55f, 1f));
        Image handle = CreateImage(sliderObject.transform, "Handle", new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(30f, 38f), Color.white);
        fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = minValue;
        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;
        slider.onValueChanged.AddListener(value => onChanged(value));
        return slider;
    }

    private static Image CreateImage(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_FontAsset GetDefaultFont()
    {
        if (TMP_Settings.defaultFontAsset != null)
            return TMP_Settings.defaultFontAsset;

        TMP_FontAsset fallback = Resources.Load<TMP_FontAsset>(
            "Fonts & Materials/LiberationSans SDF");
        if (fallback == null)
            Debug.LogError("Could not find a default TextMeshPro font for generated menu text.");
        return fallback;
    }

    private static TMP_InputField CreateInputField(
        Transform parent,
        Vector2 position,
        Vector2 size)
    {
        GameObject fieldObject = new GameObject(
            "LevelName",
            typeof(RectTransform),
            typeof(Image),
            typeof(TMP_InputField));
        fieldObject.transform.SetParent(parent, false);
        RectTransform rect = fieldObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        fieldObject.GetComponent<Image>().color = new Color(0.16f, 0.2f, 0.25f, 1f);

        TMP_Text text = CreateLabel(fieldObject.transform, string.Empty, 22f, Vector2.zero, size);
        TMP_Text placeholder = CreateLabel(fieldObject.transform, "LEVEL NAME", 20f,
            Vector2.zero, size);
        placeholder.color = new Color(0.72f, 0.76f, 0.8f, 0.8f);
        TMP_InputField input = fieldObject.GetComponent<TMP_InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.textViewport = fieldObject.GetComponent<RectTransform>();
        input.targetGraphic = fieldObject.GetComponent<Image>();
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterLimit = 3;
        input.onValidateInput += ValidateLevelNumberCharacter;
        input.text = string.Empty;
        placeholder.text = "ENTER NUMBER";
        return input;
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
