using UnityEngine;

namespace TrapTheCat.Core
{
    [CreateAssetMenu(fileName = "Circle", menuName = "Trap The Cat/Board Shapes/Circle")]
    public sealed class CircleShape : BoardShape
    {
        public CircleShape() : base("Circle")
        {
        }

        public override bool IsCellActive(int x, int y, int width, int height)
        {
            float centerX = (width - 1) * 0.5f;
            float centerY = (height - 1) * 0.5f;
            float halfWidth = Mathf.Max(0.5f, (width - 1) * 0.5f);
            float halfHeight = Mathf.Max(0.5f, (height - 1) * 0.5f);
            float dx = (x - centerX) / halfWidth;
            float dy = (y - centerY) / halfHeight;
            return dx * dx + dy * dy <= 1f;
        }
    }
}
