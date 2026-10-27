using NUnit.Framework;
using TrapTheCat.Core;
using UnityEngine;

public class LevelEditorSessionTests
{
    private static readonly Vector2Int Inside = new Vector2Int(4, 4);
    private static readonly Vector2Int OtherInside = new Vector2Int(5, 4);
    private static readonly Vector2Int Edge = new Vector2Int(0, 4);

    private static LevelEditorSession NewSession(bool locked)
    {
        LevelEditorSession session = new LevelEditorSession(TestShapes.List.Shapes, new System.Random(7));
        session.StartNewDraft();
        if (locked)
            session.ToggleLock();
        return session;
    }

    [Test]
    public void ANewDraft_StartsUnlocked_OnTheFirstShape_AtTheDefaultSize()
    {
        LevelEditorSession session = new LevelEditorSession(TestShapes.List.Shapes, new System.Random(7));
        bool animated = false;
        session.BoardRebuilt += animate => animated = animate;

        session.StartNewDraft();

        Assert.IsFalse(session.IsLocked);
        Assert.AreSame(TestShapes.List.Shapes[0], session.Shape);
        Assert.AreEqual(LevelEditorSession.DefaultSize, session.Size);
        Assert.IsFalse(session.Board.HasCat);
        Assert.IsTrue(animated, "a new board's tiles animate in");
    }

    [Test]
    public void TilesCannotBeEdited_UntilTheBoardIsLocked()
    {
        LevelEditorSession session = NewSession(false);

        Assert.IsFalse(session.EditCell(Inside));
        Assert.IsFalse(session.Board.IsBlocked(Inside));
    }

    [Test]
    public void TheBlockTool_TogglesBlocks_AndUndoReversesEachClick()
    {
        LevelEditorSession session = NewSession(true);

        Assert.IsTrue(session.EditCell(Inside));
        Assert.IsTrue(session.Board.IsBlocked(Inside));
        Assert.IsTrue(session.EditCell(Inside));
        Assert.IsFalse(session.Board.IsBlocked(Inside), "a second click removes the block");

        session.Undo();
        Assert.IsTrue(session.Board.IsBlocked(Inside));
        session.Undo();
        Assert.IsFalse(session.Board.IsBlocked(Inside));
        Assert.IsFalse(session.CanUndo);
    }

    [Test]
    public void TheCatTool_RefusesEdgesAndBlocks_AndUndoMovesTheCatBack()
    {
        LevelEditorSession session = NewSession(true);
        session.EditCell(OtherInside);
        session.Tool = EditorTool.Cat;

        Assert.IsFalse(session.EditCell(Edge), "the cat cannot start on an exit");
        Assert.IsFalse(session.EditCell(OtherInside), "or on a block");
        Assert.IsTrue(session.EditCell(Inside));
        Assert.AreEqual(Inside, session.Board.CatCell);

        session.Tool = EditorTool.Blocks;
        Assert.IsFalse(session.EditCell(Inside), "blocks cannot cover the cat");

        session.Undo();
        Assert.IsFalse(session.Board.HasCat);
    }

    [Test]
    public void ShapeAndSize_AreFixedWhileLocked()
    {
        LevelEditorSession session = NewSession(true);
        BoardShape shape = session.Shape;

        session.ChangeShape(1);
        session.SetSize(LevelEditorSession.MaxSize);

        Assert.AreSame(shape, session.Shape);
        Assert.AreEqual(LevelEditorSession.DefaultSize, session.Size);
    }

    [Test]
    public void ChangingTheShape_WrapsAround_AndDropsThePlacedLayout()
    {
        LevelEditorSession session = NewSession(true);
        session.EditCell(Inside);
        session.ToggleLock();

        session.ChangeShape(-1);

        Assert.AreSame(TestShapes.List.Shapes[TestShapes.List.Shapes.Count - 1], session.Shape);
        Assert.AreEqual(0, session.Board.BlockedCells.Count);
        Assert.IsFalse(session.CanUndo);
    }

    [Test]
    public void SizeIsClampedToTheEditorRange()
    {
        LevelEditorSession session = NewSession(false);

        session.SetSize(100);

        Assert.AreEqual(LevelEditorSession.MaxSize, session.Size);
    }

    [Test]
    public void ADraftWithoutACat_IsNotValid()
    {
        LevelEditorSession session = NewSession(true);

        Assert.IsFalse(session.Validate().IsValid);
        Assert.IsFalse(session.IsValidated);
    }

    [Test]
    public void AGeneratedLayout_IsLockedValidatedAndHasACat_AndAnEditUnvalidatesIt()
    {
        LevelEditorSession session = NewSession(false);

        LevelLayoutGenerator generator = session.BeginGeneration();
        Assert.IsNotNull(generator);
        bool found = false;
        while (!found && generator.HasAttemptsLeft)
            found = generator.TryNextAttempt();
        Assert.IsTrue(found, "the seeded run finds a winnable layout");

        session.ApplyGeneratedLayout(generator);

        Assert.IsTrue(session.IsLocked);
        Assert.IsTrue(session.IsValidated);
        Assert.AreEqual(generator.CatCell, session.Board.CatCell);
        Assert.AreEqual(generator.Blocks.Count, session.Board.BlockedCells.Count);

        session.CycleDifficulty();
        Assert.IsFalse(session.IsValidated, "a different cat needs validating again");
    }
}
