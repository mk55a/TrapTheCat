// Add to a Button or Toggle. Pushes the label/icon down by 2 art pixels while the button is held
// (matching the pressed sprite), and keeps it down while a Toggle is on (for editor tool modes).
// "content" = a child RectTransform holding the label + icon.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PixelButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
    [SerializeField] RectTransform content;
    [SerializeField] float pressOffset = 2f;

    Selectable selectable;
    Toggle toggle;
    Vector2 rest;
    bool held, inside;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        toggle = GetComponent<Toggle>();
        if (content) rest = content.anchoredPosition;
        if (toggle) toggle.onValueChanged.AddListener(_ => Refresh());
    }

    void OnEnable() => Refresh();
    void OnDisable() { held = false; Refresh(); }

    public void OnPointerDown(PointerEventData e) { if (Interactable) { held = true; inside = true; Refresh(); } }
    public void OnPointerUp(PointerEventData e) { held = false; Refresh(); }
    public void OnPointerExit(PointerEventData e) { inside = false; Refresh(); }
    public void OnPointerEnter(PointerEventData e) { inside = true; Refresh(); }

    bool Interactable => selectable == null || selectable.IsInteractable();

    void Refresh()
    {
        if (!content) return;
        bool down = (held && inside) || (toggle != null && toggle.isOn);
        content.anchoredPosition = rest + (down ? Vector2.down * pressOffset : Vector2.zero);
    }
}
