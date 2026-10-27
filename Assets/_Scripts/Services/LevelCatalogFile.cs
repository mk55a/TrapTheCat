using System;
using System.Collections.Generic;

namespace TrapTheCat.Services
{
    [Serializable]
    public class LevelCatalogFile
    {
        public const int CurrentFormatVersion = 1;

        public int formatVersion = CurrentFormatVersion;
        public bool suppressBundledCustomLevels;
        public List<LevelFileEntry> levels = new List<LevelFileEntry>();
    }
}
