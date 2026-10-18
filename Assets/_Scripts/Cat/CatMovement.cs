using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatMovement : MonoBehaviour
{
    public static event Action OnCatTrapped;
    public static event Action OnCatEscaped;

    public List<Vector3> pathVectorList;
    public Pathfinding pathfinding;

    [SerializeField] private float _speed = 2.5f;
    [SerializeField] private CatDifficulty _difficulty = CatDifficulty.Normal;

    private Cat _cat;
    private GridManager _gridManager;
    private LineRenderer _lineRenderer;
    private CatSpriteAnimator _spriteAnimator;
    private bool _isMoving;

    public CatDifficulty Difficulty
    {
        get { return _difficulty; }
    }

    private void Awake()
    {
        _cat = GetComponent<Cat>();
        _gridManager = GridManager.Instance;
        _lineRenderer = GetComponent<LineRenderer>();
        _spriteAnimator = GetComponent<CatSpriteAnimator>();
        RefreshPathfinding();
    }

    private void Update()
    {
        if (_cat != null && _cat.canMove && !_isMoving)
            TakeCatTurn();
    }

    public void RefreshPathfinding()
    {
        _gridManager = GridManager.Instance;
        if (_gridManager != null)
        {
            pathfinding = new Pathfinding(_gridManager.width, _gridManager.height);
        }
    }

    public void ResetMovement()
    {
        StopAllCoroutines();
        _isMoving = false;
        pathVectorList = null;
        if (_spriteAnimator != null)
            _spriteAnimator.PlayIdle();
        if (_lineRenderer != null)
            _lineRenderer.positionCount = 0;
    }

    public void SetDifficulty(CatDifficulty difficulty)
    {
        _difficulty = difficulty;
    }

    private void TakeCatTurn()
    {
        if (_gridManager == null || _gridManager.grid == null || pathfinding == null)
            return;

        if (!_gridManager.TryGetGridCoordinates(transform.position, out int catX, out int catY))
        {
            _cat.SetCanMove(false);
            OnCatTrapped?.Invoke();
            return;
        }
        List<PathNode> path = FindClosestBoundaryPath(catX, catY);
        if (path == null || path.Count < 2)
        {
            _cat.SetCanMove(false);
            OnCatTrapped?.Invoke();
            return;
        }

        pathVectorList = new List<Vector3>();
        for (int i = 1; i < path.Count; i++)
            pathVectorList.Add(_gridManager.GetCenterGridPosition(path[i].x, path[i].y));
        UpdateLineRenderer();

        PathNode nextNode = path[1];
        Vector3 direction = _gridManager.GetCenterGridPosition(nextNode.x, nextNode.y) - transform.position;
        _spriteAnimator?.SetFacing(direction);
        bool escaping = _gridManager.IsPerimeterCell(nextNode.x, nextNode.y);
        _spriteAnimator?.PlayMovement(escaping);
        _isMoving = true;
        StartCoroutine(MoveOneCell(
            _gridManager.GetCenterGridPosition(nextNode.x, nextNode.y),
            nextNode.x,
            nextNode.y,
            escaping));
    }

    private List<PathNode> FindClosestBoundaryPath(int catX, int catY)
    {
        List<PathNode> closestPath = null;
        int bestCost = int.MaxValue;
        int blockedAdjacencyPenalty = GetBlockedAdjacencyPenalty();
        foreach (Coordinates coordinate in _gridManager.GetBoundaryCoordinates())
        {
            GridMapObject cell = _gridManager.grid.GetGridObject(coordinate.X, coordinate.Y);
            if (cell == null || cell.GetCellType() != GridType.Boundary)
                continue;

            List<PathNode> path = pathfinding.FindPath(
                catX,
                catY,
                coordinate.X,
                coordinate.Y,
                blockedAdjacencyPenalty);
            int routeCost = pathfinding.CalculateRouteCost(path, blockedAdjacencyPenalty);
            if (path != null && routeCost < bestCost)
            {
                closestPath = path;
                bestCost = routeCost;
            }
        }
        return closestPath;
    }

    private int GetBlockedAdjacencyPenalty()
    {
        switch (_difficulty)
        {
            case CatDifficulty.Normal:
                return 4;
            case CatDifficulty.Hard:
                return 10;
            default:
                return 0;
        }
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
            if (GameSession.Current != null)
                GameSession.Current.ChangeTurn();
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
