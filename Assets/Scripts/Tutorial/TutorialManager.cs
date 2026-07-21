using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the Subway-Surfers-style tutorial for Level 1.
///
/// Behaviour:
///  • Only runs if it's the player's FIRST time on Level 1
///    (LevelProgress.GetSelectedMenuLevel() == 1 and DifficultyManager.LevelsCompleted == 0).
///  • Waits for ProceduralLevelBuilder to finish building the level.
///  • Finds which tile the ball is currently on, looks ahead to the next tile,
///    and shows a glowing particle indicator + hint text at the correct tap region.
///  • When the ball advances to the next tile the indicator moves forward.
///  • When the level completes the tutorial is permanently dismissed via PlayerPrefs.
/// </summary>
[DefaultExecutionOrder(100)]   // run after builder (-200) and ball controller
public class TutorialManager : MonoBehaviour
{
    // ── Constants ──────────────────────────────────────────────────────
    private const string TutorialShownKey     = "TutorialShown_Level1";
    private const float  TileAdvanceThreshold = 1.2f;   // metres – how close the ball must be to advance indicator
    private const float  StartDelay           = 1.5f;   // seconds after level built before first hint appears
    private const float  SideOffset           = 0.28f;  // normalised side threshold fraction for indicator pos

    // ── Label strings ──────────────────────────────────────────────────
    private const string MsgForward = "Tap the CENTER\nof the tile to move forward";
    private const string MsgLeft    = "Tap the LEFT SIDE\nof the tile to turn left";
    private const string MsgRight   = "Tap the RIGHT SIDE\nof the tile to turn right";

    // ── Runtime references (filled on Start) ───────────────────────────
    private ProceduralLevelBuilder _builder;
    private BallController         _ball;
    private TutorialIndicator      _indicator;

    private IReadOnlyList<GameObject> _tiles;
    private int   _currentTileIndex = -1;
    private bool  _active;
    private bool  _started;

    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Only show tutorial for level 1 first-time players
        bool isLevel1       = LevelProgress.GetSelectedMenuLevel() == 1
                              && DifficultyManager.LevelsCompleted == 0;
        bool alreadySeen    = PlayerPrefs.GetInt(TutorialShownKey, 0) == 1;

