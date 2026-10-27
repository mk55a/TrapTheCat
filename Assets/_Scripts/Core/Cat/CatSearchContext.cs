namespace TrapTheCat.Core
{
    public sealed class CatSearchContext
    {
        public CatSearchBoard Board { get; private set; }
        public CatEscapeRouteFinder Routes { get; private set; }
        public CatEscapeRouteCounter RouteCounter { get; private set; }
        public CatEscapeRaceEstimator EscapeRace { get; private set; }

        public CatSearchContext(CatSearchBoard board)
        {
            Board = board;
            Routes = new CatEscapeRouteFinder(board);
            RouteCounter = new CatEscapeRouteCounter(board);
            EscapeRace = new CatEscapeRaceEstimator(board);
        }
    }
}
