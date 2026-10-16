using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] public int width, height;
    [SerializeField] public float gridCellSize = 1f;
    [SerializeField] public GameObject gridTilePrefab;
    [SerializeField] public GameObject originObject;
    [SerializeField] private bool _debugGrid;

    public Grid<GridMapObject> grid;
    public float fullNumberOfGrids;
    public float totalNumberOfGrids;
    public float emptyNumberOfGrids;

    private readonly List<Coordinates> _emptyTileCoordinates = new List<Coordinates>();
    private readonly List<Coordinates> _fullTileCoordinates = new List<Coordinates>();
    private readonly List<Coordinates> _boundaryCoordinates = new List<Coordinates>();
    private readonly List<Coordinates> _catCoordinates = new List<Coordinates>();

    public static GridManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        InitiateGrid();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void InitiateGrid()
    {
        InitiateGrid(width, height);
    }

    public void InitiateGrid(int gridWidth, int gridHeight)
    {
        if (gridWidth < 3 || gridHeight < 3 || gridCellSize <= 0f ||
            originObject == null || gridTilePrefab == null)
        {
            Debug.LogError("Grid requires dimensions of at least 3x3, a positive cell size, and assigned origin/tile references.", this);
            return;
        }

        ClearTileObjects();
        width = gridWidth;
        height = gridHeight;
        _emptyTileCoordinates.Clear();
        _fullTileCoordinates.Clear();
        _boundaryCoordinates.Clear();
        _catCoordinates.Clear();

        grid = new Grid<GridMapObject>(
            width,
            height,
            gridCellSize,
            originObject.transform.position,
            (g, x, y) => new GridMapObject(g, x, y),
            _debugGrid);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid.GetGridObject(x, y).SetType(GridType.Empty);
                _emptyTileCoordinates.Add(new Coordinates(x, y));

                GameObject tile = Instantiate(
                    gridTilePrefab,
                    GetCenterGridPosition(x, y),
                    Quaternion.identity,
                    transform);
                tile.name = $"Tile_{x}_{y}";
            }
        }

        totalNumberOfGrids = width * height;
        SetBoundaries();
        UpdateCounters();
    }

    public bool SetGridAsFull(GridMapObject cell, Vector2 position)
    {
        if (grid == null || cell == null)
            return false;

        grid.GetXY(position, out int x, out int y);
        if (!IsInside(x, y) || cell.GetCellType() != GridType.Empty)
            return false;

        return SetCellType(x, y, GridType.Full);
    }

    public bool SetCellType(int x, int y, GridType type)
    {
        if (!IsInside(x, y))
            return false;

        GridMapObject cell = grid.GetGridObject(x, y);
        GridType previousType = cell.GetCellType();
        if (previousType == type)
            return true;

        if (previousType == GridType.Cat && type != GridType.Cat)
            _catCoordinates.Remove(new Coordinates(x, y));
        if (previousType == GridType.Empty)
            _emptyTileCoordinates.Remove(new Coordinates(x, y));
        if (previousType == GridType.Full || previousType == GridType.Obstacle)
            _fullTileCoordinates.Remove(new Coordinates(x, y));
        if (previousType == GridType.Boundary)
            _boundaryCoordinates.Remove(new Coordinates(x, y));

        cell.SetType(type);

        if (type == GridType.Empty && !IsBoundary(x, y))
            _emptyTileCoordinates.Add(new Coordinates(x, y));
        else if (type == GridType.Full || type == GridType.Obstacle)
            _fullTileCoordinates.Add(new Coordinates(x, y));
        else if (type == GridType.Boundary)
            _boundaryCoordinates.Add(new Coordinates(x, y));
        else if (type == GridType.Cat)
            _catCoordinates.Add(new Coordinates(x, y));

        UpdateCounters();
        return true;
    }

    public void SetCatPosition(int x, int y)
    {
        for (int i = 0; i < _catCoordinates.Count; i++)
        {
            Coordinates previous = _catCoordinates[i];
            SetCellType(previous.X, previous.Y, IsBoundary(previous.X, previous.Y)
                ? GridType.Boundary
                : GridType.Empty);
        }

        if (!IsInside(x, y) || !grid.GetGridObject(x, y).isWalkable)
        {
            Debug.LogError($"Cannot place cat at blocked or out-of-bounds grid cell ({x}, {y}).", this);
            return;
        }

        SetCellType(x, y, GridType.Cat);
    }

    public void SetBoundaries()
    {
        if (grid == null)
            return;

        for (int x = 0; x < width; x++)
        {
            SetBoundaryIfEmpty(x, 0);
            SetBoundaryIfEmpty(x, height - 1);
        }

        for (int y = 1; y < height - 1; y++)
        {
            SetBoundaryIfEmpty(0, y);
            SetBoundaryIfEmpty(width - 1, y);
        }
    }

    public void DestroyGrid()
    {
        ClearTileObjects();
        grid = null;
        _emptyTileCoordinates.Clear();
        _fullTileCoordinates.Clear();
        _boundaryCoordinates.Clear();
        _catCoordinates.Clear();
        totalNumberOfGrids = 0;
        UpdateCounters();
    }

    public Vector3 GetCenterGridPosition()
    {
        return GetCenterGridPosition(width / 2, height / 2);
    }

    public Vector3 GetCenterGridPosition(int x, int y)
    {
        return grid.GetWorldPosition(x, y) +
               new Vector3(gridCellSize * 0.5f, gridCellSize * 0.5f, 0f);
    }

    public List<Coordinates> GetBoundaryCoordinates()
    {
        return _boundaryCoordinates;
    }

    public bool IsInside(int x, int y)
    {
        return grid != null && x >= 0 && y >= 0 && x < width && y < height;
    }

    private void SetBoundaryIfEmpty(int x, int y)
    {
        if (grid.GetGridObject(x, y).GetCellType() == GridType.Empty)
            SetCellType(x, y, GridType.Boundary);
    }

    private bool IsBoundary(int x, int y)
    {
        return x == 0 || y == 0 || x == width - 1 || y == height - 1;
    }

    private void UpdateCounters()
    {
        emptyNumberOfGrids = _emptyTileCoordinates.Count;
        fullNumberOfGrids = _fullTileCoordinates.Count;
    }

    private void ClearTileObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }
}
