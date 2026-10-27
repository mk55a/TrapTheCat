using NUnit.Framework;
using TrapTheCat.Core;
using UnityEngine;

public class LevelValidatorTests
{
    private static void AssertInvalid(LevelData level, string expectedMessage)
    {
        LevelValidationResult result = LevelValidator.Validate(level);
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(expectedMessage, result.Message);
    }

    [Test]
    public void OpenBoard_IsValid_AndReportsEscapeRoutes()
    {
        LevelData level = TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.Cells(2, 2));
        LevelValidationResult result = LevelValidator.Validate(level);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(5, result.ShortestEscapeSteps);
        Assert.AreEqual(38, result.ReachableExits);
    }

    [Test]
    public void CatOnEdge_IsInvalid()
    {
        AssertInvalid(
            TestBoards.Level(11, 11, new Vector2Int(0, 5), TestBoards.NoBlocks()),
            "The cat must start away from the escape edge.");
    }

    [Test]
    public void DuplicateBlock_IsInvalid()
    {
        AssertInvalid(
            TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.Cells(2, 2, 2, 2)),
            "Blocked tile (2, 2) is listed more than once.");
    }

    [Test]
    public void BlockOnCat_IsInvalid()
    {
        AssertInvalid(
            TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.Cells(5, 5)),
            "A starting block cannot overlap the cat.");
    }

    [Test]
    public void BlockOutsideBoard_IsInvalid()
    {
        AssertInvalid(
            TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.Cells(20, 2)),
            "Blocked tile (20, 2) is outside the selected board shape.");
    }

    [Test]
    public void SurroundedCat_IsInvalid()
    {
        AssertInvalid(
            TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.SurroundCentre()),
            "The cat has no route to an open escape edge.");
    }

    [Test]
    public void MissingCatStart_IsInvalid()
    {
        LevelData level = TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.NoBlocks());
        level.hasCatStart = false;
        AssertInvalid(level, "Set the cat's starting tile before saving the level.");
    }

    [Test]
    public void TooSmallBoard_IsInvalid()
    {
        AssertInvalid(
            TestBoards.Level(2, 11, new Vector2Int(1, 5), TestBoards.NoBlocks()),
            "Board width and height must each be at least 3.");
    }

    [Test]
    public void LevelIdWithSpace_IsInvalid()
    {
        LevelData level = TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.NoBlocks());
        level.levelId = "bad id";
        AssertInvalid(level, "Level IDs may contain only letters, numbers, hyphens, and underscores.");
    }

    [Test]
    public void WinnableLevel_PassesTheWinnabilityCheck()
    {
        LevelData level = TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.OneGapAroundCentre());
        LevelValidationResult result = LevelValidator.ValidateWinnable(level);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual("Winnable in 1 move(s); 1 of 10 opening blocks work.", result.Message);
    }
}
