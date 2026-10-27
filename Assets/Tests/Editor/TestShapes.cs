using NUnit.Framework;
using TrapTheCat.Core;
using UnityEditor;

public static class TestShapes
{
    private const string ShapeListPath = "Assets/Data/BoardShapes/BoardShapeList.asset";

    public static BoardShapeList List
    {
        get
        {
            BoardShapeList list = AssetDatabase.LoadAssetAtPath<BoardShapeList>(ShapeListPath);
            Assert.IsNotNull(list, $"Missing the board shape list at {ShapeListPath}.");
            return list;
        }
    }

    public static BoardShape Rectangle { get { return ById("Rectangle"); } }
    public static BoardShape Square { get { return ById("Square"); } }
    public static BoardShape Circle { get { return ById("Circle"); } }
    public static BoardShape Hexagon { get { return ById("Hexagon"); } }

    public static BoardShape ById(string id)
    {
        BoardShape shape = List.FindById(id);
        Assert.IsNotNull(shape, $"The board shape list has no '{id}' shape.");
        return shape;
    }
}
