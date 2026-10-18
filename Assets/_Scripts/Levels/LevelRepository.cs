using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public static class LevelRepository
{
    private const string BundledLevelsPath = "Levels";
    private const string CatalogFileName = "levelData.json";

    private static string ProjectCatalogPath
    {
        get { return Path.Combine(Application.dataPath, "Resources", "Levels", CatalogFileName); }
    }

    private static string UserCatalogPath
    {
        get { return Path.Combine(Application.persistentDataPath, CatalogFileName); }
    }

    private static string LegacyProjectLevelsPath
    {
        get { return Path.Combine(Application.dataPath, "Resources", "Levels", "Custom"); }
    }

    private static string LegacyUserLevelsPath
    {
        get { return Path.Combine(Application.persistentDataPath, "Levels"); }
    }

    public static string StorageLocation
    {
        get
        {
#if UNITY_EDITOR
            return ProjectCatalogPath;
#else
            return UserCatalogPath;
#endif
        }
    }

    public static List<LevelData> LoadAll()
    {
        Dictionary<string, LevelData> levelsById = new Dictionary<string, LevelData>();
        List<LevelData> bundledLevels = LoadBundledLevels();
        foreach (LevelData level in bundledLevels)
            AddLevel(levelsById, level, "bundled level asset");

        bool hasCatalogSource;
#if UNITY_EDITOR
        hasCatalogSource = File.Exists(ProjectCatalogPath);
        LoadCatalogFile(levelsById, ProjectCatalogPath, out _);
#else
        TextAsset bundledCatalog = Resources.Load<TextAsset>("Levels/levelData");
        if (bundledCatalog != null)
            LoadCatalogJson(
                levelsById,
                bundledCatalog.text,
                "Resources/Levels/levelData.json",
                out _);
        hasCatalogSource = bundledCatalog != null;
#endif

        hasCatalogSource |= File.Exists(UserCatalogPath);
        Dictionary<string, LevelData> userLevels = new Dictionary<string, LevelData>();
        LoadCatalogFile(userLevels, UserCatalogPath, out bool suppressBundledCustomLevels);
#if !UNITY_EDITOR
        if (suppressBundledCustomLevels)
        {
            string[] bundledCustomIds = levelsById
                .Where(entry => IsCustomLevel(entry.Value))
                .Select(entry => entry.Key)
                .ToArray();
            foreach (string levelId in bundledCustomIds)
                levelsById.Remove(levelId);
        }
#endif
        foreach (LevelData level in userLevels.Values)
            AddLevel(levelsById, level, UserCatalogPath);

        bool hasCatalogCustomLevels = levelsById.Values.Any(IsCustomLevel);
        if (hasCatalogCustomLevels)
        {
            return SortLevels(levelsById);
        }
        if (hasCatalogSource)
            return SortLevels(levelsById);

#if UNITY_EDITOR
        LoadLegacyDirectory(levelsById, LegacyProjectLevelsPath);
#endif
        LoadLegacyDirectory(levelsById, LegacyUserLevelsPath);

        return SortLevels(levelsById);
    }

    private static List<LevelData> SortLevels(Dictionary<string, LevelData> levelsById)
    {
        return levelsById.Values
            .OrderBy(level => IsCustomLevel(level) ? 1 : 0)
            .ThenBy(level => IsCustomLevel(level)
                ? level.customLevelNumber
                : level.progressionOrder)
            .ThenBy(level => level.displayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static LevelData LoadById(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return null;

        return LoadAll().FirstOrDefault(level => level.levelId == levelId);
    }

    public static bool HasCustomLevelNumber(int levelNumber)
    {
        return LoadAll().Any(level =>
            IsCustomLevel(level) && level.customLevelNumber == levelNumber);
    }

    public static bool SaveCustom(LevelData level, out string error)
    {
        error = null;
        if (level == null || level.customLevelNumber < 1 || level.customLevelNumber > 999)
        {
            error = "Enter a level number from 1 to 999.";
            return false;
        }

        level.levelId = GetCustomLevelId(level.customLevelNumber);
        level.displayName = $"Level {level.customLevelNumber}";
        level.progressionOrder = 1000 + level.customLevelNumber;

        LevelValidationResult validation = LevelValidator.Validate(level);
        if (!validation.IsValid)
        {
            error = validation.Message;
            return false;
        }

        try
        {
            string catalogPath = StorageLocation;
            List<LevelData> levels = LoadAll()
                .Where(existing => IsCustomLevel(existing) &&
                                   existing.customLevelNumber != level.customLevelNumber)
                .ToList();
            levels.Add(level);
            levels = levels
                .OrderBy(existing => existing.customLevelNumber)
                .ToList();

            LevelCatalogData catalog = new LevelCatalogData();
            catalog.suppressBundledCustomLevels = true;
            foreach (LevelData entry in levels)
                catalog.levels.Add(LevelJsonData.FromLevel(entry));

            Directory.CreateDirectory(Path.GetDirectoryName(catalogPath));
            WriteCatalogAtomically(catalogPath, JsonConvert.SerializeObject(catalog, Formatting.Indented));
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            Debug.Log($"Saved {level.displayName} to '{catalogPath}'.");
            return true;
        }
        catch (IOException exception)
        {
            error = $"Could not save the level catalog: {exception.Message}";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            error = $"Could not save the level catalog: {exception.Message}";
            return false;
        }
        catch (UnityException exception)
        {
            error = $"The catalog was saved, but Unity could not refresh its asset database: {exception.Message}";
            return false;
        }
        catch (JsonException exception)
        {
            error = $"Could not serialize the level catalog: {exception.Message}";
            return false;
        }
    }

    public static bool ClearCustomLevels(out string error)
    {
        error = null;
        try
        {
#if UNITY_EDITOR
            ClearLegacyLevelFiles(LegacyProjectLevelsPath);
#endif
            ClearLegacyLevelFiles(LegacyUserLevelsPath);

            LevelCatalogData emptyCatalog = new LevelCatalogData
            {
                suppressBundledCustomLevels = true
            };
            string catalogJson = JsonConvert.SerializeObject(emptyCatalog, Formatting.Indented);
#if UNITY_EDITOR
            Directory.CreateDirectory(Path.GetDirectoryName(ProjectCatalogPath));
            WriteCatalogAtomically(ProjectCatalogPath, catalogJson);
            UnityEditor.AssetDatabase.Refresh();
#endif
            Directory.CreateDirectory(Path.GetDirectoryName(UserCatalogPath));
            WriteCatalogAtomically(UserCatalogPath, catalogJson);
            Debug.Log("Cleared all custom levels. FTUE and player progress were preserved.");
            return true;
        }
        catch (IOException exception)
        {
            error = $"Could not clear custom levels: {exception.Message}";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            error = $"Could not clear custom levels: {exception.Message}";
            return false;
        }
        catch (UnityException exception)
        {
            error = $"Custom levels were cleared, but Unity could not refresh its asset database: {exception.Message}";
            return false;
        }
        catch (JsonException exception)
        {
            error = $"Could not serialize the cleared level catalog: {exception.Message}";
            return false;
        }
    }

    public static List<LevelData> GetAvailableLevels(GameSaveData saveData)
    {
        HashSet<string> unlocked = new HashSet<string>();
        if (saveData != null && saveData.unlockedLevelIds != null)
            unlocked.UnionWith(saveData.unlockedLevelIds);

        return LoadAll()
            .Where(level => IsCustomLevel(level) || unlocked.Contains(level.levelId))
            .ToList();
    }

    public static List<LevelData> LoadBundledLevels()
    {
        return Resources.LoadAll<LevelDefinition>(BundledLevelsPath)
            .Select(LevelData.FromDefinition)
            .Where(level => level != null)
            .OrderBy(level => level.progressionOrder)
            .ToList();
    }

    public static bool IsCustomLevel(LevelData level)
    {
        return level != null &&
               (level.customLevelNumber > 0 || level.progressionOrder >= 1000);
    }

    private static string GetCustomLevelId(int number)
    {
        return $"custom-level-{number}";
    }

    private static void LoadCatalogFile(
        Dictionary<string, LevelData> levelsById,
        string path,
        out bool suppressBundledCustomLevels)
    {
        suppressBundledCustomLevels = false;
        if (!File.Exists(path))
            return;

        try
        {
            LoadCatalogJson(
                levelsById,
                File.ReadAllText(path),
                path,
                out suppressBundledCustomLevels);
        }
        catch (IOException exception)
        {
            Debug.LogError($"Could not read level catalog '{path}': {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError($"Could not read level catalog '{path}': {exception.Message}");
        }
    }

    private static void LoadCatalogJson(
        Dictionary<string, LevelData> levelsById,
        string json,
        string source,
        out bool suppressBundledCustomLevels)
    {
        suppressBundledCustomLevels = false;
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError($"Level catalog '{source}' is empty.");
            return;
        }

        try
        {
            LevelCatalogData catalog = JsonConvert.DeserializeObject<LevelCatalogData>(json);
            if (catalog == null || catalog.formatVersion != 1 || catalog.levels == null)
            {
                Debug.LogError($"Level catalog '{source}' has an unsupported or invalid format.");
                return;
            }

            suppressBundledCustomLevels = catalog.suppressBundledCustomLevels;
            foreach (LevelJsonData serializedLevel in catalog.levels)
            {
                if (serializedLevel == null)
                    continue;

                LevelData level = serializedLevel.ToLevel(out string error);
                if (level == null)
                {
                    Debug.LogError($"Could not parse a level in '{source}': {error}");
                    continue;
                }

                NormalizeLegacyCustomLevel(level, levelsById);
                AddLevel(levelsById, level, source);
            }
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"Could not parse level catalog '{source}': {exception.Message}");
        }
        catch (JsonException exception)
        {
            Debug.LogError($"Could not parse level catalog '{source}': {exception.Message}");
        }
    }

    private static void LoadLegacyDirectory(
        Dictionary<string, LevelData> levelsById,
        string directory)
    {
        if (!Directory.Exists(directory))
            return;

        try
        {
            string[] files = Directory.GetFiles(directory, "*.json");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                try
                {
                    LevelJsonData serializedLevel =
                        JsonConvert.DeserializeObject<LevelJsonData>(File.ReadAllText(file));
                    if (serializedLevel == null)
                    {
                        Debug.LogError($"Legacy level file '{file}' contains no level data.");
                        continue;
                    }

                    LevelData level = serializedLevel.ToLevel(out string error);
                    if (level == null)
                    {
                        Debug.LogError($"Could not parse legacy level '{file}': {error}");
                        continue;
                    }

                    NormalizeLegacyCustomLevel(level, levelsById);
                    AddLevel(levelsById, level, file);
                }
                catch (ArgumentException exception)
                {
                    Debug.LogError($"Could not parse legacy level '{file}': {exception.Message}");
                }
                catch (JsonException exception)
                {
                    Debug.LogError($"Could not parse legacy level '{file}': {exception.Message}");
                }
            }
        }
        catch (IOException exception)
        {
            Debug.LogError($"Could not read legacy levels from '{directory}': {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError($"Could not read legacy levels from '{directory}': {exception.Message}");
        }
    }

    private static void NormalizeLegacyCustomLevel(
        LevelData level,
        Dictionary<string, LevelData> levelsById)
    {
        if (!IsCustomLevel(level) || level.customLevelNumber > 0)
            return;

        level.customLevelNumber = ParseCustomLevelNumber(level.levelId);
        if (level.customLevelNumber < 1)
            level.customLevelNumber = GetNextCustomLevelNumber(levelsById);
        level.levelId = GetCustomLevelId(level.customLevelNumber);
        level.displayName = $"Level {level.customLevelNumber}";
        level.progressionOrder = 1000 + level.customLevelNumber;
    }

    private static void AddLevel(
        Dictionary<string, LevelData> levelsById,
        LevelData level,
        string source)
    {
        LevelValidationResult validation = LevelValidator.Validate(level);
        if (!validation.IsValid)
        {
            Debug.LogError($"Ignoring invalid level from '{source}': {validation.Message}");
            return;
        }

        if (levelsById.ContainsKey(level.levelId))
        {
            Debug.LogWarning($"Ignoring duplicate level ID '{level.levelId}' from '{source}'.");
            return;
        }

        if (IsCustomLevel(level) &&
            levelsById.Values.Any(existing =>
                IsCustomLevel(existing) &&
                existing.customLevelNumber == level.customLevelNumber))
        {
            Debug.LogWarning(
                $"Ignoring duplicate custom level number {level.customLevelNumber} from '{source}'.");
            return;
        }

        levelsById.Add(level.levelId, level);
    }

    private static int ParseCustomLevelNumber(string levelId)
    {
        const string prefix = "custom-level-";
        if (string.IsNullOrWhiteSpace(levelId) ||
            !levelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return 0;

        return int.TryParse(levelId.Substring(prefix.Length), out int number) && number > 0
            ? number
            : 0;
    }

    private static int GetNextCustomLevelNumber(Dictionary<string, LevelData> levelsById)
    {
        HashSet<int> usedNumbers = new HashSet<int>(
            levelsById.Values
                .Where(IsCustomLevel)
                .Select(level => level.customLevelNumber));
        for (int number = 1; number <= 999; number++)
        {
            if (!usedNumbers.Contains(number))
                return number;
        }

        throw new InvalidOperationException("No custom level numbers remain.");
    }

    private static void ClearLegacyLevelFiles(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        string[] files = Directory.GetFiles(directory, "*.json");
        foreach (string file in files)
            File.Delete(file);
    }

    private static void WriteCatalogAtomically(string path, string contents)
    {
        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, contents);

        if (File.Exists(path))
        {
            string backupPath = path + "." + Guid.NewGuid().ToString("N") + ".bak";
            File.Replace(temporaryPath, path, backupPath);
            try
            {
                File.Delete(backupPath);
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"Saved the level catalog, but could not remove backup '{backupPath}': {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogWarning($"Saved the level catalog, but could not remove backup '{backupPath}': {exception.Message}");
            }
        }
        else
            File.Move(temporaryPath, path);
    }
}
