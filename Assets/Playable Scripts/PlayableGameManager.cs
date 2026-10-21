using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayableGameManager : MonoBehaviour, IGameSessionController
{
    [SerializeField] private GameObject _cat;
    [SerializeField] private TextAsset _levelData;
    [SerializeField] private GameObject _levelEndPanel;
    [SerializeField] private TMP_Text _levelEndText;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private int _maxPlayerMoves = 3;

    private GridManager _gridManager;
    private CatMovement _catMovement;
    private Turn _currentTurn;
    private bool _gameOver;
    private int _movesUsed;
    private PlayableCellData _tutorialCell;

    public bool IsGameOver
    {
        get { return _gameOver; }
    }

    public bool PlayerWon { get; private set; }
    public int MovesUsed { get { return _movesUsed; } }
    public int MovesLeft { get { return Mathf.Max(0, _maxPlayerMoves - _movesUsed); } }
    public PlayableCellData TutorialCell { get { return _tutorialCell; } }
    public GridManager Grid { get { return _gridManager; } }

    public bool IsEditorMode
    {
        get { return false; }
    }

    private void Awake()
    {
        GameSession.Attach(this);
    }

    private void OnEnable()
    {
        if (_retryButton != null)
            _retryButton.onClick.AddListener(RestartLevel);
        if (_continueButton != null)
            _continueButton.onClick.AddListener(ContinueToInstall);
        CatMovement.OnCatTrapped += OnCatTrapped;
        CatMovement.OnCatEscaped += OnCatEscaped;
    }

    private void Start()
    {
        _gridManager = GridManager.Instance;
        _catMovement = _cat != null ? _cat.GetComponent<CatMovement>() : null;
        if (_gridManager == null || _cat == null || _catMovement == null)
        {
            Debug.LogError("PlayableGameManager requires the grid and a Cat with CatMovement.", this);
            return;
        }

        if (_levelEndPanel != null)
            _levelEndPanel.SetActive(false);
        StartLevel();
    }

    private void OnDisable()
    {
        if (_retryButton != null)
            _retryButton.onClick.RemoveListener(RestartLevel);
        if (_continueButton != null)
            _continueButton.onClick.RemoveListener(ContinueToInstall);
        CatMovement.OnCatTrapped -= OnCatTrapped;
        CatMovement.OnCatEscaped -= OnCatEscaped;
    }

    public Turn GetCurrentTurn()
    {
        return _currentTurn;
    }

    public void RegisterPlayerMove()
    {
        if (_gameOver || _currentTurn != Turn.Player)
            return;

        _movesUsed++;
        ChangeTurn();
    }

    public void ChangeTurn()
    {
        if (_gameOver)
            return;

        _currentTurn = _currentTurn == Turn.Player ? Turn.Cat : Turn.Player;
        // Out of moves: the player never gets the turn back, so the cat walks out.
        if (_currentTurn == Turn.Player && _movesUsed >= _maxPlayerMoves)
            _currentTurn = Turn.Cat;
        GameSession.NotifyTurnChanged();
    }

    public void RestartLevel()
    {
        if (_gridManager == null || _catMovement == null)
            return;

        StartLevel();
    }

    private void StartLevel()
    {
        _gameOver = false;
        PlayerWon = false;
        _movesUsed = 0;
        _currentTurn = Turn.Player;
        if (_levelEndPanel != null)
            _levelEndPanel.SetActive(false);

        if (!PlayableLevelLoader.Load(
                _levelData,
                _gridManager,
                _cat.transform,
                _catMovement,
                out _tutorialCell))
        {
            _gameOver = true;
            return;
        }

        _cat.SetActive(true);
        GameSession.NotifyTurnChanged();
    }

    private void OnCatTrapped()
    {
        EndLevel("CAT TRAPPED!", true);
    }

    private void OnCatEscaped()
    {
        EndLevel("OH NO, IT ESCAPED!", false);
    }

    private void EndLevel(string message, bool playerWon)
    {
        if (_gameOver)
            return;

        _gameOver = true;
        PlayerWon = playerWon;
        _currentTurn = Turn.Player;
        Cat cat = _cat.GetComponent<Cat>();
        if (cat != null)
            cat.SetCanMove(false);
        if (playerWon)
            _cat.GetComponent<CatSpriteAnimator>()?.PlayTrappedReaction();

        if (_levelEndText != null)
            _levelEndText.text = message;
        if (_retryButton != null)
            _retryButton.gameObject.SetActive(true);
        if (_continueButton != null)
            _continueButton.gameObject.SetActive(true);
        if (_levelEndPanel != null)
            _levelEndPanel.SetActive(true);

        GameSession.NotifyTurnChanged();
#if UNITY_LUNA
        Luna.Unity.LifeCycle.GameEnded();
#endif
    }

    private void ContinueToInstall()
    {
#if UNITY_LUNA
        Luna.Unity.Playable.InstallFullGame();
#else
        RestartLevel();
#endif
    }

    private void OnDestroy()
    {
        GameSession.Detach(this);
    }
}
