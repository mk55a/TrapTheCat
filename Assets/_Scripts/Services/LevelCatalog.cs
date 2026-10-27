using System;
using System.Collections.Generic;
using System.IO;
using TrapTheCat.Core;
using UnityEngine;

namespace TrapTheCat.Services
{
    public sealed class LevelCatalog
    {
        private const string CustomLevelIdPrefix = "custom-level-";
        private const int FirstCustomProgressionOrder = 1000;
        private const int MaxCustomLevelNumber = 999;

        private readonly List<LevelData> _bundledLevels;
        private readonly LevelCatalogStorage _storage;
        private readonly BoardShapeList _shapes;
        private List<LevelData> _levels = new List<LevelData>();

        public LevelCatalog(List<LevelData> bundledLevels, LevelCatalogStorage storage, BoardShapeList shapes)
        {
            _bundledLevels = bundledLevels ?? new List<LevelData>();
            _storage = storage;
            _shapes = shapes;
            Reload();
        }

        public IReadOnlyList<LevelData> Levels
        {
            get { return _levels; }
        }

        public IReadOnlyList<LevelData> BundledLevels
        {
            get { return _bundledLevels; }
        }

        public bool HasCustomLevels
        {
            get { return _levels.Exists(level => level.IsCustom); }
        }

        public LevelData FindById(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
                return null;
            return _levels.Find(level => level.levelId == levelId);
        }

        public bool HasCustomLevelNumber(int levelNumber)
        {
            return _levels.Exists(level => level.IsCustom && level.customLevelNumber == levelNumber);
        }

        public void Reload()
        {
            _levels = new LevelCatalogLoader(_bundledLevels, _storage, _shapes).Load();
        }

        public bool SaveCustomLevel(LevelData level, out string error)
        {
            error = null;
            if (level == null || level.customLevelNumber < 1 || level.customLevelNumber > MaxCustomLevelNumber)
            {
                error = "Enter a level number from 1 to 999.";
                return false;
            }

            AssignCustomIdentity(level, level.customLevelNumber);
            LevelValidationResult validation = LevelValidator.ValidateWinnable(level);
            if (!validation.IsValid)
            {
                error = validation.Message;
                return false;
            }

            List<LevelData> customLevels = _levels.FindAll(existing =>
                existing.IsCustom && existing.customLevelNumber != level.customLevelNumber);
            customLevels.Add(level);
            customLevels.Sort((a, b) => a.customLevelNumber.CompareTo(b.customLevelNumber));

            LevelCatalogFile catalog = new LevelCatalogFile { suppressBundledCustomLevels = true };
            foreach (LevelData customLevel in customLevels)
                catalog.levels.Add(LevelFileEntry.FromLevel(customLevel));

            if (!TryWrite(() => _storage.WriteSaveCatalog(catalog), "Could not save the level catalog", out error))
                return false;

            Debug.Log($"Saved {level.displayName} to '{_storage.SaveLocation}'.");
            Reload();
            return true;
        }

        public bool ClearCustomLevels(out string error)
        {
            if (!TryWrite(_storage.WriteEmptyCatalogs, "Could not clear custom levels", out error))
                return false;

            Debug.Log("Cleared all custom levels. Bundled levels and player progress were kept.");
            Reload();
            return true;
        }

        public static void AssignCustomIdentity(LevelData level, int customLevelNumber)
        {
            level.customLevelNumber = customLevelNumber;
            level.levelId = CustomLevelIdPrefix + customLevelNumber;
            level.displayName = $"Level {customLevelNumber}";
            level.progressionOrder = FirstCustomProgressionOrder + customLevelNumber;
        }

        public static int ParseCustomLevelNumber(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId) ||
                !levelId.StartsWith(CustomLevelIdPrefix, StringComparison.OrdinalIgnoreCase))
                return 0;

            return int.TryParse(levelId.Substring(CustomLevelIdPrefix.Length), out int number) && number > 0
                ? number
                : 0;
        }

        private static bool TryWrite(Action write, string failureMessage, out string error)
        {
            error = null;
            try
            {
                write();
                return true;
            }
            catch (IOException exception)
            {
                error = $"{failureMessage}: {exception.Message}";
            }
            catch (UnauthorizedAccessException exception)
            {
                error = $"{failureMessage}: {exception.Message}";
            }
            catch (UnityException exception)
            {
                error = $"The file was written, but Unity could not refresh its asset database: {exception.Message}";
            }
            return false;
        }
    }
}
