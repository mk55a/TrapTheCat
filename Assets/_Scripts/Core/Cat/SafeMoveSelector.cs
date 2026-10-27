using System.Collections.Generic;

namespace TrapTheCat.Core
{
    public static class SafeMoveSelector
    {
        private const int RoutesWorthCounting = 3;

        public static int MostEscapeRoutes(CatSearchContext search, IEnumerable<int> moves)
        {
            int best = CatEscapeRouteFinder.NoCell;
            int bestRoutes = -1;
            int bestDistance = int.MaxValue;
            int bestRoom = -1;
            foreach (int move in moves)
            {
                int distance = search.Routes.NearestExitDistance(move);
                if (distance < 0)
                    continue;

                int routes = search.RouteCounter.BlocksNeededToTrap(move, RoutesWorthCounting);
                int room = search.Board.CountOpenNeighbours(move);
                if (best >= 0 && !IsSafer(routes, distance, room, bestRoutes, bestDistance, bestRoom))
                    continue;

                best = move;
                bestRoutes = routes;
                bestDistance = distance;
                bestRoom = room;
            }
            return best;
        }

        private static bool IsSafer(int routes, int distance, int room, int bestRoutes, int bestDistance, int bestRoom)
        {
            if (routes != bestRoutes)
                return routes > bestRoutes;
            if (distance != bestDistance)
                return distance < bestDistance;
            return room > bestRoom;
        }
    }
}
