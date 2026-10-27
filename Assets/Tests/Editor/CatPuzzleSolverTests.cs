using System.Collections.Generic;
using NUnit.Framework;
using TrapTheCat.Core;
using UnityEngine;

public class CatPuzzleSolverTests
{
    private static PuzzleSolution Solve(int size, List<Vector2Int> blocks, Vector2Int cat, CatDifficulty difficulty)
    {
        bool[,] mask = new BoardLayout(size, size, TestShapes.Rectangle).CreateActiveMask();
        return CatPuzzleSolver.Solve(mask, blocks, cat, difficulty);
    }

    [TestCase(CatDifficulty.Easy)]
    [TestCase(CatDifficulty.Normal)]
    [TestCase(CatDifficulty.Hard)]
    public void OneGap_IsWonInOneMove_ByBlockingTheGap(CatDifficulty difficulty)
    {
        PuzzleSolution solution = Solve(11, TestBoards.OneGapAroundCentre(), TestBoards.Centre, difficulty);

        Assert.IsTrue(solution.Winnable);
        Assert.IsFalse(solution.Inconclusive);
        Assert.AreEqual(1, solution.MinPlayerMoves);
        Assert.AreEqual(1, solution.WinningFirstMoves);
        Assert.AreEqual(10, solution.FirstMoveOptions);
        CollectionAssert.AreEqual(TestBoards.Cells(6, 6), solution.WinningLine);
    }

    [TestCase(CatDifficulty.Easy)]
    [TestCase(CatDifficulty.Normal)]
    [TestCase(CatDifficulty.Hard)]
    public void CatTwoStepsFromOpenEdges_CannotBeTrapped(CatDifficulty difficulty)
    {
        List<Vector2Int> blocks = TestBoards.Cells(3, 4, 5, 4, 3, 3, 4, 3, 3, 5, 4, 5, 5, 2, 6, 4);
        PuzzleSolution solution = Solve(9, blocks, new Vector2Int(4, 4), difficulty);

        Assert.IsFalse(solution.Winnable);
        Assert.IsFalse(solution.Inconclusive);
    }

    [TestCase(CatDifficulty.Easy, false, 0)]
    [TestCase(CatDifficulty.Normal, false, 0)]
    [TestCase(CatDifficulty.Hard, true, 7)]
    public void SmallBoard_ResultPerDifficulty(CatDifficulty difficulty, bool winnable, int minMoves)
    {
        List<Vector2Int> blocks = TestBoards.Cells(2, 2, 3, 2, 2, 4, 3, 4, 1, 3);
        PuzzleSolution solution = Solve(7, blocks, new Vector2Int(3, 3), difficulty);

        Assert.IsFalse(solution.Inconclusive);
        Assert.AreEqual(winnable, solution.Winnable);
        Assert.AreEqual(minMoves, solution.MinPlayerMoves);
    }
}
