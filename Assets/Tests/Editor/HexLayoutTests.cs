using NUnit.Framework;
using TrapTheCat.Core;
using TrapTheCat.Presentation;
using UnityEngine;

public class HexLayoutTests
{
    private static readonly HexLayout Layout = new HexLayout(1f, 1f, Vector3.zero);

    [Test]
    public void OddRows_AreShiftedHalfATileRight()
    {
        Vector3 even = Layout.CellToWorld(new Vector2Int(2, 0));
        Vector3 odd = Layout.CellToWorld(new Vector2Int(2, 1));

        Assert.AreEqual(0.5f, odd.x - even.x, 0.0001f);
        Assert.AreEqual(0.8f, odd.y - even.y, 0.0001f);
    }

    [Test]
    public void EveryCellCentre_MapsBackToItsCell()
    {
        BoardLayout board = new BoardLayout(11, 11, TestShapes.Hexagon);

        foreach (Vector2Int cell in board.ActiveCells)
        {
            Assert.IsTrue(Layout.TryWorldToCell(Layout.CellToWorld(cell), board, out Vector2Int found));
            Assert.AreEqual(cell, found);
        }
    }

    [Test]
    public void PositionsOffTheBoard_HaveNoCell()
    {
        BoardLayout board = new BoardLayout(11, 11, TestShapes.Rectangle);

        Assert.IsFalse(Layout.TryWorldToCell(new Vector3(-5f, -5f, 0f), board, out _));
        Assert.IsFalse(Layout.TryWorldToCell(new Vector3(30f, 4f, 0f), board, out _));
    }

    [Test]
    public void BoardCentre_IsHalfTheBoardSize()
    {
        Vector2 size = Layout.GetBoardSize(10, 8);

        Assert.AreEqual(size * 0.5f, Layout.GetBoardCentreOffset(10, 8));
    }
}
