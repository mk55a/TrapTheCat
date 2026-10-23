using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] public int width, height;
    [SerializeField] public float gridCellSize = 1f;
    [SerializeField, Min(1f)] private float _tileSpacingMultiplier = 1.08f;
    [SerializeField, Range(0f, 0.45f)] private float _cameraMargin = 0.1f;
    [SerializeField] private BoardShape _boardShape = BoardShape.Rectangle;
    [SerializeField] public GameObject gridTilePrefab;
    [SerializeField] public GameObject originObject;
    [SerializeField] private bool _debugGrid;
    [SerializeField, Min(0f)] private float _spawnWaveDuration = 0.9f;
    [SerializeField, Min(0f)] private float _cameraMoveDuration = 0.45f;

    public Grid<GridMapObject> grid;
    public float fullNumberOfGrids;
    public float totalNumberOfGrids;
    public float emptyNumberOfGrids;

    private readonly List<Coordinates> _emptyTileCoordinates = new List<Coordinates>();
    private readonly List<Coordinates> _fullTileCoordinates = new List<Coordinates>();
    private readonly List<Coordinates> _boundaryCoordinates = new List<Coordinates>();
    private readonly List<Coordinates> _catCoordinates = new List<Coordinates>();
    private GridTile[,] _tileObjects;
    private bool[,] _activeCells;
    private bool[,] _perimeterCells;
    private bool _hasCameraHome;
    private Vector3 _cameraHome;
    private Vector2 _boardSize;
    private float _reservedRightFraction;
    private Coroutine _cameraMove;
    private Vector3 _cameraTargetPosition;
    private float _cameraTargetSize;
    public static GridManager Instance { get; private set; }

    public BoardShape Shape
    {
        get { return _boardShape; }
    }

    // Layout follows the hex_tiles_sheet art: 30x32 px hexagon, interlocking rows 24 px apart.
    // gridCellSize is the world width of one tile.
    private const float ArtWidthPx = 30f;
    private const float ArtHeightPx = 32f;
    private const float RowPitchPx = 24f;

    private float TileWidth { get { return gridCellSize; } }
    private float TileHeight { get { return gridCellSize * ArtHeightPx / ArtWidthPx; } }
    private float ColumnPitch { get { return gridCellSize * _tileSpacingMultiplier; } }
    private float RowPitch { get { return gridCellSize * RowPitchPx / ArtWidthPx * _tileSpacingMultiplier; } }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void InitiateGrid()
    {
        InitiateGrid(width, height, _boardShape);
    }

    public void InitiateGrid(int gridWidth, int gridHeight)
    {
        InitiateGrid(gridWidth, gridHeight, BoardShape.Rectangle);
    }

    // With animate set, tiles pop in as a wave rippling outward from the board centre.
    public void InitiateGrid(int gridWidth, int gridHeight, BoardShape shape, bool animate = false)
    {
        if (gridWidth < 3 || gridHeight < 3 || gridCellSize <= 0f ||
            _tileSpacingMultiplier < 1f ||
            originObject == null || gridTilePrefab == null)
        {
            Debug.LogError("Grid requires dimensions of at least 3x3, a positive cell size, and assigned origin/tile references.", this);
            return;
        }

        ClearTileObjects();
        width = gridWidth;
        height = gridHeight;
        _boardShape = shape;
        _activeCells = BoardTopology.BuildMask(width, height, shape);
        _perimeterCells = BuildPerimeterMask(width, height);
        CenterGridOnCamera();
        _emptyTileCoordinates.Clear();
        _fullTileCoordinates.Clear();
        _boundaryCoordinates.Clear();
        _catCoordinates.Clear();
        _tileObjects = new GridTile[width, height];

        grid = new Grid<GridMapObject>(
            width,
            height,
            gridCellSize,
            originObject.transform.position,
            (g, x, y) => new GridMapObject(g, x, y),
            _debugGrid);

        Vector3 boardCenter = GetCenterGridPosition(width / 2, height / 2);
        float maxDistance = 0.001f;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_activeCells[x, y])
                    maxDistance = Mathf.Max(
                        maxDistance,
                        Vector3.Distance(GetCenterGridPosition(x, y), boardCenter));
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!_activeCells[x, y])
                    continue;

                grid.GetGridObject(x, y).SetType(GridType.Empty);
                _emptyTileCoordinates.Add(new Coordinates(x, y));

                GameObject tile = Instantiate(
                    gridTilePrefab,
                    GetCenterGridPosition(x, y),
                    Quaternion.identity,
                    transform);
                tile.name = $"Tile_{x}_{y}";
                GridTile gridTile = tile.GetComponent<GridTile>();
                if (gridTile != null)
                {
                    _tileObjects[x, y] = gridTile;
                    gridTile.Initialize(grid.GetGridObject(x, y), TileWidth);
                    if (animate)
                    {
                        float ring = Vector3.Distance(GetCenterGridPosition(x, y), boardCenter) / maxDistance;
                        gridTile.PlaySpawn(ring * _spawnWaveDuration + Random.Range(0f, 0.06f));
                    }
                }
            }
        }

        totalNumberOfGrids = GetActiveCoordinates().Count;
        SetBoundaries();
        UpdateCounters();
    }

    public CatBoard CreateCatBoard()
    {
        if (grid == null || _activeCells == null)
            return null;

        List<Vector2Int> blocked = new List<Vector2Int>(_fullTileCoordinates.Count);
        foreach (Coordinates coordinate in _fullTileCoordinates)
            blocked.Add(new Vector2Int(coordinate.X, coordinate.Y));
        return new CatBoard(_activeCells, blocked);
    }

    public bool SetGridAsFull(GridMapObject cell, Vector2 position)
    {
        if (grid == null || cell == null)
            return false;

        if (!TryGetGridCoordinates(position, out int x, out int y))
            return false;
        if (!CanPlaceBlockAt(position) || grid.GetGridObject(x, y) != cell)
            return false;

        return SetCellType(x, y, GridType.Full);
    }

    public bool CanPlaceBlockAt(Vector3 position)
    {
        if (!TryGetGridCoordinates(position, out int x, out int y))
            return false;

        GridType type = grid.GetGridObject(x, y).GetCellType();
        return type == GridType.Empty || type == GridType.Boundary;
    }

    public GridTile GetTileAt(int x, int y)
    {
        if (!IsInside(x, y) || _tileObjects == null)
            return null;

        return _tileObjects[x, y];
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

        foreach (Coordinates coordinate in GetActiveCoordinates())
        {
            if (_perimeterCells[coordinate.X, coordinate.Y])
                SetBoundaryIfEmpty(coordinate.X, coordinate.Y);
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
        _tileObjects = null;
        _activeCells = null;
        _perimeterCells = null;
        totalNumberOfGrids = 0;
        UpdateCounters();
    }

    public Vector3 GetCenterGridPosition()
    {
        return GetCenterGridPosition(width / 2, height / 2);
    }

    public Vector3 GetCenterGridPosition(int x, int y)
    {
        float rowOffset = (y & 1) == 1 ? ColumnPitch * 0.5f : 0f;
        Vector3 origin = originObject.transform.position;
        return new Vector3(
            origin.x + TileWidth * 0.5f + x * ColumnPitch + rowOffset,
            origin.y + TileHeight * 0.5f + y * RowPitch,
            origin.z);
    }

    public bool TryGetGridCoordinates(Vector3 worldPosition, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (grid == null)
            return false;

        Vector3 origin = originObject.transform.position;
        float halfWidth = TileWidth * 0.5f;
        float halfHeight = TileHeight * 0.5f;
        int approximateRow = Mathf.RoundToInt((worldPosition.y - origin.y - halfHeight) / RowPitch);
        float closestDistance = float.MaxValue;
        bool found = false;

        for (int candidateY = approximateRow - 1; candidateY <= approximateRow + 1; candidateY++)
        {
            float rowOffset = (candidateY & 1) == 1 ? ColumnPitch * 0.5f : 0f;
            int approximateColumn = Mathf.RoundToInt(
                (worldPosition.x - origin.x - halfWidth - rowOffset) / ColumnPitch);

            for (int candidateX = approximateColumn - 1; candidateX <= approximateColumn + 1; candidateX++)
            {
                if (!IsInside(candidateX, candidateY))
                    continue;

                Vector3 center = GetCenterGridPosition(candidateX, candidateY);
                float dx = Mathf.Abs(worldPosition.x - center.x);
                float dy = Mathf.Abs(worldPosition.y - center.y);
                bool insideHex = dx <= halfWidth &&
                                 (dy <= halfHeight * 0.5f ||
                                  dx / halfWidth + 2f * dy / halfHeight <= 2f);
                if (!insideHex)
                    continue;

                float distance = dx * dx + dy * dy;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    x = candidateX;
                    y = candidateY;
                    found = true;
                }
            }
        }

        return found;
    }

    public List<Coordinates> GetBoundaryCoordinates()
    {
        return _boundaryCoordinates;
    }

    public bool IsInside(int x, int y)
    {
        return IsActive(x, y);
    }

    public bool IsActive(int x, int y)
    {
        return _activeCells != null && x >= 0 && y >= 0 &&
               x < width && y < height && _activeCells[x, y];
    }

    public bool IsPerimeterCell(int x, int y)
    {
        return IsBoundary(x, y);
    }

    public List<Coordinates> GetActiveCoordinates()
    {
        List<Coordinates> coordinates = new List<Coordinates>();
        if (_activeCells == null)
            return coordinates;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_activeCells[x, y])
                    coordinates.Add(new Coordinates(x, y));
            }
        }
        return coordinates;
    }

    public List<Coordinates> GetNeighborCoordinates(int x, int y)
    {
        List<Coordinates> neighbors = new List<Coordinates>(6);
        if (!IsActive(x, y))
            return neighbors;

        foreach (Vector2Int neighbor in BoardTopology.GetNeighbors(_activeCells, x, y))
            neighbors.Add(new Coordinates(neighbor.x, neighbor.y));
        return neighbors;
    }

    private void SetBoundaryIfEmpty(int x, int y)
    {
        if (grid.GetGridObject(x, y).GetCellType() == GridType.Empty)
            SetCellType(x, y, GridType.Boundary);
    }

    private bool IsBoundary(int x, int y)
    {
        return _perimeterCells != null && x >= 0 && y >= 0 &&
               x < width && y < height && _perimeterCells[x, y];
    }

    private bool[,] BuildPerimeterMask(int gridWidth, int gridHeight)
    {
        bool[,] perimeter = new bool[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (_activeCells[x, y])
                    perimeter[x, y] = BoardTopology.IsBoundary(_activeCells, x, y);
            }
        }
        return perimeter;
    }

    private void CenterGridOnCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("GridManager could not find the Main Camera; keeping the configured grid origin.", this);
            return;
        }

        Vector3 cameraForward = mainCamera.transform.forward;
        if (Mathf.Abs(cameraForward.z) < 0.0001f)
        {
            Debug.LogWarning("Main Camera does not face the grid plane; keeping the configured grid origin.", mainCamera);
            return;
        }

        // The board always sits at the camera's starting centre; the camera moves to frame it.
        bool firstFraming = !_hasCameraHome;
        if (firstFraming)
        {
            _cameraHome = mainCamera.transform.position;
            _hasCameraHome = true;
        }

        float horizontalSpacing = ColumnPitch;
        float verticalSpacing = RowPitch;
        float rightmostRowOffset = height > 1 ? horizontalSpacing * 0.5f : 0f;
        float gridCenterX = TileWidth * 0.5f + ((width - 1) * horizontalSpacing + rightmostRowOffset) * 0.5f;
        float gridCenterY = TileHeight * 0.5f + (height - 1) * verticalSpacing * 0.5f;
        _boardSize = new Vector2(
            (width - 1) * horizontalSpacing + rightmostRowOffset + TileWidth,
            (height - 1) * verticalSpacing + TileHeight);

        Vector3 currentOrigin = originObject.transform.position;
        float cameraDepth = (currentOrigin.z - _cameraHome.z) / cameraForward.z;
        Vector3 homeCenter = _cameraHome + cameraForward * cameraDepth;

        originObject.transform.position = new Vector3(
            homeCenter.x - gridCenterX,
            homeCenter.y - gridCenterY,
            currentOrigin.z);
        FrameCamera(mainCamera, !firstFraming);
    }

    // Keeps the right-hand fraction of the screen free (e.g. for the level editor panel) by
    // sliding and zooming the camera so the board fits in the remaining space.
    public void SetReservedRightFraction(float fraction)
    {
        fraction = Mathf.Clamp(fraction, 0f, 0.6f);
        if (Mathf.Approximately(fraction, _reservedRightFraction))
            return;

        _reservedRightFraction = fraction;
        Camera mainCamera = Camera.main;
        if (mainCamera != null && _hasCameraHome && _boardSize != Vector2.zero)
            FrameCamera(mainCamera, true);
    }

    private void FrameCamera(Camera mainCamera, bool animate)
    {
        float visibleFraction = 1f - 2f * _cameraMargin;
        float usableAspect = Mathf.Max(0.01f, mainCamera.aspect * (1f - _reservedRightFraction));
        float size = Mathf.Max(
            _boardSize.y / (2f * visibleFraction),
            _boardSize.x / (2f * usableAspect * visibleFraction));
        // Shift right so the board centre lands in the middle of the unreserved area.
        Vector3 position = _cameraHome +
                           mainCamera.transform.right * (_reservedRightFraction * size * mainCamera.aspect);

        bool sameTarget = _cameraMove != null &&
                          Mathf.Approximately(size, _cameraTargetSize) &&
                          (position - _cameraTargetPosition).sqrMagnitude < 0.000001f;
        if (sameTarget)
            return;

        if (_cameraMove != null)
            StopCoroutine(_cameraMove);
        _cameraMove = null;
        _cameraTargetPosition = position;
        _cameraTargetSize = size;

        bool alreadyThere = Mathf.Approximately(mainCamera.orthographicSize, size) &&
                            (mainCamera.transform.position - position).sqrMagnitude < 0.000001f;
        if (!animate || alreadyThere || _cameraMoveDuration <= 0f)
        {
            mainCamera.orthographicSize = size;
            mainCamera.transform.position = position;
            return;
        }

        _cameraMove = StartCoroutine(MoveCamera(mainCamera, position, size));
    }

    private System.Collections.IEnumerator MoveCamera(Camera mainCamera, Vector3 position, float size)
    {
        Vector3 startPosition = mainCamera.transform.position;
        float startSize = mainCamera.orthographicSize;
        for (float t = 0f; t < _cameraMoveDuration; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / _cameraMoveDuration);
            mainCamera.transform.position = Vector3.Lerp(startPosition, position, k);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, size, k);
            yield return null;
        }

        mainCamera.transform.position = position;
        mainCamera.orthographicSize = size;
        _cameraMove = null;
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
