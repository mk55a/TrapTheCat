using UnityEngine;

public class GridTile : MonoBehaviour
{
    [SerializeField] private float _hoverScaleMultiplier = 1.06f;
    [SerializeField] private Color _hoverColor = new Color(1f, 1f, 0.82f, 1f);
    [SerializeField] private Color _clickFlashColor = new Color(1f, 0.78f, 0.36f, 1f);
    [SerializeField] private float _clickAnimationDuration = 0.24f;
    [SerializeField] private float _animationSmoothing = 14f;
    [SerializeField] private float _spawnDuration = 0.4f;
    [Tooltip("Width in pixels of the visible hexagon inside the sprite cell.")]
    [SerializeField] private float _artPixelWidth = 30f;

    private float ArtPixelWidth { get { return _artPixelWidth; } }

    private bool _spawning;
    private float _spawnDelay;
    private float _spawnTime;
    private float _typeHold;
    private bool _pulseOnRelease;

    [Header("Sprites")]
    [SerializeField] private Sprite _emptySprite;
    [SerializeField] private Sprite _blockedSprite;
    [SerializeField] private Sprite _hoverSprite;
    [SerializeField] private Sprite _pressedSprite;

    private Sprite EmptySprite { get { return _emptySprite; } }
    private Sprite BlockedSprite { get { return _blockedSprite; } }

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
    }

    public void Initialize(GridMapObject gridObject, float tileWidth)
    {
        _gridObject = gridObject;
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (EmptySprite == null || BlockedSprite == null)
        {
            Debug.LogError("GridTile requires assigned empty and blocked sprites.", this);
            return;
        }

        _spriteRenderer.sprite = EmptySprite;

        // Scale so the 30 px of art in the sprite spans tileWidth world units.
        float artWorldWidth = ArtPixelWidth / _spriteRenderer.sprite.pixelsPerUnit;
        transform.localScale = Vector3.one * (tileWidth / artWorldWidth);

        _baseScale = transform.localScale;
        _baseColor = _spriteRenderer.color;
        _isHovered = false;
        _clickAnimationTime = 0f;
        _hasAppliedType = true;
        _lastType = GridType.Empty;
    }

    // Keeps showing the empty sprite for a while even if the cell is already blocked,
    // then pops the blocked sprite in with a click pulse.
    public void HoldVisualType(float seconds)
    {
        _typeHold = Mathf.Max(0f, seconds);
        _hasAppliedType = false;
        _lastType = GridType.Empty;
        _pulseOnRelease = true;
        if (_spriteRenderer != null)
            _spriteRenderer.sprite = EmptySprite;
    }

    public void SetHovered(bool isHovered)
    {
        _isHovered = isHovered;
    }

    // Hides the tile until the delay passes, then pops it in with a small overshoot.
    public void PlaySpawn(float delay)
    {
        if (_spriteRenderer == null)
            return;

        _spawning = true;
        _spawnDelay = Mathf.Max(0f, delay);
        _spawnTime = 0f;
        transform.localScale = Vector3.zero;
        Color hidden = _baseColor;
        hidden.a = 0f;
        _spriteRenderer.color = hidden;
    }

    private bool UpdateSpawn()
    {
        if (_spawnDelay > 0f)
        {
            _spawnDelay -= Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            return true;
        }

        _spawnTime += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        float t = Mathf.Clamp01(_spawnTime / _spawnDuration);
        const float overshoot = 1.9f;
        float s = t - 1f;
        float eased = 1f + (overshoot + 1f) * s * s * s + overshoot * s * s;
        transform.localScale = _baseScale * Mathf.Max(0f, eased);
        Color color = _baseColor;
        color.a = Mathf.Clamp01(t * 2.5f);
        _spriteRenderer.color = color;
        if (t >= 1f)
        {
            _spawning = false;
            transform.localScale = _baseScale;
            _spriteRenderer.color = _baseColor;
        }
        return _spawning;
    }

    public void PlayClickFeedback()
    {
        _clickAnimationTime = Mathf.Max(0.01f, _clickAnimationDuration);
    }

    private void Update()
    {
        if (_spriteRenderer == null)
            return;

        bool holding = _typeHold > 0f;
        if (holding)
        {
            _typeHold -= Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (_typeHold <= 0f)
                holding = false;
        }

        if (_gridObject != null && !holding)
        {
            if (_pulseOnRelease)
            {
                _pulseOnRelease = false;
                PlayClickFeedback();
            }
            GridType type = _gridObject.GetCellType();
            _lastType = type;
            _hasAppliedType = true;
            bool blocked = type == GridType.Full || type == GridType.Obstacle;
            Sprite desired = EmptySprite;
            if (blocked)
                desired = BlockedSprite;
            else if (_clickAnimationTime > 0f && _pressedSprite != null)
                desired = _pressedSprite;
            else if (_isHovered && _hoverSprite != null)
                desired = _hoverSprite;
            if (desired != null && _spriteRenderer.sprite != desired)
                _spriteRenderer.sprite = desired;
        }

        if (_spawning && UpdateSpawn())
            return;

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

}
