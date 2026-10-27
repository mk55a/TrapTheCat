using System.Collections.Generic;
using TrapTheCat.Core;

namespace TrapTheCat.Services
{
    public sealed class ProgressionService
    {
        private readonly ISaveService _saveService;
        private readonly LevelCatalog _catalog;
        private readonly SaveData _data;

        public ProgressionService(ISaveService saveService, LevelCatalog catalog)
        {
            _saveService = saveService;
            _catalog = catalog;
            _data = saveService.Load();

            if (catalog.BundledLevels.Count > 0)
                Unlock(catalog.BundledLevels[0].levelId);
        }

        public bool HasSeenTutorial
        {
            get { return _data.hasSeenTutorial; }
        }

        public bool IsUnlocked(LevelData level)
        {
            return level != null && (level.IsCustom || _data.unlockedLevelIds.Contains(level.levelId));
        }

        public void RecordPlay(bool tutorialShown)
        {
            _data.playCount++;
            if (tutorialShown)
                _data.hasSeenTutorial = true;
            _saveService.Save(_data);
        }

        public void RecordWin(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
                return;

            if (!_data.completedLevelIds.Contains(levelId))
                _data.completedLevelIds.Add(levelId);

            IReadOnlyList<LevelData> bundled = _catalog.BundledLevels;
            for (int i = 0; i < bundled.Count - 1; i++)
            {
                if (bundled[i].levelId == levelId)
                {
                    Unlock(bundled[i + 1].levelId);
                    break;
                }
            }

            _saveService.Save(_data);
        }

        private void Unlock(string levelId)
        {
            if (!_data.unlockedLevelIds.Contains(levelId))
                _data.unlockedLevelIds.Add(levelId);
        }
    }
}
