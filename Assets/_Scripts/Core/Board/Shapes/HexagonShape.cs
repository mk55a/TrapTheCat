using UnityEngine;

namespace TrapTheCat.Core
{
    [CreateAssetMenu(fileName = "Hexagon", menuName = "Trap The Cat/Board Shapes/Hexagon")]
    public sealed class HexagonShape : BoardShape
    {
        public HexagonShape() : base("Hexagon")
        {
        }

        public override bool IsCellActive(int x, int y, int width, int height)
        {
            float centerX = (width - 1) * 0.5f;
            float centerY = (height - 1) * 0.5f;
            float centerAxialX = centerX - Mathf.Floor(centerY / 2f);
            float axialX = x - (y - (y & 1)) / 2;
            float radius = Mathf.Max(1f, (Mathf.Min(width, height) - 1) * 0.5f);
            float q = Mathf.Abs(axialX - centerAxialX);
            float r = Mathf.Abs(y - centerY);
            float diagonal = Mathf.Abs((axialX + y) - (centerAxialX + centerY));
            return Mathf.Max(q, Mathf.Max(r, diagonal)) <= radius;
        }
    }
}
