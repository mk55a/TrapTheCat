using System.Collections.Generic;
using NUnit.Framework;
using TrapTheCat.Core;
using TrapTheCat.Services;

public class LevelCatalogTests
{
    [Test]
    public void BundledLevels_AreListedInProgressionOrder()
    {
        LevelCatalog catalog = TestCatalogs.Catalog(
            TestCatalogs.BundledLevel("level-b", 2),
            TestCatalogs.BundledLevel("level-a", 1));

        Assert.AreEqual(2, catalog.Levels.Count);
        Assert.AreEqual("level-a", catalog.Levels[0].levelId);
        Assert.AreEqual("level-b", catalog.Levels[1].levelId);
        Assert.IsFalse(catalog.HasCustomLevels);
    }

    [Test]
    public void FindById_ReturnsTheLevel_OrNullWhenMissing()
    {
        LevelCatalog catalog = TestCatalogs.Catalog(TestCatalogs.BundledLevel("level-a", 1));

        Assert.AreEqual("level-a", catalog.FindById("level-a").levelId);
        Assert.IsNull(catalog.FindById("missing"));
        Assert.IsNull(catalog.FindById(null));
    }

    [Test]
    public void CustomIdentity_FollowsTheLevelNumber()
    {
        LevelData level = TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.NoBlocks());
        LevelCatalog.AssignCustomIdentity(level, 12);

        Assert.IsTrue(level.IsCustom);
        Assert.AreEqual("custom-level-12", level.levelId);
        Assert.AreEqual("Level 12", level.displayName);
        Assert.AreEqual(12, LevelCatalog.ParseCustomLevelNumber(level.levelId));
        Assert.AreEqual(0, LevelCatalog.ParseCustomLevelNumber("ftue-01"));
    }

    [Test]
    public void SavedCustomLevel_IsKeptAfterReload_AndClearRemovesIt()
    {
        string folder = TestCatalogs.NewTemporaryFolder();
        LevelCatalogStorage storage = TestCatalogs.EmptyStorage(folder);
        LevelCatalog catalog = new LevelCatalog(
            new List<LevelData> { TestCatalogs.BundledLevel("level-a", 1) },
            storage,
            TestShapes.List);
        LevelData custom = TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.OneGapAroundCentre());
        custom.customLevelNumber = 3;

        Assert.IsTrue(catalog.SaveCustomLevel(custom, out string saveError), saveError);
        Assert.IsTrue(catalog.HasCustomLevelNumber(3));

        LevelCatalog reloaded = new LevelCatalog(
            new List<LevelData> { TestCatalogs.BundledLevel("level-a", 1) },
            storage,
            TestShapes.List);
        Assert.IsNotNull(reloaded.FindById("custom-level-3"));
        Assert.AreSame(custom.boardShape, reloaded.FindById("custom-level-3").boardShape, "the shape id is saved and read back");

        Assert.IsTrue(reloaded.ClearCustomLevels(out string clearError), clearError);
        Assert.IsFalse(reloaded.HasCustomLevels);
        Assert.IsNotNull(reloaded.FindById("level-a"));
    }
}
