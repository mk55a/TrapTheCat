using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    public sealed class PuzzleSolution
    {
        public bool Winnable;
        public bool Inconclusive;
        public int MinPlayerMoves;
        public int WinningFirstMoves;
        public int FirstMoveOptions;
        public List<Vector2Int> WinningLine = new List<Vector2Int>();
    }
}
