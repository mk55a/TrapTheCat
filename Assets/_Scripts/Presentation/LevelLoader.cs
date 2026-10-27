using TrapTheCat.Core;
using UnityEngine;

namespace TrapTheCat.Presentation
{
    public class LevelLoader
    {
        private readonly GridManager _gridManager;
        private readonly CatView _catView;

        public LevelLoader(GridManager gridManager, CatView catView)
        {
            _gridManager = gridManager;
            _catView = catView;
        }

        public BoardState Load(LevelData level)
        {
            if (!Validate(level))
                return null;

            _catView.ResetForLevel();
            _gridManager.CreateGrid(BoardState.FromLevel(level), true);
            if (_gridManager.Board == null)
                return null;

            _catView.PlaceAt(level.catStart);
            PlayReveal(level);
            return _gridManager.Board;
        }

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
                TileView tile = _gridManager.GetTileAt(cell);
                if (tile != null)
                    tile.HoldVisualType(blockedDelay + i * stagger);
            }

            float catDelay = blockedDelay + level.blockedCells.Count * stagger + catGap;
            _catView.RevealAfter(catDelay);
        }

        private bool Validate(LevelData level)
        {
            if (level == null)
            {
                Debug.LogError("Cannot load a null level definition.");
                return false;
            }
            if (_gridManager == null || _catView == null)
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
}
