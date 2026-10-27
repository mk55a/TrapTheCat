using System.Collections.Generic;

namespace TrapTheCat.Core
{
    public sealed class CatEscapeRouteFinder
    {
        public const int NoCell = -1;

        private readonly CatSearchBoard _board;
        private readonly int[] _distance;
        private readonly int[] _previous;
        private readonly int[] _queue;

        public CatSearchBoard Board
        {
            get { return _board; }
        }

        public CatEscapeRouteFinder(CatSearchBoard board)
        {
            _board = board;
            _distance = new int[board.Count];
            _previous = new int[board.Count];
            _queue = new int[board.Count];
        }

        public void SearchFrom(int from, int treatAsOpen = NoCell)
        {
            for (int i = 0; i < _board.Count; i++)
            {
                _distance[i] = -1;
                _previous[i] = NoCell;
            }

            int head = 0;
            int tail = 0;
            _queue[tail++] = from;
            _distance[from] = 0;
            while (head < tail)
            {
                int current = _queue[head++];
                if (current != from && _board.IsExit(current))
                    continue;

                int[] neighbours = _board.Neighbours(current);
                for (int i = 0; i < neighbours.Length; i++)
                {
                    int next = neighbours[i];
                    if (_distance[next] >= 0 || (_board.IsBlocked(next) && next != treatAsOpen))
                        continue;

                    _distance[next] = _distance[current] + 1;
                    _previous[next] = current;
                    _queue[tail++] = next;
                }
            }
        }

        public int Distance(int cell)
        {
            return _distance[cell];
        }

        public int PreviousOnRoute(int cell)
        {
            return _previous[cell];
        }

        public bool IsReachableExit(int cell)
        {
            return _board.IsExit(cell) && _distance[cell] > 0;
        }

        public int FirstStepTowards(int from, int target)
        {
            if (target < 0 || _distance[target] <= 0)
                return NoCell;

            int current = target;
            while (_previous[current] != from)
            {
                current = _previous[current];
                if (current < 0)
                    return NoCell;
            }
            return current;
        }

        public int NearestExit(int from, int treatAsOpen = NoCell)
        {
            SearchFrom(from, treatAsOpen);
            int best = NoCell;
            for (int i = 0; i < _board.Count; i++)
            {
                if (IsReachableExit(i) && (best < 0 || _distance[i] < _distance[best]))
                    best = i;
            }
            return best;
        }

        public bool TryMeasureExits(int from, int slack, out int shortest, out int nearbyExits)
        {
            SearchFrom(from);
            shortest = int.MaxValue;
            nearbyExits = 0;
            for (int i = 0; i < _board.Count; i++)
            {
                if (IsReachableExit(i) && _distance[i] < shortest)
                    shortest = _distance[i];
            }

            if (shortest == int.MaxValue)
                return false;

            for (int i = 0; i < _board.Count; i++)
            {
                if (IsReachableExit(i) && _distance[i] <= shortest + slack)
                    nearbyExits++;
            }
            return true;
        }

        public bool CanReachExit(int from)
        {
            return TryMeasureExits(from, 0, out int shortest, out int nearbyExits);
        }

        public List<int> RouteToNearestExit(int from)
        {
            List<int> route = new List<int>();
            int exit = NearestExit(from);
            if (exit < 0)
                return route;

            for (int current = exit; current >= 0; current = _previous[current])
                route.Add(current);
            route.Reverse();
            return route;
        }
    }
}
