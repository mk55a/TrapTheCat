using System.Collections.Generic;

namespace TrapTheCat.Core
{
    public sealed class EscapeRaceStrategy : CatMoveStrategy
    {
        public override int ChooseMove(CatSearchContext search, int cat)
        {
            CatEscapeRaceEstimator race = search.EscapeRace;
            race.Estimate();

            List<int> moves = search.Board.OpenNeighbours(cat);
            int fastest = CatEscapeRaceEstimator.CannotOutrunBlocks;
            foreach (int move in moves)
            {
                if (race.MovesToEscape(move) < fastest)
                    fastest = race.MovesToEscape(move);
            }

            List<int> fastestMoves = new List<int>(moves.Count);
            foreach (int move in moves)
            {
                if (race.MovesToEscape(move) == fastest)
                    fastestMoves.Add(move);
            }

            return SafeMoveSelector.MostEscapeRoutes(search, fastestMoves);
        }
    }
}
