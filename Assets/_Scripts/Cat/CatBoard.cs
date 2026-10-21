using System;
using System.Collections.Generic;
using UnityEngine;

// Flat, allocation-light snapshot of the board used by the cat AI and the level solver.
// BFS results live in shared scratch buffers and are only valid until the next BFS call.
public sealed class CatBoard
{
    private readonly bool[] _active;
    private readonly bool[] _perimeter;
    private readonly bool[] _blocked;
    private readonly int[][] _neighbors;
    private readonly int[] _dist;
    private readonly int[] _parent;
    private readonly int[] _queue;
    private readonly int[] _catDist;
    private readonly int[] _exitDist;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Count { get; private set; }

    public CatBoard(bool[,] active, IEnumerable<Vector2Int> blockedCells)
    {
        Width = active.GetLength(0);
        Height = active.GetLength(1);
        Count = Width * Height;
        _active = new bool[Count];
        _perimeter = new bool[Count];
        _blocked = new bool[Count];
        _neighbors = new int[Count][];
        _dist = new int[Count];
        _parent = new int[Count];
        _queue = new int[Count];
        _catDist = new int[Count];
        _exitDist = new int[Count];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
                _active[Index(x, y)] = active[x, y];
        }

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int index = Index(x, y);
                if (!_active[index])
                {
                    _neighbors[index] = new int[0];
                    continue;
                }

