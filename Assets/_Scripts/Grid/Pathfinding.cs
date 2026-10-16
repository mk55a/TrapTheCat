using System.Collections.Generic;
using UnityEngine;

public class Pathfinding
{
    private const int MOVE_COST = 10;

    public Grid<PathNode> _grid;
    private readonly GridManager _gridManager;
    private List<PathNode> _openList;
    private List<PathNode> _closedList;

    public static Pathfinding Instance { get; private set; }

    public Pathfinding(int width, int height)
    {
        _gridManager = GridManager.Instance;
        if (_gridManager == null || _gridManager.originObject == null)
        {
            Debug.LogError("Pathfinding requires an initialized GridManager.");
            return;
        }

        _grid = new Grid<PathNode>(
            width,
            height,
            _gridManager.gridCellSize,
            _gridManager.originObject.transform.position,
            (g, x, y) => new PathNode(g, x, y),
            false);
        Instance = this;
    }

    public Grid<PathNode> GetGrid()
    {
        return _grid;
    }

    public List<Vector3> FindPath(Vector3 startWorldPosition, Vector3 endWorldPosition)
    {
        if (_grid == null ||
            !_gridManager.TryGetGridCoordinates(startWorldPosition, out int startX, out int startY) ||
            !_gridManager.TryGetGridCoordinates(endWorldPosition, out int endX, out int endY))
            return null;

        List<PathNode> path = FindPath(startX, startY, endX, endY);
        if (path == null)
            return null;

        List<Vector3> vectorPath = new List<Vector3>();
        foreach (PathNode pathNode in path)
            vectorPath.Add(_gridManager.GetCenterGridPosition(pathNode.x, pathNode.y));
        return vectorPath;
    }

    public List<PathNode> FindPath(int startX, int startY, int endX, int endY)
    {
        if (_grid == null || !_gridManager.IsInside(startX, startY) ||
            !_gridManager.IsInside(endX, endY))
            return null;

        GridMapObject destination = _gridManager.grid.GetGridObject(endX, endY);
        if (destination == null || !destination.isWalkable)
            return null;

        PathNode startNode = _grid.GetGridObject(startX, startY);
        PathNode endNode = _grid.GetGridObject(endX, endY);
        _openList = new List<PathNode> { startNode };
        _closedList = new List<PathNode>();

        for (int x = 0; x < _grid.GetWidth(); x++)
        {
            for (int y = 0; y < _grid.GetHeight(); y++)
            {
                PathNode node = _grid.GetGridObject(x, y);
                node.gCost = int.MaxValue;
                node.hCost = 0;
                node.CalculateFCost();
                node.cameFromNode = null;
            }
        }

        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startNode, endNode);
        startNode.CalculateFCost();

        while (_openList.Count > 0)
        {
            PathNode currentNode = GetLowestFCostNode(_openList);
            if (currentNode == endNode)
                return CalculatedPath(endNode);

            _openList.Remove(currentNode);
            _closedList.Add(currentNode);

            foreach (PathNode neighbour in GetNeighbourList(currentNode))
            {
                if (_closedList.Contains(neighbour))
                    continue;

                GridMapObject cell = _gridManager.grid.GetGridObject(neighbour.x, neighbour.y);
                if (cell == null || !cell.isWalkable ||
                    (cell.GetCellType() == GridType.Boundary && neighbour != endNode))
                    continue;

                int tentativeGCost = currentNode.gCost + MOVE_COST;
                if (tentativeGCost >= neighbour.gCost)
                    continue;

                neighbour.cameFromNode = currentNode;
                neighbour.gCost = tentativeGCost;
                neighbour.hCost = CalculateDistanceCost(neighbour, endNode);
                neighbour.CalculateFCost();
                if (!_openList.Contains(neighbour))
                    _openList.Add(neighbour);
            }
        }

        return null;
    }

    public PathNode GetNode(int x, int y)
    {
        return _grid != null ? _grid.GetGridObject(x, y) : null;
    }

    private List<PathNode> GetNeighbourList(PathNode current)
    {
        List<PathNode> neighbours = new List<PathNode>(6);
        bool oddRow = (current.y & 1) == 1;

        AddNeighbourIfInside(neighbours, current.x - 1, current.y);
        AddNeighbourIfInside(neighbours, current.x + 1, current.y);
        AddNeighbourIfInside(neighbours, current.x + (oddRow ? 0 : -1), current.y - 1);
        AddNeighbourIfInside(neighbours, current.x + (oddRow ? 1 : 0), current.y - 1);
        AddNeighbourIfInside(neighbours, current.x + (oddRow ? 0 : -1), current.y + 1);
        AddNeighbourIfInside(neighbours, current.x + (oddRow ? 1 : 0), current.y + 1);
        return neighbours;
    }

    private void AddNeighbourIfInside(List<PathNode> neighbours, int x, int y)
    {
        if (_gridManager.IsInside(x, y))
            neighbours.Add(_grid.GetGridObject(x, y));
    }

    private List<PathNode> CalculatedPath(PathNode endNode)
    {
        List<PathNode> path = new List<PathNode> { endNode };
        PathNode current = endNode;
        while (current.cameFromNode != null)
        {
            current = current.cameFromNode;
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    private int CalculateDistanceCost(PathNode a, PathNode b)
    {
        int aAxialX = a.x - (a.y - (a.y & 1)) / 2;
        int bAxialX = b.x - (b.y - (b.y & 1)) / 2;
        int deltaX = aAxialX - bAxialX;
        int deltaY = a.y - b.y;
        return (Mathf.Abs(deltaX) + Mathf.Abs(deltaY) + Mathf.Abs(deltaX + deltaY)) / 2 * MOVE_COST;
    }

    private PathNode GetLowestFCostNode(List<PathNode> nodes)
    {
        PathNode lowest = nodes[0];
        for (int i = 1; i < nodes.Count; i++)
        {
            if (nodes[i].fCost < lowest.fCost)
                lowest = nodes[i];
        }
        return lowest;
    }
}
