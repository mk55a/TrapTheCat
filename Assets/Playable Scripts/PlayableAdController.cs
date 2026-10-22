using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Drives the ad-specific presentation: intro banner, tutorial finger and animated end card.
public class PlayableAdController : MonoBehaviour
{
    [SerializeField] private PlayableGameManager _manager;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private TMP_Text _fontSource;
    [SerializeField] private Texture2D _handTexture;
    [SerializeField] private float _handScale = 3f;
    [SerializeField] private RectTransform _endPanel;
    [SerializeField] private Button _installButton;
    [SerializeField] private Button _retryButton;
    [SerializeField] private string _introMessage = "TRAP THE CAT!\nBlock tiles so it can't reach the edge";
    [SerializeField] private string _installLabel = "INSTALL NOW";
    [SerializeField] private string _nextLevelLabel = "NEXT LEVEL";
    [SerializeField] private string _retryLabel = "TRY AGAIN";

    private RectTransform _canvasRect;
    private RectTransform _handRect;
    private RawImage _hand;
    private TMP_Text _banner;
    private RectTransform _endCard;
    private Vector3 _endCardScale = Vector3.one;
    private Vector3 _installScale = Vector3.one;
    private bool _endShown;
    private float _endShownTime;

    private void Awake()
    {
        if (_canvas == null || _manager == null)
        {
            Debug.LogError("PlayableAdController needs the manager and canvas assigned.", this);
            enabled = false;
            return;
        }

        _canvasRect = _canvas.transform as RectTransform;
        BuildBanner();
        BuildHand();
        if (_endPanel != null && _endPanel.childCount > 0)
        {
            _endCard = _endPanel.GetChild(0) as RectTransform;
            if (_endCard != null)
                _endCardScale = _endCard.localScale;
        }
        if (_installButton != null)
            _installScale = _installButton.transform.localScale;
        SetLabel(_installButton, _installLabel);
        SetLabel(_retryButton, _retryLabel);
    }

    private void Update()
    {
        UpdateGuidance();
        UpdateEndCard();
    }

    private void UpdateGuidance()
    {
        bool revealDone = Time.unscaledTime >= LevelReveal.ReadyTime;
        bool guiding = revealDone && !_manager.IsGameOver && _manager.MovesUsed == 0 &&
                       _manager.TutorialCell != null && _manager.Grid != null &&
                       _manager.Grid.grid != null;

        if (_banner != null)
            _banner.gameObject.SetActive(_manager.LevelIndex == 0 && !_manager.IsGameOver && _manager.MovesUsed == 0);
        if (_hand != null)
            _hand.gameObject.SetActive(guiding);
        if (!guiding || _hand == null)
            return;

        PlayableCellData cell = _manager.TutorialCell;
        Vector3 world = _manager.Grid.GetCenterGridPosition(cell.x, cell.y);
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 screen = cam.WorldToScreenPoint(world);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screen, null, out Vector2 local))
            return;

        float phase = Mathf.Repeat(Time.time, 1.1f) / 1.1f;
        float tap = Mathf.Sin(phase * Mathf.PI);
        _handRect.anchoredPosition = local - _canvasRect.rect.center + new Vector2(0f, 40f * (1f - tap));
        _handRect.localScale = Vector3.one * (1f - 0.15f * tap);
    }

    private void UpdateEndCard()
    {
        if (_endPanel == null)
            return;

        bool shown = _endPanel.gameObject.activeInHierarchy;
        if (shown && !_endShown)
        {
            SetLabel(_installButton, _manager.AdvancesToNextLevel ? _nextLevelLabel : _installLabel);
            SetLabel(_retryButton, _retryLabel);
        }
        _endShown = shown;
        if (!shown)
            return;

        float t = Mathf.Clamp01((Time.time - _endShownTime) / 0.4f);
        float pop = t >= 1f ? 1f : 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
        if (_endCard != null)
            _endCard.localScale = _endCardScale * Mathf.Max(0.01f, pop);

        if (_installButton != null)
        {
            float pulse = 1f + 0.06f * Mathf.Sin((Time.time - _endShownTime) * 6f);
            _installButton.transform.localScale = _installScale * pulse;
        }
    }

    private void BuildBanner()
    {
        GameObject go = new GameObject("IntroBanner", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(_canvas.transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -40f);
        rect.sizeDelta = new Vector2(1400f, 200f);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (_fontSource != null)
        {
            text.font = _fontSource.font;
            text.fontSharedMaterial = _fontSource.fontSharedMaterial;
        }
        text.text = _introMessage;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 56f;
        text.color = Color.white;
        text.raycastTarget = false;
        _banner = text;
    }

    private void BuildHand()
    {
        if (_handTexture == null)
        {
            Debug.LogError("PlayableAdController needs a hand texture.", this);
            return;
        }

        GameObject go = new GameObject("TutorialHand", typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(_canvas.transform, false);
        _handRect = go.GetComponent<RectTransform>();
        _handRect.anchorMin = new Vector2(0.5f, 0.5f);
        _handRect.anchorMax = new Vector2(0.5f, 0.5f);
        _handRect.sizeDelta = new Vector2(_handTexture.width, _handTexture.height) * _handScale;
        // The fingertip is near the top-left of the texture, so pivot there.
        _handRect.pivot = new Vector2(14f / _handTexture.width, 1f - 2f / _handTexture.height);
        _hand = go.GetComponent<RawImage>();
        _hand.texture = _handTexture;
        _hand.raycastTarget = false;
        go.SetActive(false);
    }

    private static void SetLabel(Button button, string label)
    {
        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;
    }
}
