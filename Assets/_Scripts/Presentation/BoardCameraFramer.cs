using System.Collections;
using UnityEngine;

namespace TrapTheCat.Presentation
{
    public sealed class BoardCameraFramer : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [Tooltip("Empty space kept around the board, as a fraction of the screen on each side.")]
        [SerializeField, Range(0f, 0.45f)] private float _margin = 0.1f;
        [SerializeField, Min(0f)] private float _moveDuration = 0.45f;

        private bool _hasHome;
        private Vector3 _home;
        private Vector2 _boardSize;
        private float _reservedRightFraction;
        private Coroutine _move;
        private Vector3 _targetPosition;
        private float _targetSize;

        public bool TryFrameBoard(Vector2 boardSize, float planeZ, out Vector3 boardCentre)
        {
            boardCentre = Vector3.zero;
            if (_camera == null)
            {
                Debug.LogWarning("BoardCameraFramer has no camera assigned; keeping the configured grid origin.", this);
                return false;
            }

            Vector3 forward = _camera.transform.forward;
            if (Mathf.Abs(forward.z) < 0.0001f)
            {
                Debug.LogWarning("The camera does not face the grid plane; keeping the configured grid origin.", _camera);
                return false;
            }

            bool firstFraming = !_hasHome;
            if (firstFraming)
            {
                _home = _camera.transform.position;
                _hasHome = true;
            }

            _boardSize = boardSize;
            float depth = (planeZ - _home.z) / forward.z;
            boardCentre = _home + forward * depth;
            FrameCamera(!firstFraming);
            return true;
        }

        public void SetReservedRightFraction(float fraction)
        {
            fraction = Mathf.Clamp(fraction, 0f, 0.6f);
            if (Mathf.Approximately(fraction, _reservedRightFraction))
                return;

            _reservedRightFraction = fraction;
            if (_camera != null && _hasHome && _boardSize != Vector2.zero)
                FrameCamera(true);
        }

        private void FrameCamera(bool animate)
        {
            float visibleFraction = 1f - 2f * _margin;
            float usableAspect = Mathf.Max(0.01f, _camera.aspect * (1f - _reservedRightFraction));
            float size = Mathf.Max(
                _boardSize.y / (2f * visibleFraction),
                _boardSize.x / (2f * usableAspect * visibleFraction));
            Vector3 position = _home + _camera.transform.right * (_reservedRightFraction * size * _camera.aspect);

            bool sameTarget = _move != null &&
                              Mathf.Approximately(size, _targetSize) &&
                              (position - _targetPosition).sqrMagnitude < 0.000001f;
            if (sameTarget)
                return;

            if (_move != null)
                StopCoroutine(_move);
            _move = null;
            _targetPosition = position;
            _targetSize = size;

            bool alreadyThere = Mathf.Approximately(_camera.orthographicSize, size) &&
                                (_camera.transform.position - position).sqrMagnitude < 0.000001f;
            if (!animate || alreadyThere || _moveDuration <= 0f)
            {
                _camera.orthographicSize = size;
                _camera.transform.position = position;
                return;
            }

            _move = StartCoroutine(MoveCamera(position, size));
        }

        private IEnumerator MoveCamera(Vector3 position, float size)
        {
            Vector3 startPosition = _camera.transform.position;
            float startSize = _camera.orthographicSize;
            for (float t = 0f; t < _moveDuration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / _moveDuration);
                _camera.transform.position = Vector3.Lerp(startPosition, position, k);
                _camera.orthographicSize = Mathf.Lerp(startSize, size, k);
                yield return null;
            }

            _camera.transform.position = position;
            _camera.orthographicSize = size;
            _move = null;
        }
    }
}