                List<Vector2Int> neighbors = BoardTopology.GetNeighbors(active, x, y);
                _neighbors[index] = new int[neighbors.Count];
                for (int i = 0; i < neighbors.Count; i++)
                    _neighbors[index][i] = Index(neighbors[i].x, neighbors[i].y);
                _perimeter[index] = neighbors.Count < 6;
            }
        }

        if (blockedCells != null)
        {
            foreach (Vector2Int cell in blockedCells)
                _blocked[Index(cell.x, cell.y)] = true;
        }
    }

    public int Index(int x, int y)
    {
        return y * Width + x;
    }

    public Vector2Int Cell(int index)
    {
        return new Vector2Int(index % Width, index / Width);
    }

    public bool IsPerimeter(int index)
    {
        return _perimeter[index];
    }

    public bool IsBlocked(int index)
    {
        return _blocked[index];
    }

    public bool IsActive(int index)
    {
        return _active[index];
    }

    public int[] Neighbors(int index)
    {
        return _neighbors[index];
    }

    public void SetBlocked(int index, bool blocked)
    {
        _blocked[index] = blocked;
    }

    public int Distance(int index)
    {
        return _dist[index];
    }

    // Walks outward from the cat. Perimeter cells are exits, so the search never continues past them.
    // A blocked cell may be made passable to model a cat that commits to an exit that later closes.
    public void RunBfs(int from, int passableBlocked)
    {
        for (int i = 0; i < Count; i++)
        {
            _dist[i] = -1;
            _parent[i] = -1;
        }

        int head = 0;
        int tail = 0;
        _queue[tail++] = from;
        _dist[from] = 0;
        while (head < tail)
        {
            int current = _queue[head++];
            if (current != from && _perimeter[current])
                continue;

            int[] neighbors = _neighbors[current];
            for (int i = 0; i < neighbors.Length; i++)
            {
                int next = neighbors[i];
                if (_dist[next] >= 0 || (_blocked[next] && next != passableBlocked))
                    continue;

                _dist[next] = _dist[current] + 1;
                _parent[next] = current;
                _queue[tail++] = next;
            }
        }
    }

    // First cell on the last BFS route from the cat to the target, or -1 when unreachable.
    public int FirstStepOnRoute(int from, int target)
    {
        if (target < 0 || _dist[target] <= 0)
            return -1;

        int current = target;
        while (_parent[current] != from)
        {
            current = _parent[current];
            if (current < 0)
                return -1;
        }
        return current;
    }

    // Nearest open exit by BFS distance; ties resolve to the lowest cell index.
    public int NearestExit(int from, int passableBlocked)
    {
        RunBfs(from, passableBlocked);
        int best = -1;
        for (int i = 0; i < Count; i++)
        {
            if (_perimeter[i] && _dist[i] > 0 && (best < 0 || _dist[i] < _dist[best]))
                best = i;
        }
        return best;
    }

    // Shortest distance to an open exit and how many exits sit within the given slack of it.
    public bool TryMeasureExits(int from, int slack, out int shortest, out int nearCount)
    {
        RunBfs(from, -1);
        shortest = int.MaxValue;
        nearCount = 0;
        for (int i = 0; i < Count; i++)
        {
            if (_perimeter[i] && _dist[i] > 0 && _dist[i] < shortest)
                shortest = _dist[i];
        }

        if (shortest == int.MaxValue)
            return false;

        for (int i = 0; i < Count; i++)
        {
            if (_perimeter[i] && _dist[i] > 0 && _dist[i] <= shortest + slack)
                nearCount++;
        }
        return true;
    }

    public bool HasExit(int from)
    {
        return TryMeasureExits(from, 0, out int shortest, out int nearCount);
    }

    public int CountBlockedNeighbors(int index)
    {
        int count = 0;
        int[] neighbors = _neighbors[index];
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (_blocked[neighbors[i]])
                count++;
        }
        return count;
    }

    // Blocked cells around an exit, with the outer ring counting less. Used to spot exits the player is closing.
    public int GetExitClutter(int index)
    {
        int clutter = CountBlockedNeighbors(index) * 6;
        int[] neighbors = _neighbors[index];
        for (int i = 0; i < neighbors.Length; i++)
            clutter += CountBlockedNeighbors(neighbors[i]) * 2;
        return clutter;
    }

    // Blocked-neighbour load along the last BFS route to the target.
    public int GetRouteClutter(int from, int target)
    {
        int clutter = 0;
        int current = target;
        while (current >= 0 && current != from)
        {
            clutter += CountBlockedNeighbors(current);
            current = _parent[current];
        }
        return clutter;
    }

    public List<int> GetLegalMoves(int from)
    {
        List<int> moves = new List<int>(6);
        int[] neighbors = _neighbors[from];
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (!_blocked[neighbors[i]])
                moves.Add(neighbors[i]);
        }
        return moves;
    }

    // Cells worth blocking this turn, most promising first: cells on the cat's shortest escape routes,
    // then cells close to the cat. This bounds the player branching factor for lookahead and solving.
    public List<int> GetCandidateBlocks(int cat, int max)
    {
        List<int> result = new List<int>();
        RunBfs(cat, -1);
        Array.Copy(_dist, _catDist, Count);

        int head = 0;
        int tail = 0;
        for (int i = 0; i < Count; i++)
        {
            _exitDist[i] = -1;
            if (_perimeter[i] && _catDist[i] > 0)
            {
                _exitDist[i] = 0;
                _queue[tail++] = i;
            }
        }

        while (head < tail)
        {
            int current = _queue[head++];
            int[] neighbors = _neighbors[current];
            for (int i = 0; i < neighbors.Length; i++)
            {
                int next = neighbors[i];
                if (_exitDist[next] >= 0 || _blocked[next] || _perimeter[next])
                    continue;

                _exitDist[next] = _exitDist[current] + 1;
                _queue[tail++] = next;
            }
        }

        int shortest = _exitDist[cat];
        if (shortest <= 0)
            return result;

        List<int> keys = new List<int>();
        for (int i = 0; i < Count; i++)
        {
            if (i == cat || _blocked[i] || _catDist[i] < 0)
                continue;

            int slack = _exitDist[i] < 0 ? 99 : _catDist[i] + _exitDist[i] - shortest;
            if (slack > 1 && _catDist[i] > 2)
                continue;

            int tier = slack == 0 ? 0 : (slack == 1 ? 1 : 2);
            keys.Add(((tier * 16 + Mathf.Min(_catDist[i], 15)) << 12) | i);
        }

        keys.Sort();
        for (int i = 0; i < keys.Count && result.Count < max; i++)
            result.Add(keys[i] & 4095);
        return result;
    }

    // Cells from the given start (inclusive) to its nearest exit, for drawing the cat's intended route.
    public List<int> BuildRouteToNearestExit(int from)
    {
        List<int> route = new List<int>();
        int exit = NearestExit(from, -1);
        if (exit < 0)
            return route;

        for (int current = exit; current >= 0; current = _parent[current])
            route.Add(current);
        route.Reverse();
        return route;
    }
}
