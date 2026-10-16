using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CursorTooltip : MonoBehaviour
{
    private const string PrefabResourcePath = "UI/CursorText";
    private static readonly Vector2 CursorOffset = new Vector2(20f, -24f);

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private RectTransform _tooltipRect;
    private TMP_Text _label;
    private Object _activeOwner;

    public static CursorTooltip Create()
    {
        GameObject canvasObject = new GameObject("CursorTooltipCanvas");
        CursorTooltip tooltip = canvasObject.AddComponent<CursorTooltip>();
        if (tooltip.Initialize())
            return tooltip;

        Destroy(canvasObject);
        return null;
    }

    public void Show(Object owner, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        _activeOwner = owner;
        _label.text = text;
        _tooltipRect.sizeDelta = new Vector2(
            Mathf.Max(82f, _label.preferredWidth + 28f),
            38f);
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

    private bool Initialize()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 300;
        gameObject.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasRect = _canvas.GetComponent<RectTransform>();

        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"Could not load cursor tooltip prefab from Resources/{PrefabResourcePath}.");
            return false;
        }

        GameObject tooltip = Instantiate(prefab, _canvas.transform);
        tooltip.name = "CursorText";
        _tooltipRect = tooltip.GetComponent<RectTransform>();
        _label = tooltip.GetComponentInChildren<TMP_Text>(true);
        if (_tooltipRect == null || _label == null)
        {
            Debug.LogError("Cursor tooltip prefab requires a RectTransform and a TMP text component.", prefab);
            return false;
        }

        Graphic[] graphics = tooltip.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
            graphic.raycastTarget = false;

        _tooltipRect.pivot = new Vector2(0f, 1f);
        tooltip.SetActive(false);
        return true;
    }

    private void LateUpdate()
    {
        if (_tooltipRect != null && _tooltipRect.gameObject.activeSelf)
            UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (_canvasRect == null || _tooltipRect == null)
            return;

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
