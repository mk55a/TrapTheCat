using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace TrapTheCat.Core
{
    public sealed class CatPuzzleSolver
    {
        private readonly CatSearchBoard _board;
        private readonly PlayerBlockPredictor _blockPredictor;
        private readonly int _breadth;
        private readonly int _nodeBudget;
        private readonly long _timeBudgetMs;
        private readonly ulong[] _blockKeys;
        private readonly ulong[] _catKeys;
        private readonly ulong[] _targetKeys;
        private readonly Dictionary<ulong, int> _failedDepth = new Dictionary<ulong, int>();
        private readonly Stopwatch _clock = new Stopwatch();
        private int _nodes;
        private bool _outOfBudget;

        private CatPuzzleSolver(CatSearchBoard board, int breadth, int nodeBudget, long timeBudgetMs)
        {
            _board = board;
            _blockPredictor = new PlayerBlockPredictor(new CatEscapeRouteFinder(board));
            _breadth = breadth;
            _nodeBudget = nodeBudget;
            _timeBudgetMs = timeBudgetMs;
            _blockKeys = new ulong[board.Count];
            _catKeys = new ulong[board.Count];
            _targetKeys = new ulong[board.Count + 1];
            ulong state = 0x9E3779B97F4A7C15UL;
            for (int i = 0; i < board.Count; i++)
            {
                _blockKeys[i] = Next(ref state);
                _catKeys[i] = Next(ref state);
            }
            for (int i = 0; i <= board.Count; i++)
                _targetKeys[i] = Next(ref state);
        }

        public static PuzzleSolution Solve(
            bool[,] active,
            IEnumerable<Vector2Int> blockedCells,
            Vector2Int catStart,
            CatDifficulty difficulty,
            int maxPlayerMoves = 8,
            int breadth = 10,
            int nodeBudget = 400000,
            long timeBudgetMs = 4000)
        {
            CatSearchBoard board = new CatSearchBoard(active, blockedCells);
            CatPuzzleSolver solver = new CatPuzzleSolver(board, breadth, nodeBudget, timeBudgetMs);
            return solver.Run(board.Index(catStart.x, catStart.y), difficulty, maxPlayerMoves);
        }

        private PuzzleSolution Run(int cat, CatDifficulty difficulty, int maxPlayerMoves)
        {
            PuzzleSolution solution = new PuzzleSolution();
            CatBrain brain = new CatBrain(difficulty);
            ulong rootHash = 0;
            for (int i = 0; i < _board.Count; i++)
            {
                if (_board.IsBlocked(i))
                    rootHash ^= _blockKeys[i];
            }

            _clock.Start();
            List<int> line = new List<int>();
            for (int depth = 1; depth <= maxPlayerMoves; depth++)
            {
                line.Clear();
                if (CanWin(cat, brain, depth, rootHash, line))
                {
                    solution.Winnable = true;
                    solution.MinPlayerMoves = depth;
                    line.Reverse();
                    foreach (int cell in line)
                        solution.WinningLine.Add(_board.Cell(cell));
                    break;
                }
                if (_outOfBudget)
                {
                    solution.Inconclusive = true;
                    return solution;
                }
            }

            if (!solution.Winnable)
                return solution;

            CountFirstMoves(cat, brain, solution, rootHash);
            return solution;
        }

        private void CountFirstMoves(int cat, CatBrain brain, PuzzleSolution solution, ulong rootHash)
        {
            _nodes = 0;
            _outOfBudget = false;
            _clock.Restart();
            List<int> candidates = _blockPredictor.LikelyBlocks(cat, _breadth);
            solution.FirstMoveOptions = candidates.Count;
            foreach (int cell in candidates)
            {
                _board.SetBlocked(cell, true);
                CatBrain reply = brain.Clone();
                int next = reply.ChooseMove(_board, cat);
                bool wins = next < 0 ||
                            (!_board.IsExit(next) && solution.MinPlayerMoves > 1 &&
                             CanWin(next, reply, solution.MinPlayerMoves - 1,
                                 rootHash ^ _blockKeys[cell], new List<int>()));
                _board.SetBlocked(cell, false);
                if (wins)
                    solution.WinningFirstMoves++;
                if (_outOfBudget)
                    break;
            }
        }

        private bool CanWin(int cat, CatBrain brain, int depth, ulong blockHash, List<int> line)
        {
            if (++_nodes > _nodeBudget || _clock.ElapsedMilliseconds > _timeBudgetMs)
            {
                _outOfBudget = true;
                return false;
            }

            ulong key = blockHash ^ _catKeys[cat] ^ _targetKeys[brain.Target + 1];
            if (_failedDepth.TryGetValue(key, out int failedDepth) && failedDepth >= depth)
                return false;

            List<int> candidates = _blockPredictor.LikelyBlocks(cat, _breadth);
            foreach (int cell in candidates)
            {
                _board.SetBlocked(cell, true);
                CatBrain reply = brain.Clone();
                int next = reply.ChooseMove(_board, cat);
                bool wins = next < 0;
                if (!wins && !_board.IsExit(next) && depth > 1)
                    wins = CanWin(next, reply, depth - 1, blockHash ^ _blockKeys[cell], line);
                _board.SetBlocked(cell, false);

                if (wins)
                {
                    line.Add(cell);
                    return true;
                }
                if (_outOfBudget)
                    return false;
            }

            _failedDepth[key] = depth;
            return false;
        }

        private static ulong Next(ref ulong state)
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            return state;
        }
    }
}
