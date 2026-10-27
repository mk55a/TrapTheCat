using System.Collections.Generic;

namespace TrapTheCat.Core
{
    public sealed class LookaheadStrategy : CatMoveStrategy
    {
        private const int EscapeScore = 1000000;
        private const int TrappedScore = -1000000;
        private const int NearbyExitSlack = 2;

        private readonly int _plies;
        private readonly int _blocksConsidered;

        public LookaheadStrategy(int plies = 3, int blocksConsidered = 6)
        {
            _plies = plies;
            _blocksConsidered = blocksConsidered;
        }

        public override int ChooseMove(CatSearchContext search, int cat)
        {
            int bestMove = CatEscapeRouteFinder.NoCell;
            int bestScore = int.MinValue;
            foreach (int move in search.Board.OpenNeighbours(cat))
            {
                if (search.Board.IsExit(move))
                    return move;

                int score = Search(search, move, _plies - 1, true);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            return bestMove;
        }

        private int Search(CatSearchContext search, int cat, int plies, bool playerTurn)
        {
            if (plies <= 0)
                return Evaluate(search.Routes, cat);

            return playerTurn
                ? WorstScoreAfterPlayerBlock(search, cat, plies)
                : BestScoreAfterCatMove(search, cat, plies);
        }

        private int WorstScoreAfterPlayerBlock(CatSearchContext search, int cat, int plies)
        {
            List<int> blocks = search.BlockPredictor.LikelyBlocks(cat, _blocksConsidered);
            if (blocks.Count == 0)
                return Evaluate(search.Routes, cat);

            int worst = int.MaxValue;
            foreach (int cell in blocks)
            {
                search.Board.SetBlocked(cell, true);
                int score = Search(search, cat, plies - 1, false);
                search.Board.SetBlocked(cell, false);
                if (score < worst)
                    worst = score;
                if (worst <= TrappedScore)
                    break;
            }
            return worst;
        }

        private int BestScoreAfterCatMove(CatSearchContext search, int cat, int plies)
        {
            if (!search.Routes.CanReachExit(cat))
                return TrappedScore;

            int best = TrappedScore;
            foreach (int move in search.Board.OpenNeighbours(cat))
            {
                if (search.Board.IsExit(move))
                    return EscapeScore + plies;

                int score = Search(search, move, plies - 1, true);
                if (score > best)
                    best = score;
            }
            return best;
        }

        private static int Evaluate(CatEscapeRouteFinder routes, int cat)
        {
            if (!routes.TryMeasureExits(cat, NearbyExitSlack, out int shortest, out int nearbyExits))
                return TrappedScore;

            return 1000 - shortest * 20 + nearbyExits * 5;
        }
    }
}
