namespace TrapTheCat.Core
{
    public sealed class CautiousStrategy : CatMoveStrategy
    {
        private const int CostPerStep = 10;
        private const int BlockNextToExitCost = 6;
        private const int BlockNearExitCost = 2;
        private const int BlockAlongRouteCost = 2;

        public override int ChooseMove(CatSearchContext search, int cat)
        {
            CatSearchBoard board = search.Board;
            CatEscapeRouteFinder routes = search.Routes;
            routes.SearchFrom(cat);
            int best = CatEscapeRouteFinder.NoCell;
            int bestCost = int.MaxValue;
            for (int exit = 0; exit < board.Count; exit++)
            {
                if (!routes.IsReachableExit(exit))
                    continue;

                int cost = routes.Distance(exit) * CostPerStep +
                           ExitCrowding(board, exit) +
                           RouteCrowding(board, routes, cat, exit) * BlockAlongRouteCost;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = exit;
                }
            }

            return routes.FirstStepTowards(cat, best);
        }

        private static int ExitCrowding(CatSearchBoard board, int exit)
        {
            int crowding = board.CountBlockedNeighbours(exit) * BlockNextToExitCost;
            int[] neighbours = board.Neighbours(exit);
            for (int i = 0; i < neighbours.Length; i++)
                crowding += board.CountBlockedNeighbours(neighbours[i]) * BlockNearExitCost;
            return crowding;
        }

        private static int RouteCrowding(CatSearchBoard board, CatEscapeRouteFinder routes, int from, int exit)
        {
            int crowding = 0;
            int current = exit;
            while (current >= 0 && current != from)
            {
                crowding += board.CountBlockedNeighbours(current);
                current = routes.PreviousOnRoute(current);
            }
            return crowding;
        }
    }
}
