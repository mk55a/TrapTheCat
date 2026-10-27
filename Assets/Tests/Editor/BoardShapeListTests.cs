using System.Collections.Generic;
using NUnit.Framework;
using TrapTheCat.Core;
using UnityEngine;

public class BoardShapeListTests
{
    [Test]
    public void List_HasEveryShape_InEditorOrder()
    {
        List<string> ids = new List<string>();
        foreach (BoardShape shape in TestShapes.List.Shapes)
            ids.Add(shape.Id);

        CollectionAssert.AreEqual(new[] { "Rectangle", "Square", "Circle", "Hexagon" }, ids);
    }

    [Test]
    public void FindById_IgnoresCase_AndReturnsNullForUnknownIds()
    {
        Assert.AreSame(TestShapes.Hexagon, TestShapes.List.FindById("hexagon"));
        Assert.IsNull(TestShapes.List.FindById("Triangle"));
        Assert.IsNull(TestShapes.List.FindById(null));
    }

    [TestCase("Rectangle", 10, 10, 8)]
    [TestCase("Rectangle", 6, 6, 5)]
    [TestCase("Square", 10, 10, 10)]
    [TestCase("Circle", 9, 9, 9)]
    [TestCase("Hexagon", 9, 9, 9)]
    public void BoardSize_FollowsTheEditorSizeSetting(string shapeId, int size, int width, int height)
    {
        Assert.AreEqual(new Vector2Int(width, height), TestShapes.ById(shapeId).GetBoardSize(size));
    }
}
