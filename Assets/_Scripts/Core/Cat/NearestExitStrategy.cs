namespace TrapTheCat.Core
{
    public sealed class NearestExitStrategy : CatMoveStrategy
    {
        private int _target = CatEscapeRouteFinder.NoCell;

        public override int RememberedExit
        {
            get { return _target; }
        }

        public override int ChooseMove(CatSearchContext search, int cat)
        {
            CatEscapeRouteFinder routes = search.Routes;
            if (_target >= 0)
            {
                routes.SearchFrom(cat, _target);
                int step = routes.FirstStepTowards(cat, _target);
                if (step >= 0 && !search.Board.IsBlocked(step))
                    return step;
            }

            _target = routes.NearestExit(cat);
            if (_target < 0)
                return CatEscapeRouteFinder.NoCell;

            return routes.FirstStepTowards(cat, _target);
        }

        public override CatMoveStrategy Clone()
        {
            return new NearestExitStrategy { _target = _target };
        }
    }
}
