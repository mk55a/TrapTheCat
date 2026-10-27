using UnityEngine;

namespace TrapTheCat.Presentation
{
    public class CatAnimationPresenter : MonoBehaviour
    {
        private static readonly int WalkingParam = Animator.StringToHash("Walking");
        private static readonly int RunningParam = Animator.StringToHash("Running");
        private static readonly int WakeParam = Animator.StringToHash("Wake");
        private static readonly int LickParam = Animator.StringToHash("Lick");
        private static readonly int MeowParam = Animator.StringToHash("Meow");
        private static readonly int TrappedParam = Animator.StringToHash("Trapped");
        private static readonly int SleepingAnimation = Animator.StringToHash("Sleeping");
        private static readonly int IdleAnimation = Animator.StringToHash("Idle");

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;
        [SerializeField] private CatAnimationProfile _profile;

        private float _wakeCountdown = -1f;
        private float _idleSeconds;
        private bool _meowedThisWait;

        public CatState State { get; private set; }

        private void Start()
        {
            Sleep();
        }

        private void Update()
        {
            if (_wakeCountdown >= 0f)
            {
                _wakeCountdown -= Time.deltaTime;
                if (_wakeCountdown < 0f)
                    Wake();
            }

            if (State != CatState.Idle || _profile == null)
                return;

            _idleSeconds += Time.deltaTime;
            if (_idleSeconds >= _profile.LickAfterSeconds)
            {
                State = CatState.Licking;
                _animator.SetBool(LickParam, true);
            }
            else if (!_meowedThisWait && _idleSeconds >= _profile.MeowAfterSeconds && IsShowingIdleAnimation())
            {
                _meowedThisWait = true;
                _animator.SetTrigger(MeowParam);
            }
        }

        public void Sleep()
        {
            if (!CanAnimate())
                return;

            ResetParameters();
            _animator.speed = 1f;
            _animator.Play(SleepingAnimation, 0, 0f);
            State = CatState.Sleeping;
            _wakeCountdown = _profile != null ? _profile.SleepSeconds : 0f;
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
            EnterIdle();
        }

        public void ShowIdle()
        {
            if (!CanAnimate())
                return;

            StopMoving();
            EnterIdle();
        }

        public void ShowMoving(bool escaping)
        {
            if (!CanAnimate())
                return;

            ResetWaiting();
            _animator.SetBool(WalkingParam, true);
            _animator.SetBool(RunningParam, escaping);
            State = escaping ? CatState.Running : CatState.Walking;
        }

        public void ShowTrapped()
        {
            if (!CanAnimate())
                return;

            ResetWaiting();
            StopMoving();
            _animator.SetTrigger(TrappedParam);
            State = CatState.Trapped;
        }

        public void ShowEscaped()
        {
            State = CatState.Escaped;
            if (_animator != null)
                _animator.speed = 0f;
        }

        public void Face(Vector3 movement)
        {
            if (_spriteRenderer != null && Mathf.Abs(movement.x) > 0.0001f)
                _spriteRenderer.flipX = movement.x < 0f;
        }

        private void EnterIdle()
        {
            State = CatState.Idle;
            ResetWaiting();
        }

        private void StopMoving()
        {
            _animator.SetBool(WalkingParam, false);
            _animator.SetBool(RunningParam, false);
        }

        private void ResetWaiting()
        {
            _idleSeconds = 0f;
            _meowedThisWait = false;
            if (_animator != null && _animator.runtimeAnimatorController != null)
                _animator.SetBool(LickParam, false);
        }

        private bool IsShowingIdleAnimation()
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash == IdleAnimation && !_animator.IsInTransition(0);
        }

        private bool CanAnimate()
        {
            return _animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null;
        }

        private void ResetParameters()
        {
            StopMoving();
            _animator.ResetTrigger(WakeParam);
            _animator.ResetTrigger(MeowParam);
            _animator.ResetTrigger(TrappedParam);
            ResetWaiting();
        }
    }
}
