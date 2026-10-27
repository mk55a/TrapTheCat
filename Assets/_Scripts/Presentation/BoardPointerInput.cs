using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrapTheCat.Presentation
{
    public sealed class BoardPointerInput : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private CursorPresenter _cursor;

        private TileView _hoveredTile;
        private bool _holdingSelectedCell;
        private bool _showOpenHandAfterRelease;

        public event Action<Vector2Int> CellClicked;

        public Func<Vector2Int, bool> IsCellSelectable { get; set; }

        private void Update()
        {
            bool selectable = TryGetSelectableCell(out Vector2Int cell);

            if (Input.GetMouseButtonDown(0))
            {
                _holdingSelectedCell = selectable;
                if (selectable)
                {
                    _cursor.Show(CursorShape.HandClosed);
                    _gridManager.GetTileAt(cell)?.PlayClickFeedback();
                    CellClicked?.Invoke(cell);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                _showOpenHandAfterRelease = _holdingSelectedCell;
                _holdingSelectedCell = false;
            }

            if (_showOpenHandAfterRelease)
            {
                _cursor.Show(CursorShape.HandOpen);
                _showOpenHandAfterRelease = false;
            }
            else if (Input.GetMouseButton(0) && _holdingSelectedCell)
            {
                _cursor.Show(CursorShape.HandClosed);
            }
            else
            {
                _cursor.Show(selectable ? CursorShape.HandPoint : CursorShape.Pointer);
            }

            SetHoveredTile(selectable ? _gridManager.GetTileAt(cell) : null);
        }

        private bool TryGetSelectableCell(out Vector2Int cell)
        {
            cell = Vector2Int.zero;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (overUI || _camera == null || _gridManager.Board == null || IsCellSelectable == null)
                return false;

            Vector3 worldPosition = _camera.ScreenToWorldPoint(Input.mousePosition);
            worldPosition.z = 0f;
            return _gridManager.Hex.TryWorldToCell(worldPosition, _gridManager.Board.Layout, out cell) &&
                   IsCellSelectable(cell);
        }

        private void SetHoveredTile(TileView tile)
        {
            if (_hoveredTile == tile)
                return;

            if (_hoveredTile != null)
                _hoveredTile.SetHovered(false);
            _hoveredTile = tile;
            if (_hoveredTile != null)
                _hoveredTile.SetHovered(true);
        }

        private void OnDisable()
        {
            SetHoveredTile(null);
        }
    }
}
