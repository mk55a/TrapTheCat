using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// Checks the playable ad levels with the real solver: every level except the last must be
// winnable within the move limit, and the last one must be a guaranteed loss.
public static class PlayableLevelCheck
{
    private const int MaxMoves = 8;

    [MenuItem("TrapTheCat/Validate Playable Levels")]
    public static void Run()
    {
        string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Resources/Playable" });
        List<string> paths = new List<string>();
        foreach (string guid in guids)
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        paths.Sort(StringComparer.Ordinal);

        for (int i = 0; i < paths.Count; i++)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(paths[i]);
            PlayableLevelData level = JsonConvert.DeserializeObject<PlayableLevelData>(asset.text);
            Enum.TryParse(level.boardShape, true, out BoardShape shape);
            Enum.TryParse(level.catDifficulty, true, out CatDifficulty difficulty);

            List<Vector2Int> blocked = new List<Vector2Int>();
            foreach (PlayableCellData cell in level.blockedCells)
                blocked.Add(new Vector2Int(cell.x, cell.y));

            PuzzleSolution result = CatPuzzleSolver.Solve(
                BoardTopology.BuildMask(level.width, level.height, shape),
                blocked,
                new Vector2Int(level.catStartX, level.catStartY),
                difficulty,
                MaxMoves);

            bool shouldWin = i < paths.Count - 1;
            bool ok = !result.Inconclusive && result.Winnable == shouldWin;
            string summary = result.Inconclusive
                ? "inconclusive"
                : result.Winnable ? "winnable in " + result.MinPlayerMoves + " move(s)" : "not winnable";
            string message = (ok ? "OK   " : "FAIL ") + paths[i] + ": " + summary +
                             " (expected " + (shouldWin ? "winnable" : "loss") + ")";
            if (ok)
                Debug.Log(message);
            else
                Debug.LogError(message);
        }
    }
}
