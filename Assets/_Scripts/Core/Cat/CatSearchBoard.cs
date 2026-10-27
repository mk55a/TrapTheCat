using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    public sealed class CatSearchBoard
    {
        private readonly bool[] _active;
        private readonly bool[] _exit;
        private readonly bool[] _blocked;
        private readonly int[][] _neighbours;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Count { get; private set; }

        public CatSearchBoard(bool[,] active, IEnumerable<Vector2Int> blockedCells)
        {
            Width = active.GetLength(0);
            Height = active.GetLength(1);
            Count = Width * Height;
            _active = new bool[Count];
            _exit = new bool[Count];
            _blocked = new bool[Count];
            _neighbours = new int[Count][];

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
                        _neighbours[index] = new int[0];
                        continue;
                    }

                    List<Vector2Int> neighbours = BoardLayout.FindActiveNeighbours(active, x, y);
                    _neighbours[index] = new int[neighbours.Count];
                    for (int i = 0; i < neighbours.Count; i++)
                        _neighbours[index][i] = Index(neighbours[i].x, neighbours[i].y);
                    _exit[index] = neighbours.Count < 6;
                }
            }

            if (blockedCells != null)
            {
                foreach (Vector2Int cell in blockedCells)
                    _blocked[Index(cell.x, cell.y)] = true;
            }
        }

        public static CatSearchBoard FromBoard(BoardState board)
        {
            return new CatSearchBoard(board.Layout.CreateActiveMask(), board.BlockedCells);
        }

        public int Index(int x, int y)
        {
            return y * Width + x;
        }

        public int Index(Vector2Int cell)
        {
            return Index(cell.x, cell.y);
        }

        public Vector2Int Cell(int index)
        {
            return new Vector2Int(index % Width, index / Width);
        }

        public bool IsActive(int index)
        {
            return _active[index];
        }

        public bool IsExit(int index)
        {
            return _exit[index];
        }

        public bool IsBlocked(int index)
        {
            return _blocked[index];
        }

        public void SetBlocked(int index, bool blocked)
        {
            _blocked[index] = blocked;
        }

        public int[] Neighbours(int index)
        {
            return _neighbours[index];
        }

        public int CountBlockedNeighbours(int index)
        {
            int count = 0;
            int[] neighbours = _neighbours[index];
            for (int i = 0; i < neighbours.Length; i++)
            {
                if (_blocked[neighbours[i]])
                    count++;
            }
            return count;
        }

        public int CountOpenNeighbours(int index)
        {
            return _neighbours[index].Length - CountBlockedNeighbours(index);
        }

        public List<int> OpenNeighbours(int index)
        {
            List<int> open = new List<int>(6);
            int[] neighbours = _neighbours[index];
            for (int i = 0; i < neighbours.Length; i++)
            {
                if (!_blocked[neighbours[i]])
                    open.Add(neighbours[i]);
            }
            return open;
        }
    }
}
