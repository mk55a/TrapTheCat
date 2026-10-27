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

            if (!_search.Routes.CanReachExit(cat))
                return CatEscapeRouteFinder.NoCell;

            return _strategy.ChooseMove(_search, cat);
        }

        private static CatMoveStrategy StrategyFor(CatDifficulty difficulty)
        {
            switch (difficulty)
            {
                case CatDifficulty.Easy:
                    return new NearestExitStrategy();
                case CatDifficulty.Hard:
                    return new LookaheadStrategy();
                default:
                    return new CautiousStrategy();
            }
        }
    }
}
