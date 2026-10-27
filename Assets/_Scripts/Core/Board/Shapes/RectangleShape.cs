using UnityEngine;

namespace TrapTheCat.Core
{
    [CreateAssetMenu(fileName = "Rectangle", menuName = "Trap The Cat/Board Shapes/Rectangle")]
    public sealed class RectangleShape : BoardShape
    {
        public RectangleShape() : base("Rectangle")
        {
        }

        public override Vector2Int GetBoardSize(int size)
        {
            return new Vector2Int(size, Mathf.Max(5, size - 2));
        }

        public override bool IsCellActive(int x, int y, int width, int height)
        {
            return true;
        }
    }
}
