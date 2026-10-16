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
        if (_type == type)
            return;

        this._type = type;
        if (_grid != null)
            _grid.TriggerGridObjectChanged(x, y);
    }
    public GridType GetCellType()
    {
        return this._type;
    }
    public override string ToString()
    {
        return _type.ToString();
    }
    public bool isWalkable
    {
        get { return _type != GridType.Full && _type != GridType.Obstacle; }
    }
}
