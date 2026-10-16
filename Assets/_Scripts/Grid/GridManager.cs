using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EmptyCharacter.Utils;
public class GridManager : MonoBehaviour
{
    [SerializeField] public int width, height;

    [SerializeField] public float gridCellSize;

    [SerializeField] public GameObject gridTilePrefab;

    [SerializeField] public GameObject originObject;

    private GameObject boundaryParent;

    private  GameObject fullParent;

    public Grid<GridMapObject> grid;


    [SerializeField] private List<Coordinates> _emptyTileCoordinates;
    [SerializeField] private List<Coordinates> _fullTileCoordinates ;//blue/captured
    [SerializeField] private List<Coordinates> _boundaryCoordinates;
    [SerializeField] private List<Coordinates> _allCoordinates;
    [SerializeField] private List<Coordinates> _catCoordinates;

    [SerializeField] private float _fillDelay;
    [SerializeField] private bool _debugGrid;

    public float fullNumberOfGrids;
    public float totalNumberOfGrids;
    public float emptyNumberOfGrids;

    public static GridManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType(typeof(GridManager)) as GridManager;
            return instance;
        }
        set
        {
            instance = value;
        }
    }

    private static GridManager instance;

    private void Awake()
    {
        _emptyTileCoordinates = new List<Coordinates>();
        _fullTileCoordinates = new List<Coordinates>();
        _boundaryCoordinates = new List<Coordinates>();
        _allCoordinates = new List<Coordinates>();
        InitiateGrid();
    }

    public void InitiateGrid()
    {
        _allCoordinates.Clear();
        _emptyTileCoordinates.Clear();

        totalNumberOfGrids = width * height;
        grid = new Grid<GridMapObject>(width, height, gridCellSize, originObject.transform.position, (Grid<GridMapObject> g, int x, int y) => new GridMapObject(g, x, y), _debugGrid);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = grid.GetWorldPosition(x, y) + new Vector3(gridCellSize, gridCellSize) * 0.5f;
                GameObject tile = Instantiate(gridTilePrefab, position, Quaternion.identity, transform);
                _allCoordinates.Add(new Coordinates(x, y));
                _emptyTileCoordinates.Add(new Coordinates(x, y));
                // Optional: Set up tile properties here
                tile.name = $"Tile_{x}_{y}";
            }
        }

        _allCoordinates = grid.AllGrids();
        totalNumberOfGrids = grid.allGridArray.Length;

        foreach(Coordinates coord in _emptyTileCoordinates)
        {
            SetGridAsEmpty(grid.gridArray[coord.X, coord.Y]);
        }

        SetBoundaries();
    }

   

    private void LateUpdate()
    {
        if (grid != null)
        {
            //OrganizeBlueGrids();
        }
    }


    public void SetGridAsFull(GridMapObject gridMapObject, Vector2 position)
    {
        gridMapObject.SetType(GridType.Full);

        int x, y;
        grid.GetXY(position, out x, out y);
        Debug.Log(x+","+ y);
        Coordinates coordinates = new Coordinates(x, y);

        if (_emptyTileCoordinates.Contains(coordinates))
        {
            Debug.Log("Removing from empty tiles");
            _emptyTileCoordinates.Remove(coordinates);
            emptyNumberOfGrids--;
        }
        /*else if (_boundaryCoordinates.Contains(coordinates))
        {
            Debug.Log("Removing from boundary");
            _boundaryCoordinates.Remove(coordinates);
        }*/
        else
        {
            Debug.Log("Coordinates not found in emptyTileCoordinates and boundaryTileCoordinates!");
        }

        _fullTileCoordinates.Add(coordinates);
        fullNumberOfGrids++;
    }

    public void SetGridAsFull(GridMapObject gridMapObject, int x, int y)
    {
        fullNumberOfGrids++;
        gridMapObject.SetType(GridType.Full);
    }

    public void DestroyGrid()
    {
        if (grid != null)
        {
            ClearGridArrayObjects();
            _emptyTileCoordinates.Clear();
            _fullTileCoordinates.Clear();
            _boundaryCoordinates.Clear();
            _allCoordinates.Clear();
        }

    }

    public void SetBoundaries()
    {
        _boundaryCoordinates.Clear();

        for (int x = 0; x < width; x++)
        {
            int y = 0;
            grid.GetGridObject(x, y).SetType(GridType.Boundary);
            grid.gridArray[x, y].SetType(GridType.Boundary);
            _boundaryCoordinates.Add(new Coordinates(x, y)); // Track boundary coordinates
        }
        for (int y = 1; y < height; y++)
        {
            int x = 0;
            grid.GetGridObject(x, y).SetType(GridType.Boundary);
            grid.gridArray[x, y].SetType(GridType.Boundary);
            _boundaryCoordinates.Add(new Coordinates(x, y)); // Track boundary coordinates
        }
        for (int x = 1; x < width; x++)
        {
            int y = height - 1;
            grid.GetGridObject(x, y).SetType(GridType.Boundary);
            grid.gridArray[x, y].SetType(GridType.Boundary);
            _boundaryCoordinates.Add(new Coordinates(x, y)); // Track boundary coordinates
        }
        for (int y = 1; y < height-1; y++)
        {
            int x = width - 1;
            grid.GetGridObject(x, y).SetType(GridType.Boundary);
            grid.gridArray[x, y].SetType(GridType.Boundary);
            _boundaryCoordinates.Add(new Coordinates(x, y)); // Track boundary coordinates
        }
    }

    public Vector3 GetCenterGridPosition()
    {
        int centerX = width / 2;
        int centerY = height / 2;
        Debug.Log(grid.GetWorldPosition(centerX, centerY));
        Debug.Log(new Vector3(grid.GetCellSize(), grid.GetCellSize()));
        return grid.GetWorldPosition(centerX, centerY) + new Vector3(grid.GetCellSize(), grid.GetCellSize()) * 0.5f;
    }

    public Vector3 GetCenterGridPosition(int x,int y)
    {
        return grid.GetWorldPosition(x, y) + new Vector3(grid.GetCellSize(), grid.GetCellSize()) * 0.5f;
    }

    public List<Coordinates> GetBoundaryCoordinates()
    {
        return _boundaryCoordinates;
    }
    private void SetGridAsEmpty(GridMapObject gridMapObject)
    {
        gridMapObject.SetType(GridType.Empty);
        emptyNumberOfGrids++;
    }

    private void ClearGridArrayObjects()
    {
        foreach (Transform child in this.gameObject.transform)
        {
            Destroy(child.gameObject);
        }
        grid = null;
    }
}



