using System;
using System.Collections.Generic;

namespace TrapTheCat.Services
{
    [Serializable]
    public class SaveData
    {
        public int playCount;
        public bool hasSeenTutorial;
        public List<string> unlockedLevelIds = new List<string>();
        public List<string> completedLevelIds = new List<string>();
    }
}
