public class GridMapObject
{
    private Grid<GridMapObject> _grid;
    private int x, y;
    private GridType _type;

    public GridMapObject(Grid<GridMapObject> grid, int x, int y)
    {
        this._grid = grid;
        this.x = x;
        this.y = y;
    }

    public void SetType(GridType type)
    {
        this._type = type;
    }
    public GridType GetType()
    {
        return this._type;
    }
    public override string ToString()
    {
        return _type.ToString();
    }
    public bool isWalkable
    {
        get { return _type != GridType.Full; } // Example: Boundaries are not walkable
    }
}

