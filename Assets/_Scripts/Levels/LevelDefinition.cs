using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Trap The Cat/Level")]
public class LevelDefinition : ScriptableObject
{
    public string levelId = "level-1";
    public string displayName = "First Steps";
    [Min(0)] public int progressionOrder;
    public BoardShape boardShape = BoardShape.Rectangle;
    [Min(3)] public int width = 16;
    [Min(3)] public int height = 10;
    public bool hasCatStart = true;
    public Vector2Int catStart = new Vector2Int(8, 5);
    public CatDifficulty catDifficulty = CatDifficulty.Normal;
    public List<Vector2Int> blockedCells = new List<Vector2Int>();
    public List<TutorialStep> tutorialSteps = new List<TutorialStep>();
}

[Serializable]
public class LevelData
{
    public int formatVersion = 1;
    public string levelId = "custom-level";
    public string displayName = "Custom Level";
    public int progressionOrder;
    public int customLevelNumber;
    public BoardShape boardShape = BoardShape.Rectangle;
    public int width = 10;
    public int height = 10;
    public bool hasCatStart = true;
    public Vector2Int catStart = new Vector2Int(5, 5);
    public CatDifficulty catDifficulty = CatDifficulty.Normal;
    public List<Vector2Int> blockedCells = new List<Vector2Int>();
    public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    public static LevelData FromDefinition(LevelDefinition definition)
    {
        if (definition == null)
            return null;

        return new LevelData
        {
            formatVersion = 1,
            levelId = definition.levelId,
            displayName = definition.displayName,
            boardShape = definition.boardShape,
            progressionOrder = definition.progressionOrder,
            width = definition.width,
            height = definition.height,
            hasCatStart = definition.hasCatStart,
            catStart = definition.catStart,
            catDifficulty = definition.catDifficulty,
            blockedCells = definition.blockedCells != null
                ? new List<Vector2Int>(definition.blockedCells)
                : new List<Vector2Int>(),
            tutorialSteps = definition.tutorialSteps != null
                ? new List<TutorialStep>(definition.tutorialSteps)
                : new List<TutorialStep>()
        };
    }
}

[Serializable]
public class TutorialStep
{
    [Min(0)] public int afterPlayerMoves;
    [TextArea(2, 4)] public string message;
}
