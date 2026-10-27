using System;
using System.Collections.Generic;
using TrapTheCat.Core;
using UnityEngine;

namespace TrapTheCat.Services
{
    public sealed class LevelCatalogLoader
    {
        private readonly List<LevelData> _bundledLevels;
        private readonly LevelCatalogStorage _storage;
        private readonly BoardShapeList _shapes;
        private readonly Dictionary<string, LevelData> _levelsById = new Dictionary<string, LevelData>();

        public LevelCatalogLoader(List<LevelData> bundledLevels, LevelCatalogStorage storage, BoardShapeList shapes)
        {
            _bundledLevels = bundledLevels;
            _storage = storage;
            _shapes = shapes;
        }

        public List<LevelData> Load()
        {
            _levelsById.Clear();
            foreach (LevelData level in _bundledLevels)
                AddLevel(_levelsById, level, "bundled level asset");

            AddCatalog(_levelsById, _storage.ReadBundledCatalog(), _storage.BundledCatalogName);

            LevelCatalogFile userCatalog = _storage.ReadUserCatalog();
            Dictionary<string, LevelData> userLevels = new Dictionary<string, LevelData>();
            AddCatalog(userLevels, userCatalog, _storage.UserCatalogName);
            if (userCatalog != null && userCatalog.suppressBundledCustomLevels &&
                _storage.UserCatalogCanHideBundledCustomLevels)
                RemoveCustomLevels(_levelsById);
            foreach (LevelData level in userLevels.Values)
                AddLevel(_levelsById, level, _storage.UserCatalogName);

            bool hasCustomLevels = false;
            foreach (LevelData level in _levelsById.Values)
                hasCustomLevels |= level.IsCustom;
            if (!hasCustomLevels && !_storage.HasAnyCatalog)
                _storage.ReadLegacyLevelFiles((entry, file) => AddEntry(_levelsById, entry, file));

            return Sorted(_levelsById.Values);
        }

        private void AddCatalog(Dictionary<string, LevelData> levelsById, LevelCatalogFile catalog, string source)
        {
            if (catalog == null)
                return;

            foreach (LevelFileEntry entry in catalog.levels)
            {
                if (entry != null)
                    AddEntry(levelsById, entry, source);
            }
        }

        private void AddEntry(Dictionary<string, LevelData> levelsById, LevelFileEntry entry, string source)
        {
            LevelData level = entry.ToLevel(_shapes, out string error);
            if (level == null)
            {
                Debug.LogError($"Could not parse a level in '{source}': {error}");
                return;
            }

            GiveLegacyCustomLevelANumber(level, levelsById);
            AddLevel(levelsById, level, source);
        }

        private static void GiveLegacyCustomLevelANumber(LevelData level, Dictionary<string, LevelData> levelsById)
        {
            if (!level.IsCustom || level.customLevelNumber > 0)
                return;

            int number = LevelCatalog.ParseCustomLevelNumber(level.levelId);
            if (number < 1)
                number = FirstFreeCustomLevelNumber(levelsById);
            LevelCatalog.AssignCustomIdentity(level, number);
        }

        private static void AddLevel(Dictionary<string, LevelData> levelsById, LevelData level, string source)
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

            if (level.IsCustom && UsesCustomLevelNumber(levelsById, level.customLevelNumber))
            {
                Debug.LogWarning($"Ignoring duplicate custom level number {level.customLevelNumber} from '{source}'.");
                return;
            }

            levelsById.Add(level.levelId, level);
        }

        private static bool UsesCustomLevelNumber(Dictionary<string, LevelData> levelsById, int number)
        {
            foreach (LevelData existing in levelsById.Values)
            {
                if (existing.IsCustom && existing.customLevelNumber == number)
                    return true;
            }
            return false;
        }

        private static int FirstFreeCustomLevelNumber(Dictionary<string, LevelData> levelsById)
        {
            for (int number = 1; number <= 999; number++)
            {
                if (!UsesCustomLevelNumber(levelsById, number))
                    return number;
            }
            throw new InvalidOperationException("No custom level numbers remain.");
        }

        private static void RemoveCustomLevels(Dictionary<string, LevelData> levelsById)
        {
            List<string> customIds = new List<string>();
            foreach (LevelData level in levelsById.Values)
            {
                if (level.IsCustom)
                    customIds.Add(level.levelId);
            }
            foreach (string levelId in customIds)
                levelsById.Remove(levelId);
        }

        private static List<LevelData> Sorted(IEnumerable<LevelData> levels)
        {
            List<LevelData> sorted = new List<LevelData>(levels);
            sorted.Sort((a, b) =>
            {
                int byKind = (a.IsCustom ? 1 : 0).CompareTo(b.IsCustom ? 1 : 0);
                if (byKind != 0)
                    return byKind;

                int orderA = a.IsCustom ? a.customLevelNumber : a.progressionOrder;
                int orderB = b.IsCustom ? b.customLevelNumber : b.progressionOrder;
                if (orderA != orderB)
                    return orderA.CompareTo(orderB);

                return StringComparer.OrdinalIgnoreCase.Compare(a.displayName, b.displayName);
            });
            return sorted;
        }
    }
}
