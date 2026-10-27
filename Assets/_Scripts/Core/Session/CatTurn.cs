using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    public sealed class CatTurn
    {
        private static readonly Vector2Int[] NoRoute = new Vector2Int[0];

        public bool Trapped { get; private set; }
        public Vector2Int To { get; private set; }
        public bool Escaped { get; private set; }
        public IReadOnlyList<Vector2Int> Route { get; private set; }

        private CatTurn()
        {
        }

        public static CatTurn WasTrapped()
        {
            return new CatTurn { Trapped = true, Route = NoRoute };
        }

        public static CatTurn MovedTo(Vector2Int to, bool escaped, IReadOnlyList<Vector2Int> route)
        {
            return new CatTurn { To = to, Escaped = escaped, Route = route };
        }
    }
}
