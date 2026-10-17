using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public static event Action OnTurnChanged;
    public static event Action OnPlayerMovePlaced;

    [SerializeField] private GameObject _cat;
    [SerializeField] private Button _restartButton;
    [SerializeField] private GameObject _catTrappedScreen;
    [SerializeField] private Button _catTrappedReplayButton;
    [SerializeField] private LevelDefinition _startingLevel;

    private Turn _currentTurn;
    private GridManager _gridManager;
    private CatMovement _catMovement;
    private LevelData _currentLevel;
    private LevelLoader _levelLoader;
    private TutorialPresenter _tutorialPresenter;
    private GameFlowUI _flowUI;
    private TMP_Text _resultLabel;
    private GameSaveData _saveData;
    private bool _gameOver;
    private bool _editorMode;

    public bool IsGameOver
    {
        get { return _gameOver; }
    }

    public GameSaveData SaveData
    {
        get { return _saveData; }
    }

    public bool IsEditorMode
    {
        get { return _editorMode; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (_restartButton != null)
            _restartButton.onClick.AddListener(RestartGame);
        if (_catTrappedReplayButton != null)
            _catTrappedReplayButton.onClick.AddListener(RestartGame);
        CatMovement.OnCatTrapped += OnCatTrapped;
        CatMovement.OnCatEscaped += OnCatEscaped;
    }

    private void OnDisable()
    {
        if (_restartButton != null)
            _restartButton.onClick.RemoveListener(RestartGame);
        if (_catTrappedReplayButton != null)
            _catTrappedReplayButton.onClick.RemoveListener(RestartGame);
        CatMovement.OnCatTrapped -= OnCatTrapped;
        CatMovement.OnCatEscaped -= OnCatEscaped;
    }

    private void Start()
    {
        _gridManager = GridManager.Instance;
        _catMovement = _cat != null ? _cat.GetComponent<CatMovement>() : null;
        _resultLabel = _catTrappedScreen != null
            ? _catTrappedScreen.GetComponentInChildren<TMP_Text>(true)
            : null;
        if (_catTrappedScreen != null)
            _catTrappedScreen.SetActive(false);

        if (_gridManager == null || _cat == null || _catMovement == null)
        {
            Debug.LogError("GameManager requires a GridManager, Cat, and CatMovement component.", this);
            return;
        }

        _tutorialPresenter = GetComponent<TutorialPresenter>();
        if (_tutorialPresenter == null)
            _tutorialPresenter = gameObject.AddComponent<TutorialPresenter>();

        _levelLoader = new LevelLoader(_gridManager, _cat.transform, _catMovement);
        _saveData = GameSaveSystem.Load();
        string selectedLevelId = LevelLaunchSelection.Consume();
        _gameOver = true;
        _cat.SetActive(false);
        _tutorialPresenter.Hide();
        _flowUI = gameObject.AddComponent<GameFlowUI>();
        _flowUI.Initialize(this, _gridManager);
        if (!string.IsNullOrWhiteSpace(selectedLevelId))
            StartLevelById(selectedLevelId);
    }

    public void StartLevelById(string levelId)
    {
        LevelData level = LevelRepository.LoadById(levelId);
        if (level == null)
        {
            Debug.LogError($"Could not find level '{levelId}'.");
            if (_flowUI != null)
                _flowUI.ShowLevels();
            return;
        }

        bool isCustom = LevelRepository.IsCustomLevel(level);
        bool isUnlocked = _saveData != null &&
                          _saveData.unlockedLevelIds != null &&
                          _saveData.unlockedLevelIds.Contains(level.levelId);
        if (!isCustom && !isUnlocked)
        {
            Debug.LogWarning($"Level '{level.displayName}' is still locked.");
            if (_flowUI != null)
                _flowUI.ShowLevels();
            return;
        }

        if (!_levelLoader.Load(level))
        {
            Debug.LogError($"Could not load level '{level.displayName}'.");
            if (_flowUI != null)
                _flowUI.ShowLevels();
            return;
        }

        _currentLevel = level;
        _editorMode = false;
        _gameOver = false;
        _currentTurn = Turn.Player;
        if (_catTrappedScreen != null)
            _catTrappedScreen.SetActive(false);
        _cat.SetActive(true);
        bool showTutorial = _currentLevel.levelId == "ftue-01" &&
                            !_saveData.hasSeenTutorial;
        if (showTutorial)
            _tutorialPresenter.Begin(_currentLevel);
        else
            _tutorialPresenter.Hide();

        RecordPlay(showTutorial);
        if (_flowUI != null)
            _flowUI.ShowGameplay();
        OnTurnChanged?.Invoke();
    }

    public void ChangeTurn()
    {
        if (_gameOver)
            return;

        _currentTurn = _currentTurn == Turn.Player ? Turn.Cat : Turn.Player;
        OnTurnChanged?.Invoke();
    }

    public Turn GetCurrentTurn()
    {
        return _currentTurn;
    }

    public void RegisterPlayerMove()
    {
        if (_gameOver || _currentTurn != Turn.Player)
            return;

        OnPlayerMovePlaced?.Invoke();
        if (_tutorialPresenter != null)
            _tutorialPresenter.OnPlayerMovePlaced();
        ChangeTurn();
    }

    public void OnCatTrapped()
    {
        EndGame("CAT TRAPPED!", true);
    }

    private void OnCatEscaped()
    {
        EndGame("CAT ESCAPED!", false);
    }

    private void EndGame(string message, bool playerWon)
    {
        if (_gameOver)
            return;

        _gameOver = true;
        _currentTurn = Turn.Player;
        Cat cat = _cat != null ? _cat.GetComponent<Cat>() : null;
        if (cat != null)
            cat.SetCanMove(false);
        if (playerWon)
            _cat.GetComponent<CatSpriteAnimator>()?.PlayTrappedReaction();
        if (_resultLabel != null)
            _resultLabel.text = message;
        if (_catTrappedScreen != null)
            _catTrappedScreen.SetActive(true);
        if (playerWon && _currentLevel != null)
            GameProgression.RecordWin(_saveData, _currentLevel.levelId);
        OnTurnChanged?.Invoke();
    }

    private void PlaceCatAtCenter()
    {
        if (_gridManager == null || _cat == null)
            return;

        int x = _gridManager.width / 2;
        int y = _gridManager.height / 2;
        _cat.transform.position = _gridManager.GetCenterGridPosition(x, y);
        _gridManager.SetCatPosition(x, y);
        if (_catMovement != null)
            _catMovement.RefreshPathfinding();
    }

    public void RestartGame()
    {
        if (_gridManager == null || _catMovement == null)
            return;
        if (_currentLevel == null)
        {
            if (_flowUI != null)
                _flowUI.ShowHome();
            return;
        }

        _gameOver = false;
        if (_catTrappedScreen != null)
            _catTrappedScreen.SetActive(false);

        if (_levelLoader != null)
        {
            if (!_levelLoader.Load(_currentLevel))
            {
                _gameOver = true;
                if (_resultLabel != null)
                    _resultLabel.text = "LEVEL ERROR";
                if (_catTrappedScreen != null)
                    _catTrappedScreen.SetActive(true);
                return;
            }
            if (_tutorialPresenter != null)
                _tutorialPresenter.Hide();
        }
        else
        {
            _catMovement.ResetMovement();
            _gridManager.InitiateGrid();
            PlaceCatAtCenter();
        }

        _currentTurn = Turn.Player;
        RecordPlay(false);
        OnTurnChanged?.Invoke();
    }

    private void RecordPlay(bool tutorialShown)
    {
        if (_saveData == null)
            _saveData = GameSaveSystem.Load();

        _saveData.playCount++;
        if (tutorialShown)
            _saveData.hasSeenTutorial = true;
        GameSaveSystem.Save(_saveData);
    }

    public void SetEditorMode(bool enabled)
    {
        _editorMode = enabled;
        _gameOver = true;
        if (_catMovement != null)
            _catMovement.ResetMovement();
        if (_catTrappedScreen != null)
            _catTrappedScreen.SetActive(false);
        Cat cat = _cat != null ? _cat.GetComponent<Cat>() : null;
        if (cat != null)
            cat.SetCanMove(false);
        if (_cat != null)
            _cat.SetActive(false);
        if (_tutorialPresenter != null)
            _tutorialPresenter.Hide();
    }

    public void SetEditorCatVisible(bool visible)
    {
        if (_cat != null)
        {
            _cat.SetActive(visible);
            Cat cat = _cat.GetComponent<Cat>();
            if (cat != null)
                cat.SetCanMove(false);
        }
    }

    public void ReturnToHome()
    {
        _gameOver = true;
        _editorMode = false;
        _currentLevel = null;
        if (_catMovement != null)
            _catMovement.ResetMovement();
        if (_catTrappedScreen != null)
            _catTrappedScreen.SetActive(false);
        Cat cat = _cat != null ? _cat.GetComponent<Cat>() : null;
        if (cat != null)
            cat.SetCanMove(false);
        if (_cat != null)
            _cat.SetActive(false);
        if (_tutorialPresenter != null)
            _tutorialPresenter.Hide();
        if (_flowUI != null)
            _flowUI.ShowHome();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
