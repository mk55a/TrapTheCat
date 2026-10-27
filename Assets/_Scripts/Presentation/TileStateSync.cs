using System;
using TrapTheCat.Core;
using UnityEngine;

namespace TrapTheCat.Presentation
{
    public sealed class TileStateSync : IDisposable
    {
        private readonly BoardState _board;
        private readonly TileView[,] _tiles;

        public TileStateSync(BoardState board, TileView[,] tiles)
        {
            _board = board;
            _tiles = tiles;
            foreach (Vector2Int cell in board.Layout.ActiveCells)
                ShowCell(cell);
            _board.BlockPlaced += ShowCell;
        }

        public void Dispose()
        {
            _board.BlockPlaced -= ShowCell;
        }

        private void ShowCell(Vector2Int cell)
        {
            TileView tile = _tiles[cell.x, cell.y];
            if (tile != null)
                tile.SetBlocked(_board.IsBlocked(cell));
        }
    }
}
