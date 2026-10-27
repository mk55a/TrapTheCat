using System.Collections.Generic;
using NUnit.Framework;
using TrapTheCat.Core;
using UnityEngine;

public class CatBrainTests
{
    private static Vector2Int ChooseMove(
        CatDifficulty difficulty,
        BoardShape shape,
        List<Vector2Int> blocks,
        Vector2Int cat)
    {
        CatBoard board = new CatBoard(new BoardLayout(11, 11, shape).CreateActiveMask(), blocks);
        int move = new CatBrain(difficulty).ChooseMove(board, board.Index(cat.x, cat.y));
        return move < 0 ? new Vector2Int(-1, -1) : board.Cell(move);
    }

    [TestCase(CatDifficulty.Easy, 5, 4)]
    [TestCase(CatDifficulty.Normal, 5, 4)]
    [TestCase(CatDifficulty.Hard, 6, 5)]
    public void OpenBoard_FirstMove(CatDifficulty difficulty, int x, int y)
    {
        Vector2Int move = ChooseMove(difficulty, TestShapes.Rectangle, TestBoards.NoBlocks(), TestBoards.Centre);
        Assert.AreEqual(new Vector2Int(x, y), move);
    }

    [TestCase(CatDifficulty.Easy, 5, 4)]
    [TestCase(CatDifficulty.Normal, 6, 4)]
    [TestCase(CatDifficulty.Hard, 6, 5)]
    public void WallBoard_FirstMove(CatDifficulty difficulty, int x, int y)
    {
        Vector2Int move = ChooseMove(difficulty, TestShapes.Rectangle, TestBoards.Wall(), TestBoards.Centre);
        Assert.AreEqual(new Vector2Int(x, y), move);
    }

    [TestCase(CatDifficulty.Easy, 6, 4)]
    [TestCase(CatDifficulty.Normal, 6, 4)]
    [TestCase(CatDifficulty.Hard, 6, 5)]
    public void HexagonBoard_FirstMove(CatDifficulty difficulty, int x, int y)
    {
        List<Vector2Int> blocks = TestBoards.Cells(4, 4, 5, 4, 6, 6);
        Vector2Int move = ChooseMove(difficulty, TestShapes.Hexagon, blocks, TestBoards.Centre);
        Assert.AreEqual(new Vector2Int(x, y), move);
    }

    [TestCase(CatDifficulty.Easy)]
    [TestCase(CatDifficulty.Normal)]
    [TestCase(CatDifficulty.Hard)]
    public void SurroundedCat_HasNoMove(CatDifficulty difficulty)
    {
        Vector2Int move = ChooseMove(difficulty, TestShapes.Rectangle, TestBoards.SurroundCentre(), TestBoards.Centre);
        Assert.AreEqual(new Vector2Int(-1, -1), move);
    }

    [TestCase(CatDifficulty.Easy)]
    [TestCase(CatDifficulty.Normal)]
    [TestCase(CatDifficulty.Hard)]
    public void CatNextToEdge_StepsOntoEdge(CatDifficulty difficulty)
    {
        Vector2Int move = ChooseMove(difficulty, TestShapes.Rectangle, TestBoards.NoBlocks(), new Vector2Int(1, 5));
        Assert.AreEqual(new Vector2Int(0, 5), move);
    }

    [TestCase(CatDifficulty.Easy)]
    [TestCase(CatDifficulty.Normal)]
    [TestCase(CatDifficulty.Hard)]
    public void OneGap_CatTakesTheGap(CatDifficulty difficulty)
    {
        Vector2Int move = ChooseMove(difficulty, TestShapes.Rectangle, TestBoards.OneGapAroundCentre(), TestBoards.Centre);
        Assert.AreEqual(new Vector2Int(6, 6), move);
    }

    [Test]
    public void EasyCat_RemembersItsTargetExit_OtherCatsDoNot()
    {
        CatBoard board = new CatBoard(new BoardLayout(11, 11, TestShapes.Rectangle).CreateActiveMask(), TestBoards.NoBlocks());
        int cat = board.Index(5, 5);

        CatBrain easy = new CatBrain(CatDifficulty.Easy);
        easy.ChooseMove(board, cat);
        Assert.AreEqual(board.Index(3, 0), easy.Target);

        CatBrain normal = new CatBrain(CatDifficulty.Normal);
        normal.ChooseMove(board, cat);
        Assert.AreEqual(-1, normal.Target);
    }
}
