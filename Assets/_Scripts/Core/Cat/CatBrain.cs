namespace TrapTheCat.Core
{
    public sealed class CatBrain
    {
        private readonly CatMoveStrategy _strategy;
        private CatSearchContext _search;

        public int Target
        {
            get { return _strategy.RememberedExit; }
        }

        public CatBrain(CatDifficulty difficulty)
            : this(StrategyFor(difficulty))
        {
        }

        private CatBrain(CatMoveStrategy strategy)
        {
            _strategy = strategy;
        }

        public CatBrain Clone()
        {
            return new CatBrain(_strategy.Clone()) { _search = _search };
        }

        public int ChooseMove(CatSearchBoard board, int cat)
        {
            if (_search == null || _search.Board != board)
                _search = new CatSearchContext(board);

            int adjacentExit = AdjacentOpenExit(board, cat);
            if (adjacentExit >= 0)
                return adjacentExit;

            return _strategy.ChooseMove(_search, cat);
        }

        private static int AdjacentOpenExit(CatSearchBoard board, int cat)
        {
            foreach (int neighbour in board.OpenNeighbours(cat))
            {
                if (board.IsExit(neighbour))
                    return neighbour;
            }
            return CatEscapeRouteFinder.NoCell;
        }

        private static CatMoveStrategy StrategyFor(CatDifficulty difficulty)
        {
            switch (difficulty)
            {
                case CatDifficulty.Easy:
                    return new NearestExitStrategy();
                case CatDifficulty.Hard:
                    return new EscapeRaceStrategy();
                default:
                    return new CautiousStrategy();
            }
        }
    }
}
