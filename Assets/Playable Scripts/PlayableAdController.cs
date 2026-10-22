using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Drives the ad-specific presentation: intro banner, tutorial finger and animated end card.
public class PlayableAdController : MonoBehaviour
{
    [SerializeField] private PlayableGameManager _manager;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private TMP_Text _fontSource;
    [SerializeField] private Texture2D _handTexture;
    [SerializeField] private float _handScale = 3f;
    [SerializeField] private RectTransform _endPanel;
    [SerializeField] private Button _installButton;
    [SerializeField] private Button _retryButton;
    [SerializeField] private string _introMessage = "Don't let it escape!";
    [SerializeField] private string _installLabel = "INSTALL NOW";
    [SerializeField] private string _retryLabel = "TRY AGAIN";
    [SerializeField] private float _idleHelpSeconds = 3f;

    private readonly List<Vector2Int> _dangerCells = new List<Vector2Int>();
    private GridTile _guideTile;
    private bool _dangerDirty = true;
    private bool _catPanicking;
    private float _nextMeowTime;
    private float _lastPhase;
    private RectTransform _canvasRect;
    private RectTransform _handRect;
    private RawImage _hand;
    private TMP_Text _banner;
    private RectTransform _endCard;
    private Vector3 _endCardScale = Vector3.one;
    private Vector3 _installScale = Vector3.one;
    private bool _endShown;
    private float _endShownTime;

    private void Awake()
    {
        if (_canvas == null || _manager == null)
        {
            Debug.LogError("PlayableAdController needs the manager and canvas assigned.", this);
            enabled = false;
            return;
        }

        _canvasRect = _canvas.transform as RectTransform;
        FindBanner();
        BuildHand();
        if (_endPanel != null && _endPanel.childCount > 0)
        {
            _endCard = _endPanel.GetChild(0) as RectTransform;
            if (_endCard != null)
                _endCardScale = _endCard.localScale;
        }
        if (_installButton != null)
            _installScale = _installButton.transform.localScale;
        SetLabel(_installButton, _installLabel);
        SetLabel(_retryButton, _retryLabel);
    }

    private void Update()
    {
        UpdateDanger();
        UpdateCatPanic();
        UpdateGuidance();
        UpdateEndCard();
    }

    // The cat meows now and then while it is running out of exits.
    private void UpdateCatPanic()
    {
        CatMovement catMover = _manager.CatMover;
        if (catMover == null || !_catPanicking || _manager.IsGameOver || Time.time < _nextMeowTime)
            return;

        CatSpriteAnimator animator = catMover.GetComponent<CatSpriteAnimator>();
        if (animator == null)
            return;

        animator.PlayMeow();
        _nextMeowTime = Time.time + 2.5f;
    }

    private void OnEnable()
    {
        GameSession.TurnChanged += MarkDangerDirty;
    }

    private void OnDisable()
    {
        GameSession.TurnChanged -= MarkDangerDirty;
    }

    private void MarkDangerDirty()
    {
        _dangerDirty = true;
    }

    // Tints every open edge tile the cat can still reach so the player sees where to block.
    private void UpdateDanger()
    {
        if (!_dangerDirty || Time.unscaledTime < LevelReveal.ReadyTime)
            return;

        GridManager grid = _manager.Grid;
        CatMovement catMover = _manager.CatMover;
        if (grid == null || grid.grid == null || catMover == null)
            return;

        _dangerDirty = false;
        ClearDanger(grid);
        if (_manager.IsGameOver)
            return;

        CatBoard board = grid.CreateCatBoard();
        if (board == null ||
            !grid.TryGetGridCoordinates(catMover.transform.position, out int catX, out int catY))
            return;

        int cat = board.Index(catX, catY);
        board.RunBfs(cat, -1);
        _dangerCells.Clear();
        for (int i = 0; i < board.Count; i++)
        {
            if (i != cat && board.IsPerimeter(i) && !board.IsBlocked(i) && board.Distance(i) > 0)
                _dangerCells.Add(board.Cell(i));
        }
        // Few exits left: the cat panics.
        _catPanicking = _dangerCells.Count > 0 && _dangerCells.Count <= 2;

        foreach (Vector2Int cell in _dangerCells)
        {
            GridTile tile = grid.GetTileAt(cell.x, cell.y);
            if (tile != null)
                tile.SetDanger(true);
        }
    }

    private void ClearDanger(GridManager grid)
    {
        foreach (Vector2Int cell in _dangerCells)
        {
            GridTile tile = grid.GetTileAt(cell.x, cell.y);
            if (tile != null)
                tile.SetDanger(false);
        }
        _dangerCells.Clear();
    }

