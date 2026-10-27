using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    public sealed class LevelLayoutGenerator
    {
        public const int Attempts = 60;
        private const float SparsestDensity = 0.08f;
        private const float DensestDensity = 0.45f;
        private const int NodeBudget = 100000;
        private const long TimeBudgetMs = 250;

        private readonly LevelData _candidate;
        private readonly List<Vector2Int> _freeCells;
        private readonly System.Random _random;

        public int AttemptsMade { get; private set; }
        public Vector2Int CatCell { get { return _candidate.catStart; } }
        public IReadOnlyList<Vector2Int> Blocks { get { return _candidate.blockedCells; } }
        public LevelValidationResult Result { get; private set; }

        public bool HasAttemptsLeft
        {
            get { return AttemptsMade < Attempts; }
        }

        private LevelLayoutGenerator(LevelData candidate, List<Vector2Int> freeCells, System.Random random)
        {
            _candidate = candidate;
            _freeCells = freeCells;
            _random = random;
        }

        public static LevelLayoutGenerator Create(LevelData draft, BoardLayout layout, System.Random random)
        {
            Vector2 centre = new Vector2((layout.Width - 1) * 0.5f, (layout.Height - 1) * 0.5f);
            List<Vector2Int> cells = new List<Vector2Int>();
            Vector2Int cat = Vector2Int.zero;
            float bestDistance = float.MaxValue;
            bool hasCat = false;
            foreach (Vector2Int cell in layout.ActiveCells)
            {
                cells.Add(cell);
                if (layout.IsExit(cell))
                    continue;

                float distance = ((Vector2)cell - centre).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    cat = cell;
                    hasCat = true;
                }
            }

            if (!hasCat)
                return null;

            cells.Remove(cat);
            LevelData candidate = new LevelData
            {
                levelId = draft.levelId,
                displayName = draft.displayName,
                customLevelNumber = draft.customLevelNumber,
                boardShape = draft.boardShape,
                width = draft.width,
                height = draft.height,
                hasCatStart = true,
                catStart = cat,
                catDifficulty = draft.catDifficulty,
                blockedCells = new List<Vector2Int>(),
                tutorialSteps = new List<TutorialStep>()
            };
            return new LevelLayoutGenerator(candidate, cells, random);
        }

        public bool TryNextAttempt()
        {
            if (!HasAttemptsLeft)
                throw new InvalidOperationException("The generator has used all its attempts.");

            float density = Mathf.Lerp(SparsestDensity, DensestDensity, AttemptsMade / (float)(Attempts - 1));
            int count = Mathf.RoundToInt(_freeCells.Count * density);
            ShuffleFirst(count);
            _candidate.blockedCells = _freeCells.GetRange(0, count);
            AttemptsMade++;

            Result = LevelValidator.ValidateWinnable(_candidate, NodeBudget, TimeBudgetMs);
            return Result.IsValid;
        }

        private void ShuffleFirst(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int pick = _random.Next(i, _freeCells.Count);
                Vector2Int swap = _freeCells[i];
                _freeCells[i] = _freeCells[pick];
                _freeCells[pick] = swap;
            }
        }
    }
}
