using System.Collections.Generic;

namespace TrapTheCat.Core
{
    public sealed class CatBrain
    {
        private const int EscapeScore = 1000000;
        private const int TrappedScore = -1000000;

        private readonly CatDifficulty _difficulty;
        private readonly int _hardPlies;
        private readonly int _hardBreadth;
        private int _target = -1;

        public int Target
        {
            get { return _target; }
        }

        public CatBrain(CatDifficulty difficulty, int hardPlies = 3, int hardBreadth = 6)
        {
            _difficulty = difficulty;
            _hardPlies = hardPlies;
            _hardBreadth = hardBreadth;
        }

        public CatBrain Clone()
        {
            CatBrain clone = new CatBrain(_difficulty, _hardPlies, _hardBreadth);
            clone._target = _target;
            return clone;
        }

        public void Reset()
        {
            _target = -1;
        }

        public int ChooseMove(CatBoard board, int cat)
        {
            if (!board.HasExit(cat))
                return -1;

            switch (_difficulty)
            {
                case CatDifficulty.Easy:
                    return ChooseEasy(board, cat);
                case CatDifficulty.Hard:
                    return ChooseHard(board, cat);
                default:
                    return ChooseNormal(board, cat);
            }
        }

        private int ChooseEasy(CatBoard board, int cat)
        {
            if (_target >= 0)
            {
                board.RunBfs(cat, _target);
                int step = board.FirstStepOnRoute(cat, _target);
                if (step >= 0 && !board.IsBlocked(step))
                    return step;
            }

            _target = board.NearestExit(cat, -1);
            if (_target < 0)
                return -1;

            return board.FirstStepOnRoute(cat, _target);
        }

        private int ChooseNormal(CatBoard board, int cat)
        {
            _target = -1;
            board.RunBfs(cat, -1);
            int best = -1;
            int bestCost = int.MaxValue;
            for (int i = 0; i < board.Count; i++)
            {
                if (!board.IsPerimeter(i) || board.Distance(i) <= 0)
                    continue;

                int cost = board.Distance(i) * 10 +
                           board.GetExitClutter(i) +
                           board.GetRouteClutter(cat, i) * 2;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = i;
                }
            }

            return board.FirstStepOnRoute(cat, best);
        }

        private int ChooseHard(CatBoard board, int cat)
        {
            _target = -1;
            List<int> moves = board.GetLegalMoves(cat);
            int bestMove = -1;
            int bestScore = int.MinValue;
            foreach (int move in moves)
            {
                if (board.IsPerimeter(move))
                    return move;

                int score = Search(board, move, _hardPlies - 1, true);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            return bestMove;
        }

        private int Search(CatBoard board, int cat, int plies, bool playerTurn)
        {
            if (plies <= 0)
                return Evaluate(board, cat);

            if (playerTurn)
            {
                List<int> candidates = board.GetCandidateBlocks(cat, _hardBreadth);
                if (candidates.Count == 0)
                    return Evaluate(board, cat);

                int worst = int.MaxValue;
                foreach (int cell in candidates)
                {
                    board.SetBlocked(cell, true);
                    int score = Search(board, cat, plies - 1, false);
                    board.SetBlocked(cell, false);
                    if (score < worst)
                        worst = score;
                    if (worst <= TrappedScore)
                        break;
                }
                return worst;
            }

            if (!board.HasExit(cat))
                return TrappedScore;

            int best = TrappedScore;
            foreach (int move in board.GetLegalMoves(cat))
            {
                if (board.IsPerimeter(move))
                    return EscapeScore + plies;

                int score = Search(board, move, plies - 1, true);
                if (score > best)
                    best = score;
            }
            return best;
        }

        private static int Evaluate(CatBoard board, int cat)
        {
            if (!board.TryMeasureExits(cat, 2, out int shortest, out int nearCount))
                return TrappedScore;

            return 1000 - shortest * 20 + nearCount * 5;
        }
    }
}
