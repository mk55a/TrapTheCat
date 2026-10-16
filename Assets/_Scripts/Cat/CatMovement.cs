using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatMovement : MonoBehaviour
{
    private static readonly string[] DirectionNames = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };

    public static event Action OnCatTrapped;
    public static event Action OnCatEscaped;

    public List<Vector3> pathVectorList;
    public Pathfinding pathfinding;

    [SerializeField] private float _speed = 2.5f;

    private Cat _cat;
    private GridManager _gridManager;
    private LineRenderer _lineRenderer;
    private SpriteRenderer _spriteRenderer;
    private readonly Dictionary<string, Sprite> _directionSprites = new Dictionary<string, Sprite>();
    private bool _isMoving;

    private void Awake()
    {
        _cat = GetComponent<Cat>();
        _gridManager = GridManager.Instance;
        _lineRenderer = GetComponent<LineRenderer>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        LoadDirectionalSprites();
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
            LoadDirectionalSprites();
            // Keep the cat's scale editable in the Inspector.
            // if (_spriteRenderer != null && _spriteRenderer.sprite != null)
            // {
            //     float targetHeight = _gridManager.gridCellSize * 0.9f;
            //     float spriteHeight = _spriteRenderer.sprite.bounds.size.y;
            //     if (spriteHeight > 0f)
            //         _spriteRenderer.transform.localScale = Vector3.one * (targetHeight / spriteHeight);
            // }
            pathfinding = new Pathfinding(_gridManager.width, _gridManager.height);
        }
    }

    public void ResetMovement()
    {
        StopAllCoroutines();
        _isMoving = false;
        pathVectorList = null;
        if (_lineRenderer != null)
            _lineRenderer.positionCount = 0;
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
        FaceDirection(_gridManager.GetCenterGridPosition(nextNode.x, nextNode.y) - transform.position);
        _isMoving = true;
        StartCoroutine(MoveOneCell(
            _gridManager.GetCenterGridPosition(nextNode.x, nextNode.y),
            nextNode.x,
            nextNode.y));
    }

    private List<PathNode> FindClosestBoundaryPath(int catX, int catY)
    {
        List<PathNode> closestPath = null;
        foreach (Coordinates coordinate in _gridManager.GetBoundaryCoordinates())
        {
            GridMapObject cell = _gridManager.grid.GetGridObject(coordinate.X, coordinate.Y);
            if (cell == null || cell.GetCellType() != GridType.Boundary)
                continue;

            List<PathNode> path = pathfinding.FindPath(catX, catY, coordinate.X, coordinate.Y);
            if (path != null && (closestPath == null || path.Count < closestPath.Count))
                closestPath = path;
        }
        return closestPath;
    }

    private IEnumerator MoveOneCell(Vector3 destination, int destinationX, int destinationY)
    {
        while (Vector3.Distance(transform.position, destination) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                Mathf.Max(0.01f, _speed) * Time.deltaTime);
            yield return null;
        }

        bool escaped = destinationX == 0 || destinationY == 0 ||
                       destinationX == _gridManager.width - 1 ||
                       destinationY == _gridManager.height - 1;
        transform.position = destination;
        _gridManager.SetCatPosition(destinationX, destinationY);
        pathVectorList = null;
        if (_lineRenderer != null)
            _lineRenderer.positionCount = 0;

        _isMoving = false;
        _cat.SetCanMove(false);
        if (escaped)
        {
            OnCatEscaped?.Invoke();
        }
        else if (GameManager.Instance != null)
        {
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

    private void LoadDirectionalSprites()
    {
        if (_directionSprites.Count != 8)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("Art/Chibi Gray Tabby Cat Sprite Sheet");
            foreach (Sprite sprite in sprites)
            {
                if (sprite.name.StartsWith("Cat_Sprite_", StringComparison.Ordinal))
                    _directionSprites[sprite.name.Substring("Cat_Sprite_".Length)] = sprite;
            }
        }

        if (_directionSprites.Count != 8)
        {
            Debug.LogError(
                "Cat sprite sheet must contain Cat_Sprite_N, NE, E, SE, S, SW, W, and NW.",
                this);
            return;
        }

        if (_spriteRenderer != null)
            _spriteRenderer.sprite = _directionSprites["S"];
    }

    private void FaceDirection(Vector3 direction)
    {
        if (_spriteRenderer == null || direction.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        int directionIndex = Mathf.RoundToInt(angle / 45f);
        string spriteDirection = DirectionNames[
            (directionIndex % DirectionNames.Length + DirectionNames.Length) % DirectionNames.Length];

        if (_directionSprites.TryGetValue(spriteDirection, out Sprite sprite))
            _spriteRenderer.sprite = sprite;
    }
}
