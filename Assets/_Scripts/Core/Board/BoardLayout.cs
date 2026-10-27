using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    public sealed class BoardLayout
    {
        private static readonly Vector2Int[] NoNeighbours = new Vector2Int[0];

        private readonly bool[] _active;
        private readonly bool[] _exit;
        private readonly Vector2Int[][] _neighbours;
        private readonly List<Vector2Int> _activeCells = new List<Vector2Int>();

        public int Width { get; private set; }
        public int Height { get; private set; }
        public BoardShape Shape { get; private set; }

        public IReadOnlyList<Vector2Int> ActiveCells
        {
            get { return _activeCells; }
        }

        public BoardLayout(int width, int height, BoardShape shape)
        {
            Width = width;
            Height = height;
            Shape = shape;
            int cellCount = width * height;
            _active = new bool[cellCount];
            _exit = new bool[cellCount];
            _neighbours = new Vector2Int[cellCount][];

            bool[,] mask = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    mask[x, y] = shape.IsCellActive(x, y, width, height);
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int index = Index(x, y);
                    if (!mask[x, y])
                    {
                        _neighbours[index] = NoNeighbours;
                        continue;
                    }

                    List<Vector2Int> neighbours = FindActiveNeighbours(mask, x, y);
                    _active[index] = true;
                    _neighbours[index] = neighbours.ToArray();
                    _exit[index] = neighbours.Count < 6;
                    _activeCells.Add(new Vector2Int(x, y));
                }
            }
        }

        public bool Contains(Vector2Int cell)
        {
            return IsInBounds(cell) && _active[Index(cell.x, cell.y)];
        }

        public bool IsExit(Vector2Int cell)
        {
            return IsInBounds(cell) && _exit[Index(cell.x, cell.y)];
        }

        public IReadOnlyList<Vector2Int> Neighbours(Vector2Int cell)
        {
            return IsInBounds(cell) ? _neighbours[Index(cell.x, cell.y)] : NoNeighbours;
        }

        public bool[,] CreateActiveMask()
        {
            bool[,] mask = new bool[Width, Height];
            foreach (Vector2Int cell in _activeCells)
                mask[cell.x, cell.y] = true;
            return mask;
        }

        public static List<Vector2Int> FindActiveNeighbours(bool[,] activeCells, int x, int y)
        {
            List<Vector2Int> neighbours = new List<Vector2Int>(6);
            bool oddRow = (y & 1) == 1;
            AddIfActive(neighbours, activeCells, x - 1, y);
            AddIfActive(neighbours, activeCells, x + 1, y);
            AddIfActive(neighbours, activeCells, x + (oddRow ? 0 : -1), y - 1);
            AddIfActive(neighbours, activeCells, x + (oddRow ? 1 : 0), y - 1);
            AddIfActive(neighbours, activeCells, x + (oddRow ? 0 : -1), y + 1);
            AddIfActive(neighbours, activeCells, x + (oddRow ? 1 : 0), y + 1);
            return neighbours;
        }

        private int Index(int x, int y)
        {
            return y * Width + x;
        }

        private bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;
        }

        private static void AddIfActive(List<Vector2Int> neighbours, bool[,] activeCells, int x, int y)
        {
            if (x >= 0 && y >= 0 &&
                x < activeCells.GetLength(0) &&
                y < activeCells.GetLength(1) &&
                activeCells[x, y])
                neighbours.Add(new Vector2Int(x, y));
        }
    }
}
