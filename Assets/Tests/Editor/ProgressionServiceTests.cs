using NUnit.Framework;
using TrapTheCat.Core;
using TrapTheCat.Services;

public class ProgressionServiceTests
{
    private LevelData _first;
    private LevelData _second;
    private LevelData _third;
    private LevelCatalog _catalog;
    private InMemorySaveService _save;

    [SetUp]
    public void SetUp()
    {
        _first = TestCatalogs.BundledLevel("level-a", 1);
        _second = TestCatalogs.BundledLevel("level-b", 2);
        _third = TestCatalogs.BundledLevel("level-c", 3);
        _catalog = TestCatalogs.Catalog(_first, _second, _third);
        _save = new InMemorySaveService();
    }

    [Test]
    public void NewSave_OnlyFirstBundledLevelIsUnlocked()
    {
        ProgressionService progression = new ProgressionService(_save, _catalog);

        Assert.IsTrue(progression.IsUnlocked(_first));
        Assert.IsFalse(progression.IsUnlocked(_second));
        Assert.IsFalse(progression.IsUnlocked(_third));
    }

    [Test]
    public void WinningALevel_UnlocksOnlyTheNextOne_AndSaves()
    {
        ProgressionService progression = new ProgressionService(_save, _catalog);

        progression.RecordWin(_first.levelId);

        Assert.IsTrue(progression.IsUnlocked(_second));
        Assert.IsFalse(progression.IsUnlocked(_third));
        Assert.Contains(_first.levelId, _save.Stored.completedLevelIds);
        Assert.AreEqual(1, _save.SaveCount);
    }

    [Test]
    public void WinningTheLastLevel_UnlocksNothingNew()
    {
        ProgressionService progression = new ProgressionService(_save, _catalog);

        progression.RecordWin(_third.levelId);

        Assert.IsFalse(progression.IsUnlocked(_second));
        Assert.Contains(_third.levelId, _save.Stored.completedLevelIds);
    }

    [Test]
    public void CustomLevels_AreAlwaysUnlocked()
    {
        ProgressionService progression = new ProgressionService(_save, _catalog);
        LevelData custom = TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.NoBlocks());
        LevelCatalog.AssignCustomIdentity(custom, 7);

        Assert.IsTrue(progression.IsUnlocked(custom));
    }

    [Test]
    public void RecordPlay_CountsPlays_AndRemembersTheTutorial()
    {
        ProgressionService progression = new ProgressionService(_save, _catalog);

        progression.RecordPlay(false);
        Assert.IsFalse(progression.HasSeenTutorial);

        progression.RecordPlay(true);
        Assert.IsTrue(progression.HasSeenTutorial);
        Assert.AreEqual(2, _save.Stored.playCount);
    }

    [Test]
    public void Progress_IsRestoredFromTheSave()
    {
        new ProgressionService(_save, _catalog).RecordWin(_first.levelId);

        ProgressionService reloaded = new ProgressionService(_save, _catalog);

        Assert.IsTrue(reloaded.IsUnlocked(_second));
    }
}
