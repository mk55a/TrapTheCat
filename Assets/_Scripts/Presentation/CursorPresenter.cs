using UnityEngine;

namespace TrapTheCat.Presentation
{
    public enum CursorShape
    {
        Pointer,
        HandPoint,
        HandOpen,
        HandClosed
    }

    public sealed class CursorPresenter : MonoBehaviour
    {
        private static readonly Vector2 PointerHotspot = new Vector2(4f, 4f);
        private static readonly Vector2 HandHotspot = new Vector2(14f, 2f);

        [SerializeField] private Texture2D _pointer;
        [SerializeField] private Texture2D _handPoint;
        [SerializeField] private Texture2D _handOpen;
        [SerializeField] private Texture2D _handClosed;

        private bool _hasShape;
        private CursorShape _shape;

        public void Show(CursorShape shape)
        {
            if (_hasShape && _shape == shape)
                return;

            Texture2D texture = TextureFor(shape);
            if (texture == null)
                return;

            Cursor.SetCursor(texture, shape == CursorShape.Pointer ? PointerHotspot : HandHotspot, CursorMode.Auto);
            _shape = shape;
            _hasShape = true;
        }

        public void ResetToSystemCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            _hasShape = false;
        }

        private Texture2D TextureFor(CursorShape shape)
        {
            switch (shape)
            {
                case CursorShape.HandPoint:
                    return _handPoint;
                case CursorShape.HandOpen:
                    return _handOpen;
                case CursorShape.HandClosed:
                    return _handClosed;
                default:
                    return _pointer;
            }
        }

        private void OnDisable()
        {
            ResetToSystemCursor();
        }
    }
}
