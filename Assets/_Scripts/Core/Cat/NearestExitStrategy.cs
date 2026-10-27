namespace TrapTheCat.Core
{
    public sealed class NearestExitStrategy : CatMoveStrategy
    {
        private ExitCommitment _commitment = new ExitCommitment();

        public override int RememberedExit
        {
            get { return _commitment.Target; }
        }

        public override int ChooseMove(CatSearchContext search, int cat)
        {
            return _commitment.NextStep(search, cat);
        }

        public override CatMoveStrategy Clone()
        {
            return new NearestExitStrategy { _commitment = _commitment.Clone() };
        }
    }
}
