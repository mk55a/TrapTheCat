using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TrapTheCat.Services
{
    public sealed class JsonFileSaveService : ISaveService
    {
        private readonly string _filePath;

        public JsonFileSaveService(string filePath)
        {
            _filePath = filePath;
        }

        public static string DefaultFilePath
        {
            get { return Path.Combine(Application.persistentDataPath, "trap-the-cat-save.json"); }
        }

        public SaveData Load()
        {
            if (!File.Exists(_filePath))
                return new SaveData();

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(_filePath));
                if (data == null)
                {
                    Debug.LogError($"Save file '{_filePath}' does not contain valid game data.");
                    return new SaveData();
                }

                if (data.unlockedLevelIds == null)
                    data.unlockedLevelIds = new List<string>();
                if (data.completedLevelIds == null)
                    data.completedLevelIds = new List<string>();
                return data;
            }
            catch (IOException exception)
            {
                Debug.LogError($"Could not read save file '{_filePath}': {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogError($"Could not read save file '{_filePath}': {exception.Message}");
            }
            return new SaveData();
        }

        public void Save(SaveData data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
                File.WriteAllText(_filePath, JsonUtility.ToJson(data, true));
            }
            catch (IOException exception)
            {
                Debug.LogError($"Could not write save file '{_filePath}': {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogError($"Could not write save file '{_filePath}': {exception.Message}");
            }
        }
    }
}
