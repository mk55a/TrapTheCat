using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    private const float HoverScale = 1.08f;
    private const float PressScale = 0.93f;

    private RectTransform _rectTransform;
    private Button _button;
    private Graphic _targetGraphic;
    private Vector3 _baseScale;
    private Color _baseColor;
    private Color _hoverColor;
    private CursorTooltip _tooltip;
    private string _tooltipText;
    private bool _hovered;
    private bool _pressed;

    public void Initialize(CursorTooltip tooltip, string tooltipText)
    {
        _tooltip = tooltip;
        _tooltipText = tooltipText;
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("Menu button feedback requires a RectTransform.", this);
            enabled = false;
            return;
        }

        _baseScale = _rectTransform.localScale;
        _button = GetComponent<Button>();
        _targetGraphic = _button != null ? _button.targetGraphic : GetComponent<Graphic>();
        if (_targetGraphic != null)
        {
            _baseColor = _targetGraphic.color;
            _hoverColor = new Color(
                Mathf.Min(1f, _baseColor.r * 1.12f + 0.08f),
                Mathf.Min(1f, _baseColor.g * 1.12f + 0.08f),
                Mathf.Min(1f, _baseColor.b * 1.12f + 0.08f),
                Mathf.Max(_baseColor.a, 0.95f));
        }

        if (_button != null && _button.transition == Selectable.Transition.ColorTint)
        {
            ColorBlock colors = _button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.78f, 0.42f, 1f);
            colors.pressedColor = new Color(0.82f, 0.55f, 0.3f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.55f);
            colors.fadeDuration = 0.08f;
            _button.colors = colors;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        if (_tooltip != null)
            _tooltip.Show(this, _tooltipText);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        _pressed = false;
        if (_tooltip != null)
            _tooltip.Hide(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
    }

    private void Update()
    {
        if (_rectTransform == null)
            return;

        float scale = _pressed ? PressScale : (_hovered ? HoverScale : 1f);
        Vector3 targetScale = _baseScale * scale;
        _rectTransform.localScale = Vector3.Lerp(
            _rectTransform.localScale,
            targetScale,
            Time.unscaledDeltaTime * 16f);

        if (_targetGraphic != null &&
            (_button == null || _button.transition == Selectable.Transition.SpriteSwap))
        {
            Color targetColor = _pressed ? _baseColor * 0.82f : (_hovered ? _hoverColor : _baseColor);
            targetColor.a = _pressed
                ? Mathf.Max(_baseColor.a, 0.9f)
                : (_hovered ? Mathf.Max(_baseColor.a, 0.95f) : _baseColor.a);
            _targetGraphic.color = Color.Lerp(
                _targetGraphic.color,
                targetColor,
                Time.unscaledDeltaTime * 18f);
        }
    }

    private void OnDisable()
    {
        _hovered = false;
        _pressed = false;
        if (_rectTransform != null)
            _rectTransform.localScale = _baseScale;
        if (_targetGraphic != null &&
            (_button == null || _button.transition == Selectable.Transition.SpriteSwap))
            _targetGraphic.color = _baseColor;
        if (_tooltip != null)
            _tooltip.Hide(this);
    }
}
