using System.Collections.Generic;
using NUnit.Framework;
using TrapTheCat.Core;
using UnityEngine;

public class BoardStateTests
{
    private static BoardState EmptyBoard()
    {
        return new BoardState(new BoardLayout(11, 11, TestShapes.Rectangle));
    }

    [Test]
    public void FromLevel_PlacesTheLevelsBlocksAndCat()
    {
        LevelData level = TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.Wall());

        BoardState board = BoardState.FromLevel(level);

        Assert.IsTrue(board.HasCat);
        Assert.AreEqual(TestBoards.Centre, board.CatCell);
        CollectionAssert.AreEqual(TestBoards.Wall(), board.BlockedCells);
        foreach (Vector2Int cell in TestBoards.Wall())
            Assert.IsTrue(board.IsBlocked(cell));
    }

    [Test]
    public void FromLevel_WithoutCatStart_HasNoCat()
    {
        LevelData level = TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.NoBlocks());
        level.hasCatStart = false;

        Assert.IsFalse(BoardState.FromLevel(level).HasCat);
    }

    [Test]
    public void PlaceBlock_BlocksTheCell_AndRaisesBlockPlaced()
    {
        BoardState board = EmptyBoard();
        List<Vector2Int> placed = new List<Vector2Int>();
        board.BlockPlaced += placed.Add;
        Vector2Int cell = new Vector2Int(2, 3);

        Assert.IsTrue(board.PlaceBlock(cell));

        Assert.IsTrue(board.IsBlocked(cell));
        Assert.IsFalse(board.CanPlaceBlock(cell));
        CollectionAssert.AreEqual(new[] { cell }, placed);
    }

    [Test]
    public void PlaceBlock_IsRefused_OnTheCatABlockOrOutsideTheBoard()
    {
        BoardState board = EmptyBoard();
        board.MoveCat(TestBoards.Centre);
        board.PlaceBlock(new Vector2Int(2, 3));
        int events = 0;
        board.BlockPlaced += cell => events++;

        Assert.IsFalse(board.PlaceBlock(TestBoards.Centre));
        Assert.IsFalse(board.PlaceBlock(new Vector2Int(2, 3)));
        Assert.IsFalse(board.PlaceBlock(new Vector2Int(11, 0)));
        Assert.AreEqual(0, events);
        Assert.AreEqual(1, board.BlockedCells.Count);
    }

    [Test]
    public void MoveCat_MovesToAFreeCell_AndRaisesCatMoved()
    {
        BoardState board = EmptyBoard();
        board.MoveCat(TestBoards.Centre);
        Vector2Int moved = new Vector2Int(-1, -1);
        board.CatMoved += cell => moved = cell;

        Assert.IsTrue(board.MoveCat(new Vector2Int(6, 5)));

        Assert.AreEqual(new Vector2Int(6, 5), board.CatCell);
        Assert.AreEqual(new Vector2Int(6, 5), moved);
        Assert.IsTrue(board.CanPlaceBlock(TestBoards.Centre), "the old cat cell is free again");
    }

    [Test]
    public void MoveCat_IsRefused_OntoABlock()
    {
        BoardState board = EmptyBoard();
        board.MoveCat(TestBoards.Centre);
        board.PlaceBlock(new Vector2Int(6, 5));

        Assert.IsFalse(board.MoveCat(new Vector2Int(6, 5)));
        Assert.AreEqual(TestBoards.Centre, board.CatCell);
    }
}
