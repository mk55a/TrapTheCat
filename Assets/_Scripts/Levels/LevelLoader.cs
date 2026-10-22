using System.Collections;
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
        _gridManager.InitiateGrid(level.width, level.height, level.boardShape, true);
        if (_gridManager.grid == null)
            return false;
        foreach (Vector2Int cell in level.blockedCells)
            _gridManager.SetCellType(cell.x, cell.y, GridType.Obstacle);

        _cat.position = _gridManager.GetCenterGridPosition(level.catStart.x, level.catStart.y);
        _gridManager.SetCatPosition(level.catStart.x, level.catStart.y);
        _catMovement.SetDifficulty(level.catDifficulty);
        _catMovement.RefreshPathfinding();
        PlayReveal(level);
        return true;
    }

    // Board state is final immediately; only the visuals are staged:
    // empty tiles ripple in, blocked tiles appear after a pause, then the cat.
    private void PlayReveal(LevelData level)
    {
        const float blockedDelay = 1.5f;
        const float blockedStagger = 0.05f;
        const float maxStagger = 0.5f;
        const float catGap = 0.3f;

        float stagger = level.blockedCells.Count > 0
            ? Mathf.Min(blockedStagger, maxStagger / level.blockedCells.Count)
            : 0f;
        for (int i = 0; i < level.blockedCells.Count; i++)
        {
            Vector2Int cell = level.blockedCells[i];
            GridTile tile = _gridManager.GetTileAt(cell.x, cell.y);
            if (tile != null)
                tile.HoldVisualType(blockedDelay + i * stagger);
        }

        float catDelay = blockedDelay + level.blockedCells.Count * stagger + catGap;
        _gridManager.StartCoroutine(RevealCat(catDelay));
    }

    private IEnumerator RevealCat(float delay)
    {
        Renderer[] renderers = _cat.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.enabled = false;

        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        foreach (Renderer r in renderers)
        {
            if (r != null)
                r.enabled = true;
        }
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
