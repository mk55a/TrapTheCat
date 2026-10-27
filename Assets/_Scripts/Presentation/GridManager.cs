using TrapTheCat.Core;
using UnityEngine;

namespace TrapTheCat.Presentation
{
    public class GridManager : MonoBehaviour
    {
        [Tooltip("World width of one tile.")]
        [SerializeField] private float _tileWidth = 1f;
        [SerializeField, Min(1f)] private float _tileSpacingMultiplier = 1.08f;
        [SerializeField] private TileView _tilePrefab;
        [Tooltip("Moved so the board is centred where the camera started.")]
        [SerializeField] private Transform _boardOrigin;
        [SerializeField] private BoardCameraFramer _cameraFramer;
        [SerializeField, Min(0f)] private float _spawnWaveDuration = 0.9f;

        private TileView[,] _tiles;
        private TileStateSync _tileStateSync;

        public BoardState Board { get; private set; }

        public HexLayout Hex { get; private set; }

        private void OnDestroy()
        {
            DestroyGrid();
        }

        public void CreateGrid(BoardState board, bool animate = false)
        {
            if (board == null || board.Layout.Width < 3 || board.Layout.Height < 3 ||
                _tileWidth <= 0f || _tileSpacingMultiplier < 1f ||
                _boardOrigin == null || _tilePrefab == null || _cameraFramer == null)
            {
                Debug.LogError("Grid requires a board of at least 3x3, a positive tile width, and assigned origin, tile prefab and camera framer references.", this);
                return;
            }

            DestroyGrid();
            Board = board;
            BoardLayout layout = board.Layout;
            Hex = PlaceBoard(layout.Width, layout.Height);
            _tiles = new TileView[layout.Width, layout.Height];

            Vector3 boardCenter = Hex.CellToWorld(new Vector2Int(layout.Width / 2, layout.Height / 2));
            float maxDistance = 0.001f;
            foreach (Vector2Int cell in layout.ActiveCells)
                maxDistance = Mathf.Max(maxDistance, Vector3.Distance(Hex.CellToWorld(cell), boardCenter));

            foreach (Vector2Int cell in layout.ActiveCells)
            {
                TileView tile = Instantiate(_tilePrefab, Hex.CellToWorld(cell), Quaternion.identity, transform);
                tile.name = $"Tile_{cell.x}_{cell.y}";
                tile.Initialize(Hex.TileWidth);
                if (animate)
                {
                    float ring = Vector3.Distance(Hex.CellToWorld(cell), boardCenter) / maxDistance;
                    tile.PlaySpawn(ring * _spawnWaveDuration + Random.Range(0f, 0.06f));
                }
                _tiles[cell.x, cell.y] = tile;
            }

            _tileStateSync = new TileStateSync(board, _tiles);
        }

        public TileView GetTileAt(Vector2Int cell)
        {
            if (Board == null || !Board.Layout.Contains(cell))
                return null;

            return _tiles[cell.x, cell.y];
        }

        public void DestroyGrid()
        {
            if (_tileStateSync != null)
                _tileStateSync.Dispose();
            _tileStateSync = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            Board = null;
            Hex = null;
            _tiles = null;
        }

        private HexLayout PlaceBoard(int width, int height)
        {
            HexLayout unplaced = new HexLayout(_tileWidth, _tileSpacingMultiplier, Vector3.zero);
            Vector3 origin = _boardOrigin.position;
            if (_cameraFramer.TryFrameBoard(unplaced.GetBoardSize(width, height), origin.z, out Vector3 boardCentre))
            {
                Vector2 centreOffset = unplaced.GetBoardCentreOffset(width, height);
                origin = new Vector3(boardCentre.x - centreOffset.x, boardCentre.y - centreOffset.y, origin.z);
                _boardOrigin.position = origin;
            }
            return new HexLayout(_tileWidth, _tileSpacingMultiplier, origin);
        }
    }
}
