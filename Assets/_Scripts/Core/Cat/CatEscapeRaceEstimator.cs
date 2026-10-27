namespace TrapTheCat.Core
{
    public sealed class CatEscapeRaceEstimator
    {
        public const int CannotOutrunBlocks = int.MaxValue;

        private const int RoutesThePlayerCannotBlockAtOnce = 2;

        private readonly CatSearchBoard _board;
        private readonly int[] _movesToEscape;
        private readonly int[] _settledNeighbours;
        private readonly int[] _queue;

        public CatEscapeRaceEstimator(CatSearchBoard board)
        {
            _board = board;
            _movesToEscape = new int[board.Count];
            _settledNeighbours = new int[board.Count];
            _queue = new int[board.Count];
        }

        public void Estimate()
        {
            int head = 0;
            int tail = 0;
            for (int cell = 0; cell < _board.Count; cell++)
            {
                _settledNeighbours[cell] = 0;
                _movesToEscape[cell] = CannotOutrunBlocks;
                if (IsOpen(cell) && _board.IsExit(cell))
                {
                    _movesToEscape[cell] = 0;
                    _queue[tail++] = cell;
                }
            }

            while (head < tail)
            {
                int settled = _queue[head++];
                int[] neighbours = _board.Neighbours(settled);
                for (int i = 0; i < neighbours.Length; i++)
                {
                    int next = neighbours[i];
                    if (!IsOpen(next) || _movesToEscape[next] != CannotOutrunBlocks)
                        continue;

                    _settledNeighbours[next]++;
                    if (_settledNeighbours[next] < RoutesThePlayerCannotBlockAtOnce)
                        continue;

                    _movesToEscape[next] = _movesToEscape[settled] + 1;
                    _queue[tail++] = next;
                }
            }
        }

        public int MovesToEscape(int cell)
        {
            return _movesToEscape[cell];
        }

        private bool IsOpen(int cell)
        {
            return _board.IsActive(cell) && !_board.IsBlocked(cell);
        }
    }
}
