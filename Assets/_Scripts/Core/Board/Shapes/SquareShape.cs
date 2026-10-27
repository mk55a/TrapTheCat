using UnityEngine;

namespace TrapTheCat.Core
{
    [CreateAssetMenu(fileName = "Square", menuName = "Trap The Cat/Board Shapes/Square")]
    public sealed class SquareShape : BoardShape
    {
        public SquareShape() : base("Square")
        {
        }

        public override bool IsCellActive(int x, int y, int width, int height)
        {
            float centerX = (width - 1) * 0.5f;
            float centerY = (height - 1) * 0.5f;
            float radius = (Mathf.Min(width, height) - 1) * 0.5f;
            return Mathf.Abs(x - centerX) <= radius && Mathf.Abs(y - centerY) <= radius;
        }
    }
}
