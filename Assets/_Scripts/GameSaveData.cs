using System;
using System.IO;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int playCount;
    public bool hasSeenTutorial;
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
            return new GameSaveData();

        try
        {
            string json = File.ReadAllText(SavePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null)
            {
                Debug.LogError($"Save file '{SavePath}' does not contain valid game data.");
                return new GameSaveData();
            }

            return data;
        }
        catch (IOException exception)
        {
            Debug.LogError($"Could not read save file '{SavePath}': {exception.Message}");
            return new GameSaveData();
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError($"Could not read save file '{SavePath}': {exception.Message}");
            return new GameSaveData();
        }
    }

    public static void Save(GameSaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
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
    }
}
