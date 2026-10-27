using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    public sealed class BoardState
    {
        private readonly bool[,] _blocked;
        private readonly List<Vector2Int> _blockedCells = new List<Vector2Int>();

        public event Action<Vector2Int> BlockPlaced;
        public event Action<Vector2Int> CatMoved;

        public BoardLayout Layout { get; private set; }
        public bool HasCat { get; private set; }
        public Vector2Int CatCell { get; private set; }

        public IReadOnlyList<Vector2Int> BlockedCells
        {
            get { return _blockedCells; }
        }

        public BoardState(BoardLayout layout)
        {
            Layout = layout;
            _blocked = new bool[layout.Width, layout.Height];
        }

        public static BoardState FromLevel(LevelData level)
        {
            BoardState board = new BoardState(new BoardLayout(level.width, level.height, level.boardShape));
            foreach (Vector2Int cell in level.blockedCells)
                board.PlaceBlock(cell);
            if (level.hasCatStart)
                board.MoveCat(level.catStart);
            return board;
        }

        public bool IsBlocked(Vector2Int cell)
        {
            return Layout.Contains(cell) && _blocked[cell.x, cell.y];
        }

        public bool IsCat(Vector2Int cell)
        {
            return HasCat && CatCell == cell;
        }

        public bool CanPlaceBlock(Vector2Int cell)
        {
            return Layout.Contains(cell) && !_blocked[cell.x, cell.y] && !IsCat(cell);
        }

        public bool PlaceBlock(Vector2Int cell)
        {
            if (!CanPlaceBlock(cell))
                return false;

            _blocked[cell.x, cell.y] = true;
            _blockedCells.Add(cell);
            BlockPlaced?.Invoke(cell);
            return true;
        }

        public bool MoveCat(Vector2Int cell)
        {
            if (!Layout.Contains(cell) || _blocked[cell.x, cell.y])
                return false;

            HasCat = true;
            CatCell = cell;
            CatMoved?.Invoke(cell);
            return true;
        }
    }
}
