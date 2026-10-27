namespace TrapTheCat.Core
{
    public sealed class CautiousStrategy : CatMoveStrategy
    {
        private const int FreeAtBlocksNeeded = 3;
        private const int PressuredAtBlocksNeeded = 2;

        private ExitCommitment _commitment = new ExitCommitment();

        public override int RememberedExit
        {
            get { return _commitment.Target; }
        }

        public override int ChooseMove(CatSearchContext search, int cat)
        {
            int blocksNeeded = search.RouteCounter.BlocksNeededToTrap(cat, FreeAtBlocksNeeded);
            if (blocksNeeded >= FreeAtBlocksNeeded)
                return _commitment.NextStep(search, cat);

            _commitment.Forget();
            if (blocksNeeded == PressuredAtBlocksNeeded)
                return SafeMoveSelector.MostEscapeRoutes(search, search.Board.OpenNeighbours(cat));

            return _commitment.NextStep(search, cat);
        }

        public override CatMoveStrategy Clone()
        {
            return new CautiousStrategy { _commitment = _commitment.Clone() };
        }
    }
}
