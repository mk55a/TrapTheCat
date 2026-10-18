using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MouseInputHandler : MonoBehaviour
{
    private enum CursorState
    {
        Pointer,
        HandPoint,
        HandOpen,
        HandClosed
    }

    public static MouseInputHandler Instance;

    private Texture2D _pointerCursor;
    private Texture2D _handPointCursor;
    private Texture2D _handOpenCursor;
    private Texture2D _handClosedCursor;
    private CursorState _cursorState = (CursorState)(-1);
    private GridTile _hoveredTile;
    private bool _holdingSelectableTile;
    private bool _showOpenCursorAfterRelease;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _pointerCursor = Resources.Load<Texture2D>("UI/pointer_c");
        _handPointCursor = Resources.Load<Texture2D>("UI/hand_point");
        _handOpenCursor = Resources.Load<Texture2D>("UI/hand_open");
        _handClosedCursor = Resources.Load<Texture2D>("UI/hand_closed");

        if (_pointerCursor == null || _handPointCursor == null ||
            _handOpenCursor == null || _handClosedCursor == null)
            Debug.LogError("Could not load all cursor textures from Resources/UI.", this);
    }

    private void Start()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        CursorTooltip tooltip = null;

        foreach (Button button in buttons)
        {
            if (!TryGetTooltipText(button.transform, out string tooltipText))
                continue;

            if (tooltip == null)
                tooltip = CursorTooltip.Create();

            MenuButtonFeedback feedback = button.GetComponent<MenuButtonFeedback>();
            if (feedback == null)
                feedback = button.gameObject.AddComponent<MenuButtonFeedback>();
            feedback.Initialize(tooltip, tooltipText);
        }
    }

    private void Update()
    {
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        IGameSessionController gameSession = GameSession.Current;
        if (gameSession != null &&
            gameSession.IsEditorMode &&
            GameFlowUI.Current != null &&
            GameFlowUI.Current.IsEditingLevel)
        {
            HandleEditorPointer(overUI);
            return;
        }

        bool canPlace = TryGetPointerCell(out GridManager gridManager, out Vector3 worldPosition,
            out int x, out int y) &&
            !overUI &&
            gameSession != null &&
            !gameSession.IsGameOver &&
            gameSession.GetCurrentTurn() == Turn.Player &&
            gridManager.CanPlaceBlockAt(worldPosition);

        if (Input.GetMouseButtonDown(0))
        {
            _holdingSelectableTile = canPlace;
            if (_holdingSelectableTile)
            {
                SetCursor(CursorState.HandClosed);
                GridMapObject cell = gridManager.grid.GetGridObject(x, y);
                gridManager.GetTileAt(x, y)?.PlayClickFeedback();
                if (gridManager.SetGridAsFull(cell, worldPosition))
                    gameSession.RegisterPlayerMove();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            _showOpenCursorAfterRelease = _holdingSelectableTile;
            _holdingSelectableTile = false;
        }

        if (_showOpenCursorAfterRelease)
        {
            SetCursor(CursorState.HandOpen);
            _showOpenCursorAfterRelease = false;
        }
        else if (Input.GetMouseButton(0) && _holdingSelectableTile)
        {
            SetCursor(CursorState.HandClosed);
        }
        else
        {
            SetCursor(canPlace ? CursorState.HandPoint : CursorState.Pointer);
        }

        GridTile tileUnderPointer = canPlace ? gridManager.GetTileAt(x, y) : null;
        if (_hoveredTile != tileUnderPointer)
        {
            if (_hoveredTile != null)
                _hoveredTile.SetHovered(false);

            _hoveredTile = tileUnderPointer;
            if (_hoveredTile != null)
                _hoveredTile.SetHovered(true);
        }
    }

    private void HandleEditorPointer(bool overUI)
    {
        GridManager gridManager = null;
        int x = 0;
        int y = 0;
        bool overTile = !overUI &&
                        TryGetPointerCell(out gridManager, out _, out x, out y) &&
                        gridManager.IsActive(x, y) &&
                        GameFlowUI.Current.CanEditTiles;

        if (overTile && Input.GetMouseButtonDown(0))
            GameFlowUI.Current.HandleEditorCellClick(x, y);

        GridTile tileUnderPointer = overTile ? gridManager.GetTileAt(x, y) : null;
        if (_hoveredTile != tileUnderPointer)
        {
            if (_hoveredTile != null)
                _hoveredTile.SetHovered(false);
            _hoveredTile = tileUnderPointer;
            if (_hoveredTile != null)
                _hoveredTile.SetHovered(true);
        }

        SetCursor(overTile ? CursorState.HandPoint : CursorState.Pointer);
    }

    private bool TryGetPointerCell(
        out GridManager gridManager,
        out Vector3 worldPosition,
        out int x,
        out int y)
    {
        gridManager = GridManager.Instance;
        worldPosition = Vector3.zero;
        x = 0;
        y = 0;

        Camera mainCamera = Camera.main;
        if (mainCamera == null || gridManager == null || gridManager.grid == null)
            return false;

        worldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0f;
        return gridManager.TryGetGridCoordinates(worldPosition, out x, out y);
    }

    private static bool TryGetTooltipText(Transform buttonTransform, out string text)
    {
        for (Transform current = buttonTransform; current != null; current = current.parent)
        {
            string objectName = current.name;
            if (objectName.IndexOf("ButtonHome", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                text = "HOME";
                return true;
            }

            if (objectName.IndexOf("ButtonUndo", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                text = "UNDO";
                return true;
            }

            if (objectName.IndexOf("ButtonRetry", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("RestartButton", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                text = "RESTART";
                return true;
            }
        }

        text = null;
        return false;
    }

    private void SetCursor(CursorState state)
    {
        if (_cursorState == state)
            return;

        Texture2D texture;
        Vector2 hotspot;
        switch (state)
        {
            case CursorState.HandPoint:
                texture = _handPointCursor;
                hotspot = new Vector2(14f, 2f);
                break;
            case CursorState.HandOpen:
                texture = _handOpenCursor;
                hotspot = new Vector2(14f, 2f);
                break;
            case CursorState.HandClosed:
                texture = _handClosedCursor;
                hotspot = new Vector2(14f, 2f);
                break;
            default:
                texture = _pointerCursor;
                hotspot = new Vector2(4f, 4f);
                break;
        }

        if (texture == null)
            return;

        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        _cursorState = state;
    }

    private void OnDestroy()
    {
        if (_hoveredTile != null)
            _hoveredTile.SetHovered(false);
        if (Instance == this)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (Instance == this)
            Instance = null;
    }

    private void OnDisable()
    {
        if (_hoveredTile != null)
        {
            _hoveredTile.SetHovered(false);
            _hoveredTile = null;
        }
        if (Instance == this)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        _cursorState = (CursorState)(-1);
    }
}
