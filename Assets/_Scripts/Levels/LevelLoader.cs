using System.Collections.Generic;
using UnityEngine;

public class LevelLoader
{
    private readonly GridManager _gridManager;
    private readonly Transform _cat;
    private readonly CatMovement _catMovement;

    public LevelLoader(GridManager gridManager, Transform cat, CatMovement catMovement)
    {
        _gridManager = gridManager;
        _cat = cat;
        _catMovement = catMovement;
    }

    public bool Load(LevelDefinition definition)
    {
        return Load(LevelData.FromDefinition(definition));
    }

    public bool Load(LevelData level)
    {
        if (!Validate(level))
            return false;

        _catMovement.ResetMovement();
        _gridManager.InitiateGrid(level.width, level.height, level.boardShape);
        if (_gridManager.grid == null)
            return false;
        foreach (Vector2Int cell in level.blockedCells)
            _gridManager.SetCellType(cell.x, cell.y, GridType.Obstacle);

        _cat.position = _gridManager.GetCenterGridPosition(level.catStart.x, level.catStart.y);
        _gridManager.SetCatPosition(level.catStart.x, level.catStart.y);
        _catMovement.SetDifficulty(level.catDifficulty);
        _catMovement.RefreshPathfinding();
        return true;
    }

    private bool Validate(LevelData level)
    {
        if (level == null)
        {
            Debug.LogError("Cannot load a null level definition.");
            return false;
        }
        if (_gridManager == null || _cat == null || _catMovement == null)
        {
            Debug.LogError($"Level '{level.displayName}' cannot load because the grid or cat references are missing.");
            return false;
        }
        LevelValidationResult result = LevelValidator.Validate(level);
        if (!result.IsValid)
            Debug.LogError($"Level '{level.displayName}' is invalid: {result.Message}");
        return result.IsValid;
    }
}
