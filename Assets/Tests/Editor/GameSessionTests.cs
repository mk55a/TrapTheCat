using System.Collections.Generic;
using NUnit.Framework;
using TrapTheCat.Core;
using UnityEngine;

public class GameSessionTests
{
    private static GameSession Session(Vector2Int cat, List<Vector2Int> blocks)
    {
        LevelData level = TestBoards.Level(11, 11, cat, blocks);
        return new GameSession(BoardState.FromLevel(level), CatDifficulty.Normal);
    }

    [Test]
    public void BlockingTheLastGap_TrapsTheCat_AndWins()
    {
        GameSession session = Session(TestBoards.Centre, TestBoards.OneGapAroundCentre());

        CatTurn turn = session.PlaceBlock(new Vector2Int(6, 6));

        Assert.IsTrue(turn.Trapped);
        Assert.AreEqual(GameResult.Won, session.Result);
        Assert.AreEqual(TestBoards.Centre, session.Board.CatCell, "a trapped cat stays put");
        Assert.IsFalse(session.CanPlaceBlock(new Vector2Int(0, 0)), "no moves after the game ends");
    }

    [Test]
    public void TheCatReplies_ByMovingOnTheBoard()
    {
        GameSession session = Session(TestBoards.Centre, TestBoards.OneGapAroundCentre());

        CatTurn turn = session.PlaceBlock(new Vector2Int(0, 0));

        Assert.IsFalse(turn.Trapped);
        Assert.IsFalse(turn.Escaped);
        Assert.AreEqual(new Vector2Int(6, 6), turn.To);
        Assert.AreEqual(new Vector2Int(6, 6), session.Board.CatCell);
        Assert.AreEqual(turn.To, turn.Route[0], "the drawn route starts at the cat's new cell");
        Assert.IsTrue(session.Board.Layout.IsExit(turn.Route[turn.Route.Count - 1]), "and ends at an exit");
        Assert.AreEqual(GameResult.Playing, session.Result);
        Assert.AreEqual(1, session.PlayerMoves);
    }

    [Test]
    public void TheCatReachingAnExit_Escapes_AndLoses()
    {
        GameSession session = Session(new Vector2Int(1, 5), TestBoards.NoBlocks());

        CatTurn turn = session.PlaceBlock(new Vector2Int(10, 10));

        Assert.IsTrue(turn.Escaped);
        Assert.AreEqual(new Vector2Int(0, 5), turn.To);
        Assert.AreEqual(GameResult.Lost, session.Result);
        Assert.IsTrue(session.IsOver);
    }

    [Test]
    public void RefusedBlocks_DoNotCountAsAMove()
    {
        GameSession session = Session(TestBoards.Centre, TestBoards.Wall());

        Assert.IsNull(session.PlaceBlock(TestBoards.Centre), "the cat's cell");
        Assert.IsNull(session.PlaceBlock(new Vector2Int(2, 8)), "an existing block");
        Assert.IsNull(session.PlaceBlock(new Vector2Int(20, 20)), "off the board");
        Assert.AreEqual(0, session.PlayerMoves);
        Assert.AreEqual(TestBoards.Centre, session.Board.CatCell);
    }
}
