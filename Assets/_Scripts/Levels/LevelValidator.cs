using System.Collections.Generic;
using UnityEngine;

public sealed class LevelValidationResult
{
    public bool IsValid { get; private set; }
    public string Message { get; private set; }
    public string DifficultyEstimate { get; private set; }
    public int ShortestEscapeSteps { get; private set; }
    public int ReachableExits { get; private set; }

    public LevelValidationResult(
        bool isValid,
        string message,
        string difficultyEstimate,
        int shortestEscapeSteps,
        int reachableExits)
    {
        IsValid = isValid;
        Message = message;
        DifficultyEstimate = difficultyEstimate;
        ShortestEscapeSteps = shortestEscapeSteps;
        ReachableExits = reachableExits;
    }
}

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
        if (!System.Enum.IsDefined(typeof(BoardShape), level.boardShape))
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

        bool[,] activeCells = BoardTopology.BuildMask(
            level.width,
            level.height,
            level.boardShape);
        Vector2Int catStart = level.catStart;
        if (!level.hasCatStart)
            return Invalid("Set the cat's starting tile before saving the level.");
        if (!IsActive(activeCells, catStart.x, catStart.y))
            return Invalid("The cat must start on an active tile.");
        if (BoardTopology.IsBoundary(activeCells, catStart.x, catStart.y))
            return Invalid("The cat must start away from the escape edge.");

        HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
        foreach (Vector2Int coordinate in level.blockedCells)
        {
            if (!IsActive(activeCells, coordinate.x, coordinate.y))
                return Invalid($"Blocked tile ({coordinate.x}, {coordinate.y}) is outside the selected board shape.");
            if (coordinate == catStart)
                return Invalid("A starting block cannot overlap the cat.");
            if (!blocked.Add(coordinate))
                return Invalid($"Blocked tile ({coordinate.x}, {coordinate.y}) is listed more than once.");
        }

        if (!TryMeasureEscapeRoutes(
                activeCells,
                blocked,
                catStart,
                out int shortestSteps,
                out int reachableExits))
            return Invalid("The cat has no route to an open escape edge.");

        string estimate = EstimateDifficulty(
            blocked.Count,
            shortestSteps,
            reachableExits,
            level.catDifficulty);
        return new LevelValidationResult(
            true,
            $"Valid. {reachableExits} escape tile(s) reachable; shortest route is {shortestSteps} move(s).",
            estimate,
            shortestSteps,
            reachableExits);
    }

    private static bool TryMeasureEscapeRoutes(
        bool[,] activeCells,
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
            if (BoardTopology.IsBoundary(activeCells, current.x, current.y))
            {
                shortestSteps = Mathf.Min(shortestSteps, currentDistance);
                reachableExits++;
                continue;
            }

            foreach (Vector2Int neighbor in BoardTopology.GetNeighbors(
                         activeCells,
                         current.x,
                         current.y))
            {
                if (blocked.Contains(neighbor) || distance.ContainsKey(neighbor))
                    continue;

                distance.Add(neighbor, currentDistance + 1);
                queue.Enqueue(neighbor);
            }
        }

        return reachableExits > 0;
    }

    private static string EstimateDifficulty(
        int startingBlockCount,
        int shortestEscapeSteps,
        int reachableExits,
        CatDifficulty catDifficulty)
    {
        float risk = Mathf.Max(0, 5 - shortestEscapeSteps) * 1.2f +
                     Mathf.Clamp01((reachableExits - 1) / 10f) * 2f -
                     Mathf.Min(2f, startingBlockCount * 0.25f);
        if (catDifficulty == CatDifficulty.Easy)
            risk -= 0.8f;
        else if (catDifficulty == CatDifficulty.Hard)
            risk += 0.8f;
        if (risk >= 5f)
            return "High";
        if (risk >= 3f)
            return "Medium";
        return "Low";
    }

    private static bool IsActive(bool[,] mask, int x, int y)
    {
        return x >= 0 && y >= 0 &&
               x < mask.GetLength(0) && y < mask.GetLength(1) &&
               mask[x, y];
    }

    private static LevelValidationResult Invalid(string message)
    {
        return new LevelValidationResult(false, message, "Unavailable", -1, 0);
    }
}
