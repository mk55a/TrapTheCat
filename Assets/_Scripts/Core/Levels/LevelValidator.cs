using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    public static class LevelValidator
    {
        public static LevelValidationResult Validate(LevelData level)
        {
            if (level == null)
                return Invalid("Level data is missing.");
            if (level.formatVersion != 1)
                return Invalid($"Unsupported level data version: {level.formatVersion}.");
            if (string.IsNullOrWhiteSpace(level.levelId) ||
                string.IsNullOrWhiteSpace(level.displayName))
                return Invalid("A level ID and display name are required.");
            foreach (char character in level.levelId)
            {
                if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                    return Invalid("Level IDs may contain only letters, numbers, hyphens, and underscores.");
            }
            if (level.width < 3 || level.height < 3)
                return Invalid("Board width and height must each be at least 3.");
            if (level.width > 32 || level.height > 32)
                return Invalid("Board width and height cannot exceed 32.");
            if (level.customLevelNumber < 0 || level.customLevelNumber > 999)
                return Invalid("Custom level numbers must be between 1 and 999.");
            if (level.boardShape == null)
                return Invalid("The selected board shape is not supported.");
            if (!System.Enum.IsDefined(typeof(CatDifficulty), level.catDifficulty))
                return Invalid("The selected cat difficulty is not supported.");
            if (level.blockedCells == null)
                return Invalid("The blocked-cell list is missing.");
            if (level.tutorialSteps != null)
            {
                foreach (TutorialStep step in level.tutorialSteps)
                {
                    if (step == null || step.afterPlayerMoves < 0)
                        return Invalid("Tutorial steps must be present and use non-negative move counts.");
                }
            }

            BoardLayout layout = new BoardLayout(level.width, level.height, level.boardShape);
            Vector2Int catStart = level.catStart;
            if (!level.hasCatStart)
                return Invalid("Set the cat's starting tile before saving the level.");
            if (!layout.Contains(catStart))
                return Invalid("The cat must start on an active tile.");
            if (layout.IsExit(catStart))
                return Invalid("The cat must start away from the escape edge.");

            HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
            foreach (Vector2Int coordinate in level.blockedCells)
            {
                if (!layout.Contains(coordinate))
                    return Invalid($"Blocked tile ({coordinate.x}, {coordinate.y}) is outside the selected board shape.");
                if (coordinate == catStart)
                    return Invalid("A starting block cannot overlap the cat.");
                if (!blocked.Add(coordinate))
                    return Invalid($"Blocked tile ({coordinate.x}, {coordinate.y}) is listed more than once.");
            }

            if (!TryMeasureEscapeRoutes(
                    layout,
                    blocked,
                    catStart,
                    out int shortestSteps,
                    out int reachableExits))
                return Invalid("The cat has no route to an open escape edge.");

            return new LevelValidationResult(
                true,
                $"Valid. {reachableExits} escape tile(s) reachable; shortest route is {shortestSteps} move(s).",
                shortestSteps,
                reachableExits);
        }

        public static LevelValidationResult ValidateWinnable(
            LevelData level,
            int nodeBudget = 400000,
            long timeBudgetMs = 4000)
        {
            LevelValidationResult structural = Validate(level);
            if (!structural.IsValid)
                return structural;

            string signature = BuildSignature(level);
            if (_winnableCache.TryGetValue(signature, out LevelValidationResult cached))
                return cached;

            BoardLayout layout = new BoardLayout(level.width, level.height, level.boardShape);
            PuzzleSolution solution = CatPuzzleSolver.Solve(
                layout.CreateActiveMask(),
                level.blockedCells,
                level.catStart,
                level.catDifficulty,
                8,
                10,
                nodeBudget,
                timeBudgetMs);

            LevelValidationResult result;
            if (solution.Winnable)
            {
                result = new LevelValidationResult(
                    true,
                    $"Winnable in {solution.MinPlayerMoves} move(s); {solution.WinningFirstMoves} of " +
                    $"{solution.FirstMoveOptions} opening blocks work.",
                    structural.ShortestEscapeSteps,
                    structural.ReachableExits);
            }
            else
            {
                string reason = solution.Inconclusive
                    ? "Could not prove this level is winnable in time. Block more tiles or simplify it."
                    : $"The {level.catDifficulty} cat cannot be trapped. Add blocked tiles or lower the difficulty.";
                result = Invalid(reason);
            }

            if (!solution.Inconclusive)
                _winnableCache[signature] = result;
            return result;
        }

        private static readonly Dictionary<string, LevelValidationResult> _winnableCache =
            new Dictionary<string, LevelValidationResult>();

        private static string BuildSignature(LevelData level)
        {
            List<Vector2Int> cells = new List<Vector2Int>(level.blockedCells);
            cells.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append(level.width).Append('x').Append(level.height)
                .Append('|').Append(level.boardShape.Id)
                .Append('|').Append((int)level.catDifficulty)
                .Append('|').Append(level.catStart.x).Append(',').Append(level.catStart.y);
            foreach (Vector2Int cell in cells)
                builder.Append('|').Append(cell.x).Append(',').Append(cell.y);
            return builder.ToString();
        }

        private static bool TryMeasureEscapeRoutes(
            BoardLayout layout,
            HashSet<Vector2Int> blocked,
            Vector2Int start,
            out int shortestSteps,
            out int reachableExits)
        {
            shortestSteps = int.MaxValue;
            reachableExits = 0;
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            Dictionary<Vector2Int, int> distance = new Dictionary<Vector2Int, int>();
            queue.Enqueue(start);
            distance.Add(start, 0);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentDistance = distance[current];
                if (layout.IsExit(current))
                {
                    shortestSteps = Mathf.Min(shortestSteps, currentDistance);
                    reachableExits++;
                    continue;
                }

                foreach (Vector2Int neighbor in layout.Neighbours(current))
                {
                    if (blocked.Contains(neighbor) || distance.ContainsKey(neighbor))
                        continue;

                    distance.Add(neighbor, currentDistance + 1);
                    queue.Enqueue(neighbor);
                }
            }

            return reachableExits > 0;
        }

        private static LevelValidationResult Invalid(string message)
        {
            return new LevelValidationResult(false, message, -1, 0);
        }
    }
}
