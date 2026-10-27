namespace TrapTheCat.Core
{
    public sealed class ExitCommitment
    {
        private const int StepsShorterToSwitch = 2;

        public int Target { get; private set; } = CatEscapeRouteFinder.NoCell;

        public int NextStep(CatSearchContext search, int cat)
        {
            CatEscapeRouteFinder routes = search.Routes;
            routes.SearchFrom(cat);
            int nearest = RoomiestNearestExit(search.Board, routes);
            if (nearest < 0)
            {
                Target = CatEscapeRouteFinder.NoCell;
                return CatEscapeRouteFinder.NoCell;
            }

            if (!IsStillWorthFollowing(routes, nearest))
                Target = nearest;

            return routes.StepTowards(cat, Target);
        }

        public void Forget()
        {
            Target = CatEscapeRouteFinder.NoCell;
        }

        public ExitCommitment Clone()
        {
            return new ExitCommitment { Target = Target };
        }

        private bool IsStillWorthFollowing(CatEscapeRouteFinder routes, int nearest)
        {
            if (Target < 0 || !routes.IsReachableExit(Target))
                return false;

            return routes.Distance(Target) < routes.Distance(nearest) + StepsShorterToSwitch;
        }

        private static int RoomiestNearestExit(CatSearchBoard board, CatEscapeRouteFinder routes)
        {
            int best = CatEscapeRouteFinder.NoCell;
            for (int exit = 0; exit < board.Count; exit++)
            {
                if (!routes.IsReachableExit(exit))
                    continue;

                if (best < 0 ||
                    routes.Distance(exit) < routes.Distance(best) ||
                    (routes.Distance(exit) == routes.Distance(best) &&
                     board.CountOpenNeighbours(exit) > board.CountOpenNeighbours(best)))
                    best = exit;
            }
            return best;
        }
    }
}
