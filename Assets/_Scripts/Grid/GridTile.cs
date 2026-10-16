using UnityEngine;

public class GridTile : MonoBehaviour
{
    public GridMapObject gridMapObject;

    [SerializeField] private Color _tileColor = new Color(0.92f, 0.95f, 0.82f);
    [SerializeField] private Color _blockedColor = new Color(0.22f, 0.28f, 0.39f);
    [SerializeField] private Color _boundaryColor = new Color(1f, 0.72f, 0.24f);
    [SerializeField] private Color _obstacleColor = new Color(0.47f, 0.36f, 0.62f);

    private SpriteRenderer _spriteRenderer;
    private GridType _lastType;
    private bool _hasAppliedType;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        GridManager manager = GridManager.Instance;
        if (_spriteRenderer == null || manager == null || manager.grid == null)
            return;

        GridMapObject cell = manager.grid.GetGridObject(transform.position);
        if (cell == null || (_hasAppliedType && _lastType == cell.GetCellType()))
            return;

        _lastType = cell.GetCellType();
        _hasAppliedType = true;
        switch (_lastType)
        {
            case GridType.Full:
                _spriteRenderer.color = _blockedColor;
                break;
            case GridType.Boundary:
                _spriteRenderer.color = _boundaryColor;
                break;
            case GridType.Obstacle:
                _spriteRenderer.color = _obstacleColor;
                break;
            default:
                _spriteRenderer.color = _tileColor;
                break;
        }
    }
}
