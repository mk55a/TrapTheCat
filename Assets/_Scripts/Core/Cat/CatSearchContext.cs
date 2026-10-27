namespace TrapTheCat.Core
{
    public sealed class CatSearchContext
    {
        public CatSearchBoard Board { get; private set; }
        public CatEscapeRouteFinder Routes { get; private set; }
        public PlayerBlockPredictor BlockPredictor { get; private set; }

        public CatSearchContext(CatSearchBoard board)
        {
            Board = board;
            Routes = new CatEscapeRouteFinder(board);
            BlockPredictor = new PlayerBlockPredictor(Routes);
        }
    }
}
