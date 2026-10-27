using TMPro;
using UnityEngine;

namespace TrapTheCat.UI
{
    public sealed class CursorTooltip : MonoBehaviour
    {
        private static readonly Vector2 CursorOffset = new Vector2(20f, -24f);
        private const float MinWidth = 82f;
        private const float Padding = 28f;
        private const float Height = 38f;

        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private RectTransform _tooltipRect;
        [SerializeField] private TMP_Text _label;

        private Object _activeOwner;

        public void Show(Object owner, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            _activeOwner = owner;
            _label.text = text;
            _tooltipRect.sizeDelta = new Vector2(Mathf.Max(MinWidth, _label.preferredWidth + Padding), Height);
            _tooltipRect.gameObject.SetActive(true);
            UpdatePosition();
        }

        public void Hide(Object owner)
        {
            if (_activeOwner != owner)
                return;

            _activeOwner = null;
            _tooltipRect.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_tooltipRect.gameObject.activeSelf)
                UpdatePosition();
        }

        private void UpdatePosition()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                Input.mousePosition,
                null,
                out Vector2 localPosition);

            Vector2 position = localPosition + CursorOffset;
            Rect bounds = _canvasRect.rect;
            float width = _tooltipRect.rect.width;
            float height = _tooltipRect.rect.height;
            position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax - width);
            position.y = Mathf.Clamp(position.y, bounds.yMin + height, bounds.yMax);
            _tooltipRect.anchoredPosition = position;
        }
    }
}
