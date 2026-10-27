using System;
using System.Collections.Generic;
using TrapTheCat.Core;
using UnityEngine;

namespace TrapTheCat.Services
{
    [Serializable]
    public class LevelFileEntry
    {
        public int formatVersion = 1;
        public string levelId;
        public string displayName;
        public int progressionOrder;
        public int customLevelNumber;
        public string boardShape;
        public int width;
        public int height;
        public bool hasCatStart;
        public Vector2Int catStart;
        public string catDifficulty;
        public List<Vector2Int> blockedCells = new List<Vector2Int>();
        public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

        public static LevelFileEntry FromLevel(LevelData level)
        {
            return new LevelFileEntry
            {
                formatVersion = level.formatVersion,
                levelId = level.levelId,
                displayName = level.displayName,
                progressionOrder = level.progressionOrder,
                customLevelNumber = level.customLevelNumber,
                boardShape = level.boardShape.Id,
                width = level.width,
                height = level.height,
                hasCatStart = level.hasCatStart,
                catStart = level.catStart,
                catDifficulty = level.catDifficulty.ToString(),
                blockedCells = level.blockedCells != null
                    ? new List<Vector2Int>(level.blockedCells)
                    : new List<Vector2Int>(),
                tutorialSteps = level.tutorialSteps != null
                    ? new List<TutorialStep>(level.tutorialSteps)
                    : new List<TutorialStep>()
            };
        }

        public LevelData ToLevel(BoardShapeList shapes, out string error)
        {
            error = null;
            BoardShape parsedShape = shapes.FindById(boardShape);
            if (parsedShape == null)
            {
                error = $"Unknown board shape '{boardShape}'.";
                return null;
            }
            if (!Enum.TryParse(catDifficulty, true, out CatDifficulty parsedDifficulty) ||
                !Enum.IsDefined(typeof(CatDifficulty), parsedDifficulty))
            {
                error = $"Unknown cat difficulty '{catDifficulty}'.";
                return null;
            }

            return new LevelData
            {
                formatVersion = formatVersion,
                levelId = levelId,
                displayName = displayName,
                progressionOrder = progressionOrder,
                customLevelNumber = customLevelNumber,
                boardShape = parsedShape,
                width = width,
                height = height,
                hasCatStart = hasCatStart,
                catStart = catStart,
                catDifficulty = parsedDifficulty,
                blockedCells = blockedCells,
                tutorialSteps = tutorialSteps ?? new List<TutorialStep>()
            };
        }
    }
}
