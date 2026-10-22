using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Board state is final immediately; only the visuals are staged:
// empty tiles ripple in, blocked tiles appear after a pause, then the cat.
public static class LevelReveal
{
    private const float BlockedDelay = 1.5f;
    private const float BlockedStagger = 0.05f;
    private const float MaxStagger = 0.5f;
    private const float CatGap = 0.3f;
    private const float CatWakeDelay = 1f;
    private const float GuidanceGap = 0.5f;

    private static Coroutine _running;
    private static GridManager _runner;

    // Unscaled time at which the cat is shown and the player can be guided.
    public static float ReadyTime { get; private set; }

    public static void Play(GridManager gridManager, Transform cat, IList<Vector2Int> blockedCells)
    {
        if (_running != null && _runner != null)
            _runner.StopCoroutine(_running);

        float stagger = blockedCells.Count > 0
            ? Mathf.Min(BlockedStagger, MaxStagger / blockedCells.Count)
            : 0f;
        for (int i = 0; i < blockedCells.Count; i++)
        {
            GridTile tile = gridManager.GetTileAt(blockedCells[i].x, blockedCells[i].y);
            if (tile != null)
                tile.HoldVisualType(BlockedDelay + i * stagger);
        }

        float catDelay = BlockedDelay + blockedCells.Count * stagger + CatGap;
        ReadyTime = Time.unscaledTime + catDelay + GuidanceGap;
        _runner = gridManager;
        _running = gridManager.StartCoroutine(RevealCat(cat, catDelay));
    }

    private static IEnumerator RevealCat(Transform cat, float delay)
    {
        CatSpriteAnimator animator = cat.GetComponent<CatSpriteAnimator>();
        if (animator != null)
            animator.WakeAfter(delay + CatWakeDelay);
        Renderer[] renderers = cat.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
            r.enabled = false;

        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            yield return null;
        }

        foreach (Renderer r in renderers)
        {
            if (r != null)
                r.enabled = true;
        }
        _running = null;
    }
}
