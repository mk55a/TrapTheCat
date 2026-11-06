using System;
using System.Collections;
using TrapTheCat.Core;
using UnityEngine;

namespace TrapTheCat.Presentation
{
    public class CatView : MonoBehaviour
    {
        [SerializeField] private float _speed = 2.5f;
        [SerializeField] private float _escapeRunSpeed = 6f;
        [SerializeField] private float _escapeLegSpeed = 1.8f;
        [SerializeField] private float _pauseAfterEscape = 0.25f;
        [SerializeField] private Camera _camera;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private CatAnimationPresenter _animation;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private LineRenderer _routeLine;

        private float _revealCountdown = -1f;
        private Vector3 _lastStep;

        public bool IsMoving { get; private set; }

        private void Update()
        {
            if (_revealCountdown < 0f)
                return;

            _revealCountdown -= Time.unscaledDeltaTime;
            if (_revealCountdown < 0f)
                _body.enabled = true;
        }

        public void ResetForLevel()
        {
            StopAllCoroutines();
            IsMoving = false;
            _revealCountdown = -1f;
            _body.enabled = true;
            _animation.Sleep();
            ClearRoute();
        }

        public void PlaceAt(Vector2Int cell)
        {
            transform.position = _gridManager.Hex.CellToWorld(cell);
        }

        public void RevealAfter(float delay)
        {
            _animation.WakeAfter(delay + 1f);
            _body.enabled = false;
            _revealCountdown = Mathf.Max(0f, delay);
        }

        public void ShowTrapped()
        {
            _animation.ShowTrapped();
        }

        public void PlayMove(CatTurn turn, Action<CatTurn> arrived)
        {
            DrawRoute(turn);
            Vector3 destination = _gridManager.Hex.CellToWorld(turn.To);
            _lastStep = destination - transform.position;
            _animation.Face(_lastStep);
            _animation.ShowMoving(turn.Escaped);
            if (turn.Escaped)
                _animation.SetPlaybackSpeed(_escapeLegSpeed);
            IsMoving = true;
            StartCoroutine(MoveOneCell(destination, turn, arrived));
        }

        private IEnumerator MoveOneCell(Vector3 destination, CatTurn turn, Action<CatTurn> arrived)
        {
            float speed = Mathf.Max(0.01f, turn.Escaped ? _escapeRunSpeed : _speed);
            while (Vector3.Distance(transform.position, destination) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    destination,
                    speed * Time.deltaTime);
                yield return null;
            }

            transform.position = destination;
            ClearRoute();
            if (turn.Escaped)
            {
                yield return RunOffCamera(turn.To);
                _animation.ShowEscaped();
                yield return new WaitForSeconds(_pauseAfterEscape);
            }
            else
            {
                _animation.ShowIdle();
            }

            IsMoving = false;
            arrived?.Invoke(turn);
        }

        private IEnumerator RunOffCamera(Vector2Int exit)
        {
            Vector3 direction = _gridManager.Hex.DirectionOffBoard(exit, _lastStep, _gridManager.Board.Layout);
            _animation.Face(direction);
            while (!IsOffCamera())
            {
                transform.position += direction * (Mathf.Max(0.01f, _escapeRunSpeed) * Time.deltaTime);
                yield return null;
            }
        }

        private bool IsOffCamera()
        {
            Vector3 centre = _camera.transform.position;
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            Bounds bounds = _body.bounds;
            return bounds.max.x < centre.x - halfWidth ||
                   bounds.min.x > centre.x + halfWidth ||
                   bounds.max.y < centre.y - halfHeight ||
                   bounds.min.y > centre.y + halfHeight;
        }

        private void DrawRoute(CatTurn turn)
        {
            _routeLine.positionCount = turn.Route.Count;
            for (int i = 0; i < turn.Route.Count; i++)
                _routeLine.SetPosition(i, _gridManager.Hex.CellToWorld(turn.Route[i]));
        }

        private void ClearRoute()
        {
            _routeLine.positionCount = 0;
        }
    }
}
