using TrapTheCat.Core;
using TrapTheCat.Presentation;
using TrapTheCat.Services;
using TrapTheCat.UI;
using UnityEngine;

namespace TrapTheCat.Flow
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private ScreenNavigator _screens;
        [SerializeField] private CatView _catView;
        [SerializeField] private LevelLibrary _levelLibrary;
        [SerializeField] private BoardShapeList _boardShapes;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private BoardCameraFramer _cameraFramer;
        [SerializeField] private BoardPointerInput _boardInput;
        [Header("Overlays")]
        [SerializeField] private CursorTooltip _cursorTooltipPrefab;
        [SerializeField] private TutorialPresenter _tutorialPrefab;

        private HomeScreen _homeScreen;
        private LevelsScreen _levelsScreen;
        private GameScreen _gameScreen;
        private LevelEditorScreen _editorScreen;
        private LevelData _currentLevel;
        private GameSession _session;
        private LevelLoader _levelLoader;
        private TutorialPresenter _tutorialPresenter;
        private CursorTooltip _cursorTooltip;
        private LevelCatalog _levelCatalog;
        private ProgressionService _progression;
        private bool _editorMode;

        private void Start()
        {
            if (_screens == null || _gridManager == null || _cameraFramer == null || _boardInput == null ||
                _catView == null || _levelLibrary == null || _boardShapes == null ||
                _cursorTooltipPrefab == null || _tutorialPrefab == null)
            {
                Debug.LogError("GameManager requires its ScreenNavigator, GridManager, BoardCameraFramer, BoardPointerInput, CatView, LevelLibrary, BoardShapeList, cursor tooltip and tutorial references.", this);
                return;
            }

            _levelCatalog = new LevelCatalog(
                _levelLibrary.CreateBundledLevels(),
                LevelCatalogStorage.ForCurrentPlatform(_levelLibrary.BundledCatalog),
                _boardShapes);
            _progression = new ProgressionService(
                new JsonFileSaveService(JsonFileSaveService.DefaultFilePath),
                _levelCatalog);
            _levelLoader = new LevelLoader(_gridManager, _catView);
            _tutorialPresenter = Instantiate(_tutorialPrefab);
            _tutorialPresenter.Hide();
            _cursorTooltip = Instantiate(_cursorTooltipPrefab);

            CreateScreens();
            _boardInput.IsCellSelectable = IsBoardCellSelectable;
            _boardInput.CellClicked += OnBoardCellClicked;
            ShowHome();
        }

        private void OnDestroy()
        {
            if (_boardInput != null)
                _boardInput.CellClicked -= OnBoardCellClicked;
            if (_homeScreen == null)
                return;

            _homeScreen.PlayClicked -= ShowLevelList;
            _homeScreen.LevelEditorClicked -= OpenLevelEditor;
            _levelsScreen.BackClicked -= ShowHome;
            _levelsScreen.LevelClicked -= StartLevel;
            _gameScreen.RetryClicked -= RestartLevel;
            _gameScreen.LevelListClicked -= ShowLevelList;
            _editorScreen.BackClicked -= ShowHome;
            _editorScreen.LevelSaved -= ShowLevelList;
            _editorScreen.CatVisibilityChanged -= SetCatVisible;
        }

        private void CreateScreens()
        {
            _screens.CreateScreens();
            _homeScreen = _screens.Get<HomeScreen>();
            _levelsScreen = _screens.Get<LevelsScreen>();
            _gameScreen = _screens.Get<GameScreen>();
            _editorScreen = _screens.Get<LevelEditorScreen>();

            _levelsScreen.Initialize(_levelCatalog, _progression, _screens.Popup, _cursorTooltip);
            _editorScreen.Initialize(_gridManager, _cameraFramer, _levelCatalog, _boardShapes, _screens.Popup);

            _homeScreen.PlayClicked += ShowLevelList;
            _homeScreen.LevelEditorClicked += OpenLevelEditor;
            _levelsScreen.BackClicked += ShowHome;
            _levelsScreen.LevelClicked += StartLevel;
            _gameScreen.RetryClicked += RestartLevel;
            _gameScreen.LevelListClicked += ShowLevelList;
            _editorScreen.BackClicked += ShowHome;
            _editorScreen.LevelSaved += ShowLevelList;
            _editorScreen.CatVisibilityChanged += SetCatVisible;
        }

        private void ShowHome()
        {
            LeaveBoard();
            _screens.Open<HomeScreen>();
        }

        private void ShowLevelList()
        {
            LeaveBoard();
            _screens.Open<LevelsScreen>();
        }

        private void OpenLevelEditor()
        {
            LeaveBoard();
            _editorMode = true;
            _screens.Open<LevelEditorScreen>();
        }

        private void StartLevel(LevelData level)
        {
            if (!_progression.IsUnlocked(level))
            {
                Debug.LogWarning($"Level '{level.displayName}' is still locked.");
                return;
            }

            if (!StartSession(level))
            {
                Debug.LogError($"Could not load level '{level.displayName}'.");
                ShowLevelList();
                return;
            }

            _currentLevel = level;
            _editorMode = false;
            _screens.Open<GameScreen>();
            _gameScreen.HideLevelEnd();
            _catView.gameObject.SetActive(true);
            bool showTutorial = _currentLevel.levelId == "ftue-01" && !_progression.HasSeenTutorial;
            if (showTutorial)
                _tutorialPresenter.Begin(_currentLevel);
            else
                _tutorialPresenter.Hide();

            _progression.RecordPlay(showTutorial);
        }

        private void RestartLevel()
        {
            if (_currentLevel == null)
            {
                ShowHome();
                return;
            }

            _gameScreen.HideLevelEnd();
            if (!StartSession(_currentLevel))
            {
                _gameScreen.ShowLevelEnd("LEVEL ERROR", false);
                return;
            }

            _tutorialPresenter.Hide();
            _progression.RecordPlay(false);
        }

        private void LeaveBoard()
        {
            _session = null;
            _editorMode = false;
            _currentLevel = null;
            _catView.ResetForLevel();
            _catView.gameObject.SetActive(false);
            _tutorialPresenter.Hide();
            _gridManager.DestroyGrid();
        }

        private void SetCatVisible(bool visible)
        {
            _catView.gameObject.SetActive(visible);
        }

        private bool IsBoardCellSelectable(Vector2Int cell)
        {
            return _editorMode ? _editorScreen.CanEditTiles : CanPlaceBlock(cell);
        }

        private void OnBoardCellClicked(Vector2Int cell)
        {
            if (_editorMode)
                _editorScreen.HandleEditorCellClick(cell);
            else
                PlaceBlock(cell);
        }

        private bool CanPlaceBlock(Vector2Int cell)
        {
            return _session != null && !_editorMode && !_catView.IsMoving && _session.CanPlaceBlock(cell);
        }

        private void PlaceBlock(Vector2Int cell)
        {
            if (!CanPlaceBlock(cell))
                return;

            CatTurn catTurn = _session.PlaceBlock(cell);
            _tutorialPresenter.OnPlayerMovePlaced();

            if (catTurn.Trapped)
                EndLevel("TRAPPED", true);
            else
                _catView.PlayMove(catTurn, OnCatArrived);
        }

        private void OnCatArrived(CatTurn catTurn)
        {
            if (catTurn.Escaped)
                EndLevel("CAT ESCAPED", false);
        }

        private bool StartSession(LevelData level)
        {
            BoardState board = _levelLoader.Load(level);
            _session = board != null ? new GameSession(board, level.catDifficulty) : null;
            return _session != null;
        }

        private void EndLevel(string message, bool playerWon)
        {
            if (playerWon)
                _catView.ShowTrapped();
            _gameScreen.ShowLevelEnd(message, playerWon);
            if (playerWon)
                _progression.RecordWin(_currentLevel.levelId);
        }
    }
}
