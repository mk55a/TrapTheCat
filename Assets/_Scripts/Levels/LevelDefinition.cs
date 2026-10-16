using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Trap The Cat/Level")]
public class LevelDefinition : ScriptableObject
{
    public string levelId = "level-1";
    public string displayName = "First Steps";
    [Min(3)] public int width = 16;
    [Min(3)] public int height = 10;
    public Vector2Int catStart = new Vector2Int(8, 5);
    public List<Vector2Int> blockedCells = new List<Vector2Int>();
    public List<TutorialStep> tutorialSteps = new List<TutorialStep>();
}

[Serializable]
public class TutorialStep
{
    [Min(0)] public int afterPlayerMoves;
    [TextArea(2, 4)] public string message;
}
