using NUnit.Framework;
using TrapTheCat.Core;
using UnityEngine;

public class BoardLayoutTests
{
    [TestCase("Rectangle", 121, 40)]
    [TestCase("Square", 121, 40)]
    [TestCase("Circle", 73, 31)]
    [TestCase("Hexagon", 91, 30)]
    public void Shape_On11x11_HasExpectedActiveAndEdgeCells(string shapeId, int active, int edges)
    {
        BoardLayout layout = new BoardLayout(11, 11, TestShapes.ById(shapeId));
        int edgeCount = 0;
        foreach (Vector2Int cell in layout.ActiveCells)
        {
            if (layout.IsExit(cell))
                edgeCount++;
        }

        Assert.AreEqual(active, layout.ActiveCells.Count);
        Assert.AreEqual(edges, edgeCount);
    }

    [Test]
    public void InteriorCell_HasSixNeighbours()
    {
        BoardLayout layout = new BoardLayout(11, 11, TestShapes.Rectangle);

        CollectionAssert.AreEquivalent(TestBoards.SurroundCentre(), layout.Neighbours(TestBoards.Centre));
        Assert.IsFalse(layout.IsExit(TestBoards.Centre));
    }

    [Test]
    public void CellsOutsideTheBoard_AreNotContained()
    {
        BoardLayout layout = new BoardLayout(11, 11, TestShapes.Circle);

        Assert.IsFalse(layout.Contains(new Vector2Int(-1, 5)));
        Assert.IsFalse(layout.Contains(new Vector2Int(11, 5)));
        Assert.IsFalse(layout.Contains(new Vector2Int(0, 0)), "the circle leaves its corners out");
        Assert.IsFalse(layout.IsExit(new Vector2Int(0, 0)));
        Assert.AreEqual(0, layout.Neighbours(new Vector2Int(-1, -1)).Count);
    }

    [Test]
    public void ActiveMask_MatchesContains()
    {
        BoardLayout layout = new BoardLayout(9, 7, TestShapes.Hexagon);
        bool[,] mask = layout.CreateActiveMask();

        for (int x = 0; x < 9; x++)
        {
            for (int y = 0; y < 7; y++)
                Assert.AreEqual(layout.Contains(new Vector2Int(x, y)), mask[x, y]);
        }
    }
}
