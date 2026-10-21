using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatMovement : MonoBehaviour
{
    public static event Action OnCatTrapped;
    public static event Action OnCatEscaped;

    public List<Vector3> pathVectorList;

    [SerializeField] private float _speed = 2.5f;
    [SerializeField] private CatDifficulty _difficulty = CatDifficulty.Normal;

    private Cat _cat;
    private GridManager _gridManager;
    private LineRenderer _lineRenderer;
    private CatSpriteAnimator _spriteAnimator;
    private CatBrain _brain;
    private bool _isMoving;

    public CatDifficulty Difficulty
    {
        get { return _difficulty; }
    }

    private CatBrain Brain
    {
        get { return _brain ?? (_brain = new CatBrain(_difficulty)); }
    }

    private void Awake()
    {
        _cat = GetComponent<Cat>();
        _gridManager = GridManager.Instance;
        _lineRenderer = GetComponent<LineRenderer>();
        _spriteAnimator = GetComponent<CatSpriteAnimator>();
    }

    private void Update()
    {
        if (_cat != null && _cat.canMove && !_isMoving)
            TakeCatTurn();
    }

    public void RefreshPathfinding()
    {
        _gridManager = GridManager.Instance;
        Brain.Reset();
    }

    public void ResetMovement()
    {
        StopAllCoroutines();
        _isMoving = false;
        pathVectorList = null;
        Brain.Reset();
        if (_spriteAnimator != null)
            _spriteAnimator.PlayIdle();
        if (_lineRenderer != null)
            _lineRenderer.positionCount = 0;
    }

    public void SetDifficulty(CatDifficulty difficulty)
    {
        _difficulty = difficulty;
        _brain = new CatBrain(difficulty);
    }

    private void TakeCatTurn()
    {
        if (_gridManager == null || _gridManager.grid == null)
            return;

        if (!_gridManager.TryGetGridCoordinates(transform.position, out int catX, out int catY))
        {
            _cat.SetCanMove(false);
            OnCatTrapped?.Invoke();
            return;
        }

        CatBoard board = _gridManager.CreateCatBoard();
        int catIndex = board.Index(catX, catY);
        int nextIndex = Brain.ChooseMove(board, catIndex);
        if (nextIndex < 0)
        {
            _cat.SetCanMove(false);
            OnCatTrapped?.Invoke();
            return;
        }

        pathVectorList = new List<Vector3>();
        foreach (int index in board.BuildRouteToNearestExit(nextIndex))
        {
            Vector2Int cell = board.Cell(index);
            pathVectorList.Add(_gridManager.GetCenterGridPosition(cell.x, cell.y));
        }
        if (pathVectorList.Count == 0)
        {
            Vector2Int nextCell = board.Cell(nextIndex);
            pathVectorList.Add(_gridManager.GetCenterGridPosition(nextCell.x, nextCell.y));
        }
        UpdateLineRenderer();

        Vector2Int next = board.Cell(nextIndex);
        Vector3 direction = _gridManager.GetCenterGridPosition(next.x, next.y) - transform.position;
        _spriteAnimator?.SetFacing(direction);
        bool escaping = _gridManager.IsPerimeterCell(next.x, next.y);
        _spriteAnimator?.PlayMovement(escaping);
        _isMoving = true;
        StartCoroutine(MoveOneCell(
            _gridManager.GetCenterGridPosition(next.x, next.y),
            next.x,
            next.y,
            escaping));
    }

    private IEnumerator MoveOneCell(
        Vector3 destination,
        int destinationX,
        int destinationY,
        bool escaping)
    {
        while (Vector3.Distance(transform.position, destination) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                Mathf.Max(0.01f, _speed) * Time.deltaTime);
            yield return null;
        }

        transform.position = destination;
        _gridManager.SetCatPosition(destinationX, destinationY);
        pathVectorList = null;
        if (_lineRenderer != null)
            _lineRenderer.positionCount = 0;

        _isMoving = false;
        _cat.SetCanMove(false);
        if (escaping)
        {
            _spriteAnimator?.HoldCurrentFrame();
            OnCatEscaped?.Invoke();
        }
        else
        {
            _spriteAnimator?.PlayIdle();
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeTurn();
        }
    }

    private void UpdateLineRenderer()
    {
        if (_lineRenderer == null || pathVectorList == null)
            return;

        _lineRenderer.positionCount = pathVectorList.Count;
        for (int i = 0; i < pathVectorList.Count; i++)
            _lineRenderer.SetPosition(i, pathVectorList[i]);
    }

}
