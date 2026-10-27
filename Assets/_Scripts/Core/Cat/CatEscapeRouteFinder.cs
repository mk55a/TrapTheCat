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

        public void SearchFrom(int from)
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
                    if (_distance[next] >= 0 || _board.IsBlocked(next))
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

        public bool IsReachableExit(int cell)
        {
            return _board.IsExit(cell) && _distance[cell] > 0;
        }

        public int NearestExit(int from)
        {
            SearchFrom(from);
            int best = NoCell;
            for (int i = 0; i < _board.Count; i++)
            {
                if (IsReachableExit(i) && (best < 0 || _distance[i] < _distance[best]))
                    best = i;
            }
            return best;
        }

        public int NearestExitDistance(int from)
        {
            SearchFrom(from);
            int shortest = -1;
            for (int i = 0; i < _board.Count; i++)
            {
                if (IsReachableExit(i) && (shortest < 0 || _distance[i] < shortest))
                    shortest = _distance[i];
            }
            return shortest;
        }

        public int StepTowards(int from, int exit)
        {
            SearchFrom(exit);
            int best = NoCell;
            int[] neighbours = _board.Neighbours(from);
            for (int i = 0; i < neighbours.Length; i++)
            {
                int step = neighbours[i];
                if (_board.IsBlocked(step) || _distance[step] < 0)
                    continue;

                if (best < 0 || IsBetterStep(step, best))
                    best = step;
            }
            return best;
        }

        public List<int> RouteToNearestExit(int from)
        {
            return RouteTo(from, NearestExit(from));
        }

        public List<int> RouteTo(int from, int exit)
        {
            List<int> route = new List<int>();
            if (exit < 0 || _board.IsBlocked(exit))
                return route;

            SearchFrom(from);
            if (_distance[exit] < 0)
                return route;

            for (int current = exit; current >= 0; current = _previous[current])
                route.Add(current);
            route.Reverse();
            return route;
        }

        private bool IsBetterStep(int step, int best)
        {
            if (_distance[step] != _distance[best])
                return _distance[step] < _distance[best];
            return _board.CountOpenNeighbours(step) > _board.CountOpenNeighbours(best);
        }
    }
}
