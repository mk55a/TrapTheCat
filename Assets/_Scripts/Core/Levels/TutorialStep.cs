using System;
using UnityEngine;

namespace TrapTheCat.Core
{
    [Serializable]
    public class TutorialStep
    {
        [Min(0)] public int afterPlayerMoves;
        [TextArea(2, 4)] public string message;
    }
}
