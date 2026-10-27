using UnityEngine;

namespace TrapTheCat.Core
{
    public abstract class BoardShape : ScriptableObject
    {
        [Tooltip("Saved in level files. Never change it once levels use this shape.")]
        [SerializeField] private string _id;
        [Tooltip("Shown on the level editor's shape button.")]
        [SerializeField] private string _displayName;

        protected BoardShape(string id)
        {
            _id = id;
            _displayName = id;
        }

        public string Id
        {
            get { return _id; }
        }

        public string DisplayName
        {
            get { return _displayName; }
        }

        public virtual Vector2Int GetBoardSize(int size)
        {
            return new Vector2Int(size, size);
        }

        public abstract bool IsCellActive(int x, int y, int width, int height);
    }
}
