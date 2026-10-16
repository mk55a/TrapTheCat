using UnityEngine;

public class GridTile : MonoBehaviour
{
    [SerializeField] private float _hoverScaleMultiplier = 1.06f;
    [SerializeField] private Color _hoverColor = new Color(1f, 1f, 0.82f, 1f);
    [SerializeField] private Color _clickFlashColor = new Color(1f, 0.78f, 0.36f, 1f);
    [SerializeField] private float _clickAnimationDuration = 0.24f;
    [SerializeField] private float _animationSmoothing = 14f;

    private static Sprite _emptySprite;
    private static Sprite _blockedSprite;

    private GridMapObject _gridObject;
    private SpriteRenderer _spriteRenderer;
    private Vector3 _baseScale;
    private Color _baseColor = Color.white;
    private GridType _lastType;
    private bool _hasAppliedType;
    private bool _isHovered;
    private float _clickAnimationTime;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        LoadSprites();
    }

    public void Initialize(GridMapObject gridObject, float hexRadius)
    {
        _gridObject = gridObject;
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        LoadSprites();
        if (_emptySprite == null || _blockedSprite == null)
            return;

        _spriteRenderer.sprite = _emptySprite;

        float spriteRadius = _spriteRenderer.sprite.bounds.extents.y;
        if (spriteRadius > 0f)
            transform.localScale = Vector3.one * (hexRadius / spriteRadius);

        _baseScale = transform.localScale;
        _baseColor = _spriteRenderer.color;
        _isHovered = false;
        _clickAnimationTime = 0f;
        _hasAppliedType = true;
        _lastType = GridType.Empty;
    }

    public void SetHovered(bool isHovered)
    {
        _isHovered = isHovered;
    }

    public void PlayClickFeedback()
    {
        _clickAnimationTime = Mathf.Max(0.01f, _clickAnimationDuration);
    }

    private void Update()
    {
        if (_spriteRenderer == null)
            return;

        if (_gridObject != null)
        {
            GridType type = _gridObject.GetCellType();
            if (!_hasAppliedType || _lastType != type)
            {
                _lastType = type;
                _hasAppliedType = true;
                _spriteRenderer.sprite = type == GridType.Full || type == GridType.Obstacle
                    ? _blockedSprite
                    : _emptySprite;
            }
        }

        float clickPulse = 0f;
        float clickScaleX = 1f;
        float clickScaleY = 1f;
        if (_clickAnimationTime > 0f)
        {
            _clickAnimationTime -= Time.unscaledDeltaTime;
            float progress = 1f - Mathf.Clamp01(
                _clickAnimationTime / Mathf.Max(0.01f, _clickAnimationDuration));
            clickPulse = Mathf.Sin(progress * Mathf.PI);

            if (progress < 0.25f)
            {
                float phase = progress / 0.25f;
                clickScaleX = Mathf.Lerp(1f, 1.06f, phase);
                clickScaleY = Mathf.Lerp(1f, 0.9f, phase);
            }
            else if (progress < 0.6f)
            {
                float phase = (progress - 0.25f) / 0.35f;
                clickScaleX = Mathf.Lerp(1.06f, 0.94f, phase);
                clickScaleY = Mathf.Lerp(0.9f, 1.12f, phase);
            }
            else
            {
                float phase = (progress - 0.6f) / 0.4f;
                clickScaleX = Mathf.Lerp(0.94f, 1f, phase);
                clickScaleY = Mathf.Lerp(1.12f, 1f, phase);
            }
        }

        float hoverScale = _isHovered ? _hoverScaleMultiplier : 1f;
        Vector3 targetScale = new Vector3(
            _baseScale.x * hoverScale * clickScaleX,
            _baseScale.y * hoverScale * clickScaleY,
            _baseScale.z);
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * _animationSmoothing);

        Color targetColor = _isHovered
            ? Color.Lerp(_baseColor, _hoverColor, 0.45f)
            : _baseColor;
        if (clickPulse > 0f)
            targetColor = Color.Lerp(targetColor, _clickFlashColor, clickPulse * 0.8f);
        _spriteRenderer.color = Color.Lerp(
            _spriteRenderer.color,
            targetColor,
            Time.unscaledDeltaTime * _animationSmoothing);
    }

    private static void LoadSprites()
    {
        if (_emptySprite != null && _blockedSprite != null)
            return;

        Sprite[] sprites = Resources.LoadAll<Sprite>("Art/Tiles_Sprites");
        foreach (Sprite sprite in sprites)
        {
            if (sprite.name == "Tiles_Sprites_0")
                _emptySprite = sprite;
            else if (sprite.name == "Tiles_Sprites_1")
                _blockedSprite = sprite;
        }

        if (_emptySprite == null || _blockedSprite == null)
            Debug.LogError("Tiles_Sprites must contain Tiles_Sprites_0 (empty) and Tiles_Sprites_1 (blocked).");
    }
}