        if (!isLevel1 || alreadySeen)
        {
            gameObject.SetActive(false);
            return;
        }
    }

    private void Start()
    {
        _builder = FindFirstObjectByType<ProceduralLevelBuilder>();
        _ball    = FindFirstObjectByType<BallController>();

        if (_builder == null || _ball == null)
        {
            Debug.LogWarning("[Tutorial] Missing builder or ball — disabling tutorial.");
            gameObject.SetActive(false);
            return;
        }

        // Create the indicator object
        GameObject indGo = new GameObject("TutorialIndicator");
        indGo.transform.SetParent(null);
        _indicator = indGo.AddComponent<TutorialIndicator>();

        // Subscribe to level built event
        _builder.OnLevelBuilt += OnLevelBuilt;

        // Also hook LevelCompleteUI (finish = mark tutorial done)
        StartCoroutine(WatchForLevelComplete());
    }

    private void OnLevelBuilt(int seed, int tileCount)
    {
        _tiles = _builder.SpawnedTiles;
        StartCoroutine(BeginAfterDelay(StartDelay));
    }

    private IEnumerator BeginAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _active  = true;
        _started = true;
        // Show hint for the first tile immediately
        ShowHintForTile(0);
    }

    // ─────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (!_active || _tiles == null || _tiles.Count == 0) return;
        if (_ball == null) return;

        Vector3 ballPos = _ball.transform.position;

        // Find the closest tile index to the ball
        int closest = FindClosestTileIndex(ballPos);

        if (closest != _currentTileIndex && closest >= 0)
        {
            _currentTileIndex = closest;
            ShowHintForTile(_currentTileIndex);
        }
    }

    // ── Core logic ─────────────────────────────────────────────────────

    private void ShowHintForTile(int tileIndex)
    {
        if (_tiles == null || tileIndex < 0 || tileIndex >= _tiles.Count) return;

        GameObject currentTile = _tiles[tileIndex];
        if (currentTile == null) return;

        Vector3 tileCenter = GetTileCenter(currentTile);

        // Determine the direction to the NEXT tile to figure out straight / left / right
        TapHint hint    = TapHint.Forward;
        string  message = MsgForward;

        if (tileIndex + 1 < _tiles.Count)
        {
            GameObject nextTile = _tiles[tileIndex + 1];
            if (nextTile != null)
            {
                hint    = GetTurnHint(currentTile.transform, nextTile.transform);
                message = hint == TapHint.Left    ? MsgLeft
                        : hint == TapHint.Right   ? MsgRight
                        :                           MsgForward;
            }
        }

        // Calculate where on the tile the indicator should appear
        Vector3 indicatorPos = GetIndicatorWorldPos(currentTile, hint);

        _indicator.ShowAt(indicatorPos, hint, message);
    }

    // ── Helpers ────────────────────────────────────────────────────────

    /// <summary>Looks at the direction from the current tile to the next tile
    /// relative to the current tile's local forward, and returns Left / Right / Forward.</summary>
    private TapHint GetTurnHint(Transform current, Transform next)
    {
        Vector3 toNext = next.position - current.position;
        toNext.y = 0f;
        if (toNext.sqrMagnitude < 0.01f) return TapHint.Forward;

        Vector3 forward = current.forward;
        forward.y = 0f;
        forward.Normalize();

        float dot   = Vector3.Dot(toNext.normalized, forward);
        float cross = Vector3.Cross(forward, toNext.normalized).y;

        // If cross magnitude is larger than dot magnitude, it's a turn
        if (Mathf.Abs(cross) > Mathf.Abs(dot) * 0.5f)
        {
            return cross > 0f ? TapHint.Right : TapHint.Left;
        }

        return TapHint.Forward;
    }

    /// <summary>Returns the world position for the indicator based on the hint type
    /// (centre, left third, or right third of the tile surface).</summary>
    private Vector3 GetIndicatorWorldPos(GameObject tile, TapHint hint)
    {
        Vector3 center = GetTileCenter(tile);

        if (hint == TapHint.Forward) return center;

        // Get the tile's right axis (XZ plane only)
        Vector3 right = tile.transform.right;
        right.y = 0f;
        right.Normalize();

        // Estimate half-width from collider or bounds
        float halfWidth = GetTileHalfWidth(tile);

        float offset = halfWidth * SideOffset * 2f;   // position in the outer third
        return hint == TapHint.Left
            ? center - right * offset
            : center + right * offset;
    }

    private Vector3 GetTileCenter(GameObject tile)
    {
        // Try BoxCollider first
        BoxCollider box = tile.GetComponentInChildren<BoxCollider>();
        if (box != null)
        {
            Vector3 c = box.transform.TransformPoint(box.center);
            c.y = tile.transform.position.y;
            return c;
        }

        Collider col = tile.GetComponentInChildren<Collider>();
        if (col != null)
        {
            Vector3 c = col.bounds.center;
            c.y = tile.transform.position.y;
            return c;
        }

        return tile.transform.position;
    }

    private float GetTileHalfWidth(GameObject tile)
    {
        BoxCollider box = tile.GetComponentInChildren<BoxCollider>();
        if (box != null)
        {
            // Use the world-space extent along the tile's right axis
            Vector3 worldSize = Vector3.Scale(box.size, box.transform.lossyScale);
            return worldSize.x * 0.5f;
        }

        Collider col = tile.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.extents.x;

        return 1f;
    }

    private int FindClosestTileIndex(Vector3 ballPos)
    {
        int   best    = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _tiles.Count; i++)
        {
            if (_tiles[i] == null) continue;
            float d = Vector3.Distance(_tiles[i].transform.position, ballPos);
            if (d < bestDist)
            {
                bestDist = d;
                best     = i;
            }
        }

        return best;
    }

    // ── Level completion watch ─────────────────────────────────────────

    private IEnumerator WatchForLevelComplete()
    {
        // Poll until the level complete panel appears or scene unloads
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            // Check if the LevelCompleteUI canvas is visible
            LevelCompleteUI ui = FindFirstObjectByType<LevelCompleteUI>();
            if (ui != null && ui.gameObject.activeInHierarchy)
            {
                DismissTutorial();
                yield break;
            }

            // Safety: if tutorial object is no longer in the game (scene reload) bail
            if (this == null) yield break;
        }
    }

    private void DismissTutorial()
    {
        _active = false;
        _indicator?.Hide();
        // Mark as shown so it never appears again
        PlayerPrefs.SetInt(TutorialShownKey, 1);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] Tutorial completed and dismissed permanently.");
    }

    private void OnDestroy()
    {
        if (_builder != null)
            _builder.OnLevelBuilt -= OnLevelBuilt;

        if (_indicator != null)
            Destroy(_indicator.gameObject);
    }
}
