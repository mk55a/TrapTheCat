using System.Collections.Generic;
using TrapTheCat.Core;
using UnityEngine;

public static class TestBoards
{
    public static List<Vector2Int> Cells(params int[] xy)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        for (int i = 0; i < xy.Length; i += 2)
            cells.Add(new Vector2Int(xy[i], xy[i + 1]));
        return cells;
    }

    public static readonly Vector2Int Centre = new Vector2Int(5, 5);

    public static List<Vector2Int> NoBlocks()
    {
        return new List<Vector2Int>();
    }

    public static List<Vector2Int> Wall()
    {
        return Cells(2, 8, 3, 8, 4, 8, 5, 8, 6, 8, 7, 8, 8, 8, 3, 3, 4, 3);
    }

    public static List<Vector2Int> OneGapAroundCentre()
    {
        return Cells(4, 5, 6, 5, 5, 4, 6, 4, 5, 6);
    }

    public static List<Vector2Int> SurroundCentre()
    {
        return Cells(4, 5, 6, 5, 5, 4, 6, 4, 5, 6, 6, 6);
    }

    public static LevelData Level(int width, int height, Vector2Int catStart, List<Vector2Int> blocks)
    {
        return new LevelData
        {
            levelId = "test-level",
            displayName = "Test",
            width = width,
            height = height,
            boardShape = TestShapes.Rectangle,
            catStart = catStart,
            blockedCells = blocks
        };
    }
}
