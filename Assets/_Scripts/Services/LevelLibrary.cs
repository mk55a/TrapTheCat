using System.Collections.Generic;
using TrapTheCat.Core;
using UnityEngine;

namespace TrapTheCat.Services
{
    [CreateAssetMenu(fileName = "LevelLibrary", menuName = "Trap The Cat/Level Library")]
    public sealed class LevelLibrary : ScriptableObject
    {
        [Tooltip("Bundled levels in any order; each level's Progression Order decides the play order.")]
        [SerializeField] private List<LevelDefinition> _bundledLevels = new List<LevelDefinition>();
        [Tooltip("The level catalog file (levelData.json) that builds read custom levels from.")]
        [SerializeField] private TextAsset _bundledCatalog;

        public TextAsset BundledCatalog
        {
            get { return _bundledCatalog; }
        }

        public List<LevelData> CreateBundledLevels()
        {
            List<LevelData> levels = new List<LevelData>();
            foreach (LevelDefinition definition in _bundledLevels)
            {
                LevelData level = LevelData.FromDefinition(definition);
                if (level != null)
                    levels.Add(level);
            }

            levels.Sort((a, b) => a.progressionOrder.CompareTo(b.progressionOrder));
            return levels;
        }
    }
}
