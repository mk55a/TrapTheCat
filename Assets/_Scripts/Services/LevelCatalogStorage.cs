using System;
using System.IO;
using UnityEngine;

namespace TrapTheCat.Services
{
    public sealed class LevelCatalogStorage
    {
        private const string CatalogFileName = "levelData.json";

        private readonly string _bundledCatalogPath;
        private readonly string _bundledCatalogJson;
        private readonly string _userCatalogPath;
        private readonly string _saveCatalogPath;
        private readonly string[] _legacyLevelDirectories;
        private readonly bool _userCatalogCanHideBundledCustomLevels;

        public LevelCatalogStorage(
            string bundledCatalogPath,
            string bundledCatalogJson,
            string userCatalogPath,
            string saveCatalogPath,
            string[] legacyLevelDirectories,
            bool userCatalogCanHideBundledCustomLevels)
        {
            _bundledCatalogPath = bundledCatalogPath;
            _bundledCatalogJson = bundledCatalogJson;
            _userCatalogPath = userCatalogPath;
            _saveCatalogPath = saveCatalogPath;
            _legacyLevelDirectories = legacyLevelDirectories ?? new string[0];
            _userCatalogCanHideBundledCustomLevels = userCatalogCanHideBundledCustomLevels;
        }

        public static LevelCatalogStorage ForCurrentPlatform(TextAsset bundledCatalog)
        {
            string userCatalogPath = Path.Combine(Application.persistentDataPath, CatalogFileName);
            string userLegacyDirectory = Path.Combine(Application.persistentDataPath, "Levels");
#if UNITY_EDITOR
            string projectCatalogPath = Path.Combine(Application.dataPath, "Resources", "Levels", CatalogFileName);
            string projectLegacyDirectory = Path.Combine(Application.dataPath, "Resources", "Levels", "Custom");
            return new LevelCatalogStorage(
                projectCatalogPath,
                null,
                userCatalogPath,
                projectCatalogPath,
                new[] { projectLegacyDirectory, userLegacyDirectory },
                false);
#else
            return new LevelCatalogStorage(
                null,
                bundledCatalog != null ? bundledCatalog.text : null,
                userCatalogPath,
                userCatalogPath,
                new[] { userLegacyDirectory },
                true);
#endif
        }

        public string SaveLocation
        {
            get { return _saveCatalogPath; }
        }

        public bool UserCatalogCanHideBundledCustomLevels
        {
            get { return _userCatalogCanHideBundledCustomLevels; }
        }

        public bool HasAnyCatalog
        {
            get
            {
                bool hasBundled = _bundledCatalogPath != null
                    ? File.Exists(_bundledCatalogPath)
                    : _bundledCatalogJson != null;
                return hasBundled || File.Exists(_userCatalogPath);
            }
        }

        public LevelCatalogFile ReadBundledCatalog()
        {
            if (_bundledCatalogPath != null)
                return ReadCatalogFile(_bundledCatalogPath);
            return _bundledCatalogJson != null
                ? ParseCatalog(_bundledCatalogJson, "the bundled level catalog")
                : null;
        }

        public LevelCatalogFile ReadUserCatalog()
        {
            return ReadCatalogFile(_userCatalogPath);
        }

        public string UserCatalogName
        {
            get { return _userCatalogPath; }
        }

        public string BundledCatalogName
        {
            get { return _bundledCatalogPath ?? "the bundled level catalog"; }
        }

        public void ReadLegacyLevelFiles(Action<LevelFileEntry, string> onLevelRead)
        {
            foreach (string directory in _legacyLevelDirectories)
            {
                if (!Directory.Exists(directory))
                    continue;

                try
                {
                    string[] files = Directory.GetFiles(directory, "*.json");
                    Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                    foreach (string file in files)
                        ReadLegacyLevelFile(file, onLevelRead);
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
        }

        public void WriteSaveCatalog(LevelCatalogFile catalog)
        {
            WriteCatalog(_saveCatalogPath, JsonUtility.ToJson(catalog, true));
        }

        public void WriteEmptyCatalogs()
        {
            foreach (string directory in _legacyLevelDirectories)
                DeleteLegacyLevelFiles(directory);

            LevelCatalogFile empty = new LevelCatalogFile { suppressBundledCustomLevels = true };
            string json = JsonUtility.ToJson(empty, true);
            WriteCatalog(_saveCatalogPath, json);
            if (_userCatalogPath != _saveCatalogPath)
                WriteCatalog(_userCatalogPath, json);
        }

        private static LevelCatalogFile ReadCatalogFile(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                return ParseCatalog(File.ReadAllText(path), path);
            }
            catch (IOException exception)
            {
                Debug.LogError($"Could not read level catalog '{path}': {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogError($"Could not read level catalog '{path}': {exception.Message}");
            }
            return null;
        }

        private static LevelCatalogFile ParseCatalog(string json, string source)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError($"Level catalog '{source}' is empty.");
                return null;
            }

            try
            {
                LevelCatalogFile catalog = JsonUtility.FromJson<LevelCatalogFile>(json);
                if (catalog == null ||
                    catalog.formatVersion != LevelCatalogFile.CurrentFormatVersion ||
                    catalog.levels == null)
                {
                    Debug.LogError($"Level catalog '{source}' has an unsupported or invalid format.");
                    return null;
                }
                return catalog;
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"Could not parse level catalog '{source}': {exception.Message}");
                return null;
            }
        }

        private static void ReadLegacyLevelFile(string file, Action<LevelFileEntry, string> onLevelRead)
        {
            try
            {
                LevelFileEntry entry = JsonUtility.FromJson<LevelFileEntry>(File.ReadAllText(file));
                if (entry == null)
                {
                    Debug.LogError($"Legacy level file '{file}' contains no level data.");
                    return;
                }
                onLevelRead(entry, file);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"Could not parse legacy level '{file}': {exception.Message}");
            }
        }

        private static void DeleteLegacyLevelFiles(string directory)
        {
            if (!Directory.Exists(directory))
                return;

            foreach (string file in Directory.GetFiles(directory, "*.json"))
                File.Delete(file);
        }

        private static void WriteCatalog(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
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
            {
                File.Move(temporaryPath, path);
            }

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
