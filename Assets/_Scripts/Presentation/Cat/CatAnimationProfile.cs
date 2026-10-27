using UnityEngine;

namespace TrapTheCat.Presentation
{
    [CreateAssetMenu(fileName = "CatAnimationProfile", menuName = "Trap The Cat/Cat Animation Profile")]
    public sealed class CatAnimationProfile : ScriptableObject
    {
        [Tooltip("Seconds the cat sleeps when nothing else wakes it (for example in the level editor).")]
        [SerializeField, Min(0f)] private float _sleepSeconds = 1.5f;
        [Tooltip("Seconds of waiting before the cat meows once.")]
        [SerializeField, Min(1f)] private float _meowAfterSeconds = 15f;
        [Tooltip("Seconds of waiting before the cat licks itself until the player moves.")]
        [SerializeField, Min(1f)] private float _lickAfterSeconds = 25f;

        public float SleepSeconds
        {
            get { return _sleepSeconds; }
        }

        public float MeowAfterSeconds
        {
            get { return _meowAfterSeconds; }
        }

        public float LickAfterSeconds
        {
            get { return _lickAfterSeconds; }
        }
    }
}
