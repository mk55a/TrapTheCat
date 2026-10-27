using System.Collections.Generic;
using System.IO;
using TrapTheCat.Core;
using TrapTheCat.Services;

public static class TestCatalogs
{
    public static LevelData BundledLevel(string levelId, int progressionOrder)
    {
        LevelData level = TestBoards.Level(11, 11, TestBoards.Centre, TestBoards.NoBlocks());
        level.levelId = levelId;
        level.displayName = levelId;
        level.progressionOrder = progressionOrder;
        return level;
    }

    public static string NewTemporaryFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "TrapTheCatTests", Path.GetRandomFileName());
        Directory.CreateDirectory(folder);
        return folder;
    }

    public static LevelCatalogStorage EmptyStorage(string folder)
    {
        string catalogPath = Path.Combine(folder, "levelData.json");
        return new LevelCatalogStorage(null, null, catalogPath, catalogPath, new string[0], true);
    }

    public static LevelCatalog Catalog(params LevelData[] bundledLevels)
    {
        return new LevelCatalog(
            new List<LevelData>(bundledLevels),
            EmptyStorage(NewTemporaryFolder()),
            TestShapes.List);
    }
}
