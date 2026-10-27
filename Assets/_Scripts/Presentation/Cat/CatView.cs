using System;
using System.Collections;
using TrapTheCat.Core;
using UnityEngine;

namespace TrapTheCat.Presentation
{
    public class CatView : MonoBehaviour
    {
        [SerializeField] private float _speed = 2.5f;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private CatAnimationPresenter _animation;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private LineRenderer _routeLine;

        private float _revealCountdown = -1f;

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
            _animation.Face(destination - transform.position);
            _animation.ShowMoving(turn.Escaped);
            IsMoving = true;
            StartCoroutine(MoveOneCell(destination, turn, arrived));
        }

        private IEnumerator MoveOneCell(Vector3 destination, CatTurn turn, Action<CatTurn> arrived)
        {
            while (Vector3.Distance(transform.position, destination) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    destination,
                    Mathf.Max(0.01f, _speed) * Time.deltaTime);
                yield return null;
            }

            transform.position = destination;
            ClearRoute();
            IsMoving = false;
            if (turn.Escaped)
                _animation.ShowEscaped();
            else
                _animation.ShowIdle();
            arrived?.Invoke(turn);
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
