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
    private LevelDefinition _currentLevel;
    private LevelLoader _levelLoader;
    private TutorialPresenter _tutorialPresenter;
    private TMP_Text _resultLabel;
    private GameSaveData _saveData;
    private bool _gameOver;

    public bool IsGameOver
    {
        get { return _gameOver; }
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
        bool isFirstPlay = _saveData.playCount == 0 && !_saveData.hasSeenTutorial;
        _currentLevel = _startingLevel != null
            ? _startingLevel
            : Resources.Load<LevelDefinition>("Levels/FTUE");

        if (_currentLevel != null)
        {
            if (!_levelLoader.Load(_currentLevel))
            {
                _currentLevel = null;
                PlaceCatAtCenter();
                return;
            }
            if (isFirstPlay)
                _tutorialPresenter.Begin(_currentLevel);
            else
                _tutorialPresenter.Hide();
        }
        else
        {
            PlaceCatAtCenter();
            _tutorialPresenter.Hide();
        }

        _gameOver = false;
        _currentTurn = Turn.Player;
        RecordPlay();
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
        EndGame("CAT TRAPPED!");
    }

    private void OnCatEscaped()
    {
        EndGame("CAT ESCAPED!");
    }

    private void EndGame(string message)
    {
        if (_gameOver)
            return;

        _gameOver = true;
        _currentTurn = Turn.Player;
        Cat cat = _cat != null ? _cat.GetComponent<Cat>() : null;
        if (cat != null)
            cat.SetCanMove(false);
        if (_resultLabel != null)
            _resultLabel.text = message;
        if (_catTrappedScreen != null)
            _catTrappedScreen.SetActive(true);
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

        _gameOver = false;
        if (_catTrappedScreen != null)
            _catTrappedScreen.SetActive(false);

        if (_currentLevel != null && _levelLoader != null)
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
        RecordPlay();
        OnTurnChanged?.Invoke();
    }

    private void RecordPlay()
    {
        if (_saveData == null)
            _saveData = GameSaveSystem.Load();

        _saveData.playCount++;
        _saveData.hasSeenTutorial = true;
        GameSaveSystem.Save(_saveData);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
