using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    [CreateAssetMenu(fileName = "New Level", menuName = "Trap The Cat/Level")]
    public class LevelDefinition : ScriptableObject
    {
        public string levelId = "level-1";
        public string displayName = "First Steps";
        [Min(0)] public int progressionOrder;
        public BoardShape boardShape;
        [Min(3)] public int width = 16;
        [Min(3)] public int height = 10;
        public bool hasCatStart = true;
        public Vector2Int catStart = new Vector2Int(8, 5);
        public CatDifficulty catDifficulty = CatDifficulty.Normal;
        public List<Vector2Int> blockedCells = new List<Vector2Int>();
        public List<TutorialStep> tutorialSteps = new List<TutorialStep>();
    }
}
