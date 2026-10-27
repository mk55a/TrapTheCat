namespace TrapTheCat.Core
{
    public sealed class CatEscapeRouteCounter
    {
        private const int NoEdge = -1;

        private readonly CatSearchBoard _board;
        private readonly int _sink;
        private readonly int[] _firstEdge;
        private readonly int[] _edgeTo;
        private readonly int[] _edgeNext;
        private readonly int[] _edgeCapacity;
        private readonly int[] _arrivedBy;
        private readonly int[] _queue;
        private int _edgeCount;

        public CatEscapeRouteCounter(CatSearchBoard board)
        {
            _board = board;
            int nodes = board.Count * 2 + 1;
            int maxEdges = board.Count * 16 + 16;
            _sink = board.Count * 2;
            _firstEdge = new int[nodes];
            _edgeTo = new int[maxEdges];
            _edgeNext = new int[maxEdges];
            _edgeCapacity = new int[maxEdges];
            _arrivedBy = new int[nodes];
            _queue = new int[nodes];
        }

        public int BlocksNeededToTrap(int cat, int countUpTo)
        {
            BuildFlowGraph(cat);
            int source = OutNode(cat);
            int routes = 0;
            while (routes < countUpTo && TryAddRoute(source))
                routes++;
            return routes;
        }

        private void BuildFlowGraph(int cat)
        {
            _edgeCount = 0;
            for (int i = 0; i < _firstEdge.Length; i++)
                _firstEdge[i] = NoEdge;

            for (int cell = 0; cell < _board.Count; cell++)
            {
                if (!IsOpen(cell))
                    continue;

                if (cell != cat)
                    AddEdge(InNode(cell), OutNode(cell));
                if (cell != cat && _board.IsExit(cell))
                {
                    AddEdge(OutNode(cell), _sink);
                    continue;
                }

                int[] neighbours = _board.Neighbours(cell);
                for (int i = 0; i < neighbours.Length; i++)
                {
                    int next = neighbours[i];
                    if (next != cat && IsOpen(next))
                        AddEdge(OutNode(cell), InNode(next));
                }
            }
        }

        private bool TryAddRoute(int source)
        {
            for (int i = 0; i < _arrivedBy.Length; i++)
                _arrivedBy[i] = NoEdge;

            int head = 0;
            int tail = 0;
            _queue[tail++] = source;
            bool reachedSink = false;
            while (head < tail && !reachedSink)
            {
                int node = _queue[head++];
                for (int edge = _firstEdge[node]; edge != NoEdge; edge = _edgeNext[edge])
                {
                    int next = _edgeTo[edge];
                    if (_edgeCapacity[edge] <= 0 || next == source || _arrivedBy[next] != NoEdge)
                        continue;

                    _arrivedBy[next] = edge;
                    if (next == _sink)
                    {
                        reachedSink = true;
                        break;
                    }
                    _queue[tail++] = next;
                }
            }

            if (!reachedSink)
                return false;

            for (int node = _sink; node != source; node = _edgeTo[_arrivedBy[node] ^ 1])
            {
                int edge = _arrivedBy[node];
                _edgeCapacity[edge]--;
                _edgeCapacity[edge ^ 1]++;
            }
            return true;
        }

        private void AddEdge(int from, int to)
        {
            AddOneWay(from, to, 1);
            AddOneWay(to, from, 0);
        }

        private void AddOneWay(int from, int to, int capacity)
        {
            _edgeTo[_edgeCount] = to;
            _edgeCapacity[_edgeCount] = capacity;
            _edgeNext[_edgeCount] = _firstEdge[from];
            _firstEdge[from] = _edgeCount;
            _edgeCount++;
        }

        private bool IsOpen(int cell)
        {
            return _board.IsActive(cell) && !_board.IsBlocked(cell);
        }

        private static int InNode(int cell)
        {
            return cell * 2;
        }

        private static int OutNode(int cell)
        {
            return cell * 2 + 1;
        }
    }
}
