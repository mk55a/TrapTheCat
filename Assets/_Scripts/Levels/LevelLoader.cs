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

    public bool Load(LevelDefinition level)
    {
        if (!Validate(level))
            return false;

        _catMovement.ResetMovement();
        _gridManager.InitiateGrid(level.width, level.height);
        if (_gridManager.grid == null)
            return false;
        foreach (Vector2Int cell in level.blockedCells)
            _gridManager.SetCellType(cell.x, cell.y, GridType.Obstacle);

        _cat.position = _gridManager.GetCenterGridPosition(level.catStart.x, level.catStart.y);
        _gridManager.SetCatPosition(level.catStart.x, level.catStart.y);
        _catMovement.RefreshPathfinding();
        return true;
    }

    private bool Validate(LevelDefinition level)
    {
        if (level == null)
        {
            Debug.LogError("Cannot load a null level definition.");
            return false;
        }
        if (_gridManager == null || _cat == null || _catMovement == null)
        {
            Debug.LogError($"Level '{level.name}' cannot load because the grid or cat references are missing.");
            return false;
        }
        if (level.blockedCells == null)
        {
            Debug.LogError($"Level '{level.name}' has no blocked-cell list.", level);
            return false;
        }
        if (level.width < 3 || level.height < 3 ||
            level.catStart.x <= 0 || level.catStart.y <= 0 ||
            level.catStart.x >= level.width - 1 || level.catStart.y >= level.height - 1)
        {
            Debug.LogError($"Level '{level.name}' has invalid dimensions or a cat start outside the playable area.");
            return false;
        }

        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        foreach (Vector2Int cell in level.blockedCells)
        {
            bool isInside = cell.x > 0 && cell.y > 0 &&
                            cell.x < level.width - 1 && cell.y < level.height - 1;
            if (!isInside || cell == level.catStart || !occupied.Add(cell))
            {
                Debug.LogError($"Level '{level.name}' has a blocked cell that is outside the interior, duplicated, or overlaps the cat at ({cell.x}, {cell.y}).");
                return false;
            }
        }

        return true;
    }
}