    private void SetGuideTile(GridTile tile)
    {
        if (_guideTile == tile)
            return;

        if (_guideTile != null)
            _guideTile.SetGuideHover(false);
        _guideTile = tile;
        if (_guideTile != null)
            _guideTile.SetGuideHover(true);
    }

    private void UpdateGuidance()
    {
        float readyTime = LevelReveal.ReadyTime;
        bool revealDone = Time.unscaledTime >= readyTime;
        // Level 1 always points at the tile; level 2 only after the player hesitates; level 3 never.
        float helpDelay = _manager.LevelIndex == 0 ? 0f : (_manager.LevelIndex == 1 ? _idleHelpSeconds : float.MaxValue);
        bool guiding = revealDone && Time.unscaledTime >= readyTime + helpDelay &&
                       !_manager.IsGameOver && _manager.MovesUsed == 0 &&
                       _manager.TutorialCell != null && _manager.Grid != null &&
                       _manager.Grid.grid != null;

        if (_banner != null)
            _banner.gameObject.SetActive(_manager.LevelIndex == 0 && !_manager.IsGameOver && _manager.MovesUsed == 0);
        if (_hand != null)
            _hand.gameObject.SetActive(guiding);
        if (!guiding || _hand == null)
        {
            SetGuideTile(null);
            return;
        }

        PlayableCellData cell = _manager.TutorialCell;
        SetGuideTile(_manager.Grid.GetTileAt(cell.x, cell.y));
        Vector3 world = _manager.Grid.GetCenterGridPosition(cell.x, cell.y);
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 screen = cam.WorldToScreenPoint(world);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screen, null, out Vector2 local))
            return;

        float phase = Mathf.Repeat(Time.time, 1.1f) / 1.1f;
        float tap = Mathf.Sin(phase * Mathf.PI);
        // The hand presses down at mid-cycle and the tile reacts at the same moment.
        if (_lastPhase < 0.5f && phase >= 0.5f && _guideTile != null)
            _guideTile.PlayClickFeedback();
        _lastPhase = phase;
        _handRect.anchoredPosition = local - _canvasRect.rect.center + new Vector2(0f, 40f * (1f - tap));
        _handRect.localScale = Vector3.one * (1f - 0.15f * tap);
    }

    private void UpdateEndCard()
    {
        if (_endPanel == null)
            return;

        bool shown = _endPanel.gameObject.activeInHierarchy;
        if (shown && !_endShown)
        {
            _endShownTime = Time.time;
            SetLabel(_installButton, _installLabel);
        }
        _endShown = shown;
        if (!shown)
            return;

        float t = Mathf.Clamp01((Time.time - _endShownTime) / 0.4f);
        float pop = t >= 1f ? 1f : 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
        if (_endCard != null)
            _endCard.localScale = _endCardScale * Mathf.Max(0.01f, pop);

        if (_installButton != null)
        {
            float pulse = 1f + 0.06f * Mathf.Sin((Time.time - _endShownTime) * 6f);
            _installButton.transform.localScale = _installScale * pulse;
        }
    }

    // Uses the IntroBanner authored in the scene so its styling is preserved.
    private void FindBanner()
    {
        Transform banner = _canvas.transform.Find("GameplayControls/IntroBanner");
        if (banner == null)
        {
            Debug.LogError("PlayableAdController could not find Canvas/GameplayControls/IntroBanner.", this);
            return;
        }

        _banner = banner.GetComponent<TMP_Text>();
        if (_banner != null && !string.IsNullOrEmpty(_introMessage))
            _banner.text = _introMessage;
    }

    private void BuildHand()
    {
        if (_handTexture == null)
        {
            Debug.LogError("PlayableAdController needs a hand texture.", this);
            return;
        }

        GameObject go = new GameObject("TutorialHand", typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(_canvas.transform, false);
        _handRect = go.GetComponent<RectTransform>();
        _handRect.anchorMin = new Vector2(0.5f, 0.5f);
        _handRect.anchorMax = new Vector2(0.5f, 0.5f);
        _handRect.sizeDelta = new Vector2(_handTexture.width, _handTexture.height) * _handScale;
        // The fingertip is near the top-left of the texture, so pivot there.
        _handRect.pivot = new Vector2(14f / _handTexture.width, 1f - 2f / _handTexture.height);
        _hand = go.GetComponent<RawImage>();
        _hand.texture = _handTexture;
        _hand.raycastTarget = false;
        go.SetActive(false);
    }

    private static void SetLabel(Button button, string label)
    {
        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;
    }
}
