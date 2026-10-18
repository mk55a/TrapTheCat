using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int playCount;
    public bool hasSeenTutorial;
    public List<string> unlockedLevelIds = new List<string>();
    public List<string> completedLevelIds = new List<string>();
}

public static class GameSaveSystem
{
    private static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, "trap-the-cat-save.json"); }
    }

    public static GameSaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            GameSaveData newData = new GameSaveData();
            Normalize(newData);
            return newData;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            GameSaveData data = JsonConvert.DeserializeObject<GameSaveData>(json);
            if (data == null)
            {
                Debug.LogError($"Save file '{SavePath}' does not contain valid game data.");
                data = new GameSaveData();
            }

            Normalize(data);
            return data;
        }
        catch (IOException exception)
        {
            Debug.LogError($"Could not read save file '{SavePath}': {exception.Message}");
            GameSaveData newData = new GameSaveData();
            Normalize(newData);
            return newData;
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError($"Could not read save file '{SavePath}': {exception.Message}");
            GameSaveData newData = new GameSaveData();
            Normalize(newData);
            return newData;
        }
        catch (JsonException exception)
        {
            Debug.LogError($"Save file '{SavePath}' contains invalid JSON: {exception.Message}");
            GameSaveData newData = new GameSaveData();
            Normalize(newData);
            return newData;
        }
    }

    public static void Save(GameSaveData data)
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(SavePath, json);
        }
        catch (IOException exception)
        {
            Debug.LogError($"Could not write save file '{SavePath}': {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError($"Could not write save file '{SavePath}': {exception.Message}");
        }
        catch (JsonException exception)
        {
            Debug.LogError($"Could not serialize save file '{SavePath}': {exception.Message}");
        }
    }

    private static void Normalize(GameSaveData data)
    {
        if (data.unlockedLevelIds == null)
            data.unlockedLevelIds = new List<string>();
        if (data.completedLevelIds == null)
            data.completedLevelIds = new List<string>();

        List<LevelData> bundledLevels = LevelRepository.LoadBundledLevels();
        if (bundledLevels.Count > 0 &&
            !data.unlockedLevelIds.Contains(bundledLevels[0].levelId))
            data.unlockedLevelIds.Add(bundledLevels[0].levelId);
    }
}

    public static class LevelLaunchSelection
    {
        public static string LevelId { get; set; }

        public static string Consume()
        {
            string selected = LevelId;
            LevelId = null;
            return selected;
        }
    }

    public static class GameProgression
    {
        public static void RecordWin(GameSaveData saveData, string completedLevelId)
        {
            if (saveData == null || string.IsNullOrWhiteSpace(completedLevelId))
                return;

            if (saveData.completedLevelIds == null)
                saveData.completedLevelIds = new List<string>();
            if (saveData.unlockedLevelIds == null)
                saveData.unlockedLevelIds = new List<string>();

            if (!saveData.completedLevelIds.Contains(completedLevelId))
                saveData.completedLevelIds.Add(completedLevelId);

            List<LevelData> bundledLevels = LevelRepository.LoadBundledLevels();
            for (int i = 0; i < bundledLevels.Count - 1; i++)
            {
                if (bundledLevels[i].levelId != completedLevelId)
                    continue;

                string nextLevelId = bundledLevels[i + 1].levelId;
                if (!saveData.unlockedLevelIds.Contains(nextLevelId))
                    saveData.unlockedLevelIds.Add(nextLevelId);
                break;
            }

            GameSaveSystem.Save(saveData);
        }
    }
