using UnityEngine;

// Drives Cat.controller. The controller owns the fixed chains (Sleeping -> Sitting -> Idle,
// Licking/Meow -> Idle); this script decides when to wake, walk, run, lick and meow.
public class CatSpriteAnimator : MonoBehaviour
{
    private static readonly int WalkingParam = Animator.StringToHash("Walking");
    private static readonly int RunningParam = Animator.StringToHash("Running");
    private static readonly int WakeParam = Animator.StringToHash("Wake");
    private static readonly int LickParam = Animator.StringToHash("Lick");
    private static readonly int MeowParam = Animator.StringToHash("Meow");
    private static readonly int TrappedParam = Animator.StringToHash("Trapped");
    private static readonly int SleepingState = Animator.StringToHash("Sleeping");
    private static readonly int IdleState = Animator.StringToHash("Idle");

    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Animator _animator;
    [Header("Idle behaviour")]
    [SerializeField, Min(1f)] private float _meowAfterSeconds = 15f;
    [Tooltip("After this long without a player move the cat licks until the player moves.")]
    [SerializeField, Min(1f)] private float _lickAfterSeconds = 25f;
    [Tooltip("Wakes the cat on its own if nothing calls WakeAfter (for scenes without a reveal).")]
    [SerializeField, Min(0f)] private float _defaultSleepSeconds = 1.5f;

    private float _waitTimer;
    private bool _licking;
    private bool _meowed;
    private float _wakeCountdown = -1f;
    private bool _behaviourActive;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (_animator == null || _spriteRenderer == null)
        {
            Debug.LogError("CatSpriteAnimator requires an Animator (Cat.controller) and a SpriteRenderer.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        GameManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        GameManager.OnTurnChanged -= HandleTurnChanged;
    }

    private void Start()
    {
        PlaySleeping();
    }

    private void Update()
    {
        if (_wakeCountdown >= 0f)
        {
            _wakeCountdown -= Time.deltaTime;
            if (_wakeCountdown < 0f)
                Wake();
        }

        if (!_behaviourActive)
            return;

        bool playerTurn = GameManager.Instance != null &&
                          GameManager.Instance.GetCurrentTurn() == Turn.Player;
        if (!playerTurn)
            return;

        _waitTimer += Time.deltaTime;
        if (_licking)
            return;

        if (_waitTimer >= _lickAfterSeconds)
        {
            _licking = true;
            _animator.SetBool(LickParam, true);
        }
        else if (!_meowed && _waitTimer >= _meowAfterSeconds && IsIdle())
        {
            _meowed = true;
            _animator.SetTrigger(MeowParam);
        }
    }

    public void PlaySleeping()
    {
        if (!CanAnimate())
            return;

        ResetParameters();
        _animator.speed = 1f;
        _animator.Play(SleepingState, 0, 0f);
        _behaviourActive = false;
        _wakeCountdown = _defaultSleepSeconds;
    }

    public void WakeAfter(float seconds)
    {
        _wakeCountdown = Mathf.Max(0f, seconds);
    }

    public void Wake()
    {
        if (!CanAnimate())
            return;

        _wakeCountdown = -1f;
        _animator.SetTrigger(WakeParam);
        _behaviourActive = true;
        ResetWaiting();
    }

    public void PlayIdle()
    {
        if (!CanAnimate())
            return;

        _animator.SetBool(WalkingParam, false);
        _animator.SetBool(RunningParam, false);
    }

    public void PlayMovement(bool escaping)
    {
        if (!CanAnimate())
            return;

        ResetWaiting();
        _animator.SetBool(WalkingParam, true);
        _animator.SetBool(RunningParam, escaping);
    }

    public void PlayTrappedReaction()
    {
        if (!CanAnimate())
            return;

        _behaviourActive = false;
        ResetWaiting();
        PlayIdle();
        _animator.SetTrigger(TrappedParam);
    }

    public void SetFacing(Vector3 movement)
    {
        if (_spriteRenderer != null && Mathf.Abs(movement.x) > 0.0001f)
            _spriteRenderer.flipX = movement.x < 0f;
    }

    public void HoldCurrentFrame()
    {
        _behaviourActive = false;
        if (_animator != null)
            _animator.speed = 0f;
    }

    // Any turn change means a move was made: stop licking and restart the wait.
    private void HandleTurnChanged()
    {
        ResetWaiting();
    }

    private void ResetWaiting()
    {
        _waitTimer = 0f;
        _meowed = false;
        _licking = false;
        if (_animator != null && _animator.runtimeAnimatorController != null)
            _animator.SetBool(LickParam, false);
    }

    private bool IsIdle()
    {
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
        return info.shortNameHash == IdleState && !_animator.IsInTransition(0);
    }

    private bool CanAnimate()
    {
        return _animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null;
    }

    private void ResetParameters()
    {
        _animator.SetBool(WalkingParam, false);
        _animator.SetBool(RunningParam, false);
        _animator.ResetTrigger(WakeParam);
        _animator.SetBool(LickParam, false);
        _licking = false;
        _meowed = false;
        _waitTimer = 0f;
        _animator.ResetTrigger(MeowParam);
        _animator.ResetTrigger(TrappedParam);
    }
}
