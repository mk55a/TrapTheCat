namespace TrapTheCat.Core
{
    public abstract class CatMoveStrategy
    {
        public virtual int RememberedExit
        {
            get { return CatEscapeRouteFinder.NoCell; }
        }

        public abstract int ChooseMove(CatSearchContext search, int cat);

        public virtual CatMoveStrategy Clone()
        {
            return this;
        }
    }
}
