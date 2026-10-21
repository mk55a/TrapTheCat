using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class PlayableLevelData
{
    public int formatVersion = 1;
    public int width = 10;
    public int height = 10;
    public string boardShape = "Hexagon";
    public int catStartX = 4;
    public int catStartY = 5;
    public string catDifficulty = "Normal";
    public List<PlayableCellData> blockedCells = new List<PlayableCellData>();
        public PlayableCellData tutorialCell;
    }

[Serializable]
public class PlayableCellData
{
    public int x;
    public int y;
}

public static class PlayableLevelLoader
{
    public static bool Load(
        TextAsset levelFile,
        GridManager gridManager,
        Transform cat,
        CatMovement catMovement,
        out PlayableCellData tutorialCell)
    {
        tutorialCell = null;
        if (levelFile == null)
        {
            Debug.LogError("Playable level JSON TextAsset is not assigned.");
            return false;
        }

        PlayableLevelData level;
        try
        {
            level = JsonConvert.DeserializeObject<PlayableLevelData>(levelFile.text);
        }
        catch (JsonException exception)
        {
            Debug.LogError($"Could not parse playable level '{levelFile.name}': {exception.Message}");
            return false;
        }

        if (!Validate(level, gridManager))
            return false;

        Enum.TryParse(level.boardShape, true, out BoardShape shape);
        Enum.TryParse(level.catDifficulty, true, out CatDifficulty difficulty);
        catMovement.ResetMovement();
        gridManager.InitiateGrid(level.width, level.height, shape);
        if (gridManager.grid == null)
        {
            Debug.LogError($"Could not create the playable board from '{levelFile.name}'.");
            return false;
        }

        foreach (PlayableCellData blockedCell in level.blockedCells)
            gridManager.SetCellType(blockedCell.x, blockedCell.y, GridType.Obstacle);

        Vector3 catPosition = gridManager.GetCenterGridPosition(level.catStartX, level.catStartY);
        cat.position = catPosition;
        gridManager.SetCatPosition(level.catStartX, level.catStartY);
        catMovement.SetDifficulty(difficulty);
        catMovement.RefreshPathfinding();
        if (level.tutorialCell != null &&
            BoardTopology.IsActive(level.width, level.height, shape, level.tutorialCell.x, level.tutorialCell.y))
            tutorialCell = level.tutorialCell;
        return true;
    }

    private static bool Validate(PlayableLevelData level, GridManager gridManager)
    {
        if (level == null || level.formatVersion != 1 ||
            level.width < 3 || level.height < 3 ||
            !Enum.TryParse(level.boardShape, true, out BoardShape shape) ||
            !Enum.IsDefined(typeof(BoardShape), shape) ||
            !Enum.TryParse(level.catDifficulty, true, out CatDifficulty difficulty) ||
            !Enum.IsDefined(typeof(CatDifficulty), difficulty) ||
            gridManager == null)
        {
            Debug.LogError("Playable level JSON has an unsupported format or invalid board settings.");
            return false;
        }

        if (level.blockedCells == null ||
            !BoardTopology.IsActive(level.width, level.height, shape, level.catStartX, level.catStartY) ||
            BoardTopology.IsBoundary(
                BoardTopology.BuildMask(level.width, level.height, shape),
                level.catStartX,
                level.catStartY))
        {
            Debug.LogError("Playable level JSON has an invalid cat start or blocked-cell list.");
            return false;
        }

        foreach (PlayableCellData cell in level.blockedCells)
        {
            if (cell == null ||
                !BoardTopology.IsActive(level.width, level.height, shape, cell.x, cell.y) ||
                (cell.x == level.catStartX && cell.y == level.catStartY))
            {
                Debug.LogError("Playable level JSON contains a blocked cell outside the board or under the cat.");
                return false;
            }
        }

        return true;
    }
}
