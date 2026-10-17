using UnityEngine;

public class CatSpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite[] _idleFrames;
    [SerializeField] private Sprite[] _walkFrames;
    [SerializeField] private Sprite[] _runFrames;
    [SerializeField] private Sprite[] _layingFrames;
    [SerializeField] private Sprite[] _meowFrames;
    [SerializeField] private Sprite _sittingFrame;
    [SerializeField, Min(1f)] private float _idleFrameRate = 6f;
    [SerializeField, Min(1f)] private float _walkFrameRate = 10f;
    [SerializeField, Min(1f)] private float _runFrameRate = 12f;
    [SerializeField, Min(1f)] private float _reactionFrameRate = 8f;

    private Sprite[] _activeFrames;
    private float _activeFrameRate;
    private float _frameTimer;
    private int _frameIndex;
    private bool _loop;
    private bool _isPlaying;
    private bool _playTrappedSequence;
    private bool _playingMeow;

    private void Awake()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (_spriteRenderer == null)
        {
            Debug.LogError("CatSpriteAnimator requires a SpriteRenderer.", this);
            enabled = false;
            return;
        }

        if (_idleFrames == null || _idleFrames.Length == 0 ||
            _walkFrames == null || _walkFrames.Length == 0 ||
            _runFrames == null || _runFrames.Length == 0 ||
            _layingFrames == null || _layingFrames.Length == 0 ||
            _meowFrames == null || _meowFrames.Length == 0 ||
            _sittingFrame == null)
        {
            Debug.LogError("CatSpriteAnimator is missing one or more Cat-2 animation frames.", this);
            enabled = false;
            return;
        }

        PlayIdle();
    }

    private void Update()
    {
        if (!_isPlaying || _activeFrames == null || _activeFrames.Length == 0)
            return;

        _frameTimer += Time.deltaTime;
        float frameDuration = 1f / _activeFrameRate;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            if (_frameIndex + 1 < _activeFrames.Length)
            {
                _frameIndex++;
                SetCurrentSprite();
            }
            else if (_loop)
            {
                _frameIndex = 0;
                SetCurrentSprite();
            }
            else
            {
                FinishOneShot();
                break;
            }
        }
    }

    public void PlayIdle()
    {
        _playTrappedSequence = false;
        Play(_idleFrames, _idleFrameRate, true);
    }

    public void PlayMovement(bool escaping)
    {
        _playTrappedSequence = false;
        Play(escaping ? _runFrames : _walkFrames,
            escaping ? _runFrameRate : _walkFrameRate,
            true);
    }

    public void PlayTrappedReaction()
    {
        _playTrappedSequence = true;
        _playingMeow = false;
        Play(_layingFrames, _reactionFrameRate, false);
    }

    public void SetFacing(Vector3 movement)
    {
        if (_spriteRenderer != null && Mathf.Abs(movement.x) > 0.0001f)
        {
            _spriteRenderer.flipX = movement.x < 0f;
            SetCurrentSprite();
        }
    }

    public void HoldCurrentFrame()
    {
        _isPlaying = false;
        _loop = false;
    }

    private void Play(Sprite[] frames, float frameRate, bool loop)
    {
        if (!enabled || frames == null || frames.Length == 0)
            return;

        _activeFrames = frames;
        _activeFrameRate = Mathf.Max(1f, frameRate);
        _frameTimer = 0f;
        _frameIndex = 0;
        _loop = loop;
        _isPlaying = true;
        SetCurrentSprite();
    }

    private void FinishOneShot()
    {
        if (_playTrappedSequence && !_playingMeow)
        {
            _playingMeow = true;
            Play(_meowFrames, _reactionFrameRate, false);
            return;
        }

        _playTrappedSequence = false;
        _isPlaying = false;
        _spriteRenderer.sprite = _sittingFrame;
    }

    private void SetCurrentSprite()
    {
        Sprite frame = _activeFrames[_frameIndex];
        if (frame == null)
            return;

        _spriteRenderer.sprite = frame;

        Transform spriteTransform = _spriteRenderer.transform;
        Rect rect = frame.rect;
        Vector2 pivot = frame.pivot;
        float pixelsPerUnit = frame.pixelsPerUnit;
        float horizontalOffset = (rect.width * 0.5f - pivot.x) / pixelsPerUnit;
        if (_spriteRenderer.flipX)
            horizontalOffset = -horizontalOffset;
        Vector3 centerOffset = new Vector3(
            horizontalOffset,
            (rect.height * 0.5f - pivot.y) / pixelsPerUnit,
            0f);
        spriteTransform.localPosition = centerOffset;
    }
}
