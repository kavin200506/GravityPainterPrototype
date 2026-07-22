using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the Subway-Surfers-style tutorial for Level 1.
///
/// Behaviour:
///  • Only runs on Level 1 (first-time players).
///  • Uses a hardcoded lookup of tile names → hint type (Left / Right / Forward).
///  • Shows a glowing particle indicator + hint text over the current tile.
///  • Advances automatically as the ball moves tile-to-tile.
///  • Permanently dismissed via PlayerPrefs when the level completes.
/// </summary>
[DefaultExecutionOrder(100)]   // run after builder (-200) and ball controller
public class TutorialManager : MonoBehaviour
{
    // ── Constants ──────────────────────────────────────────────────────
    private const float  StartDelay       = 1.5f;   // seconds before first hint appears
    private const float  SideOffset       = 0.28f;  // how far into the side region the indicator sits

    // ── Label strings ──────────────────────────────────────────────────
    private const string MsgForward = "Tap the CENTER\nof the tile to move forward";
    private const string MsgLeft    = "Tap the LEFT SIDE\nof the tile to turn left";
    private const string MsgRight   = "Tap the RIGHT SIDE\nof the tile to turn right";

    // ── Hardcoded tile-name → hint lookup for Level 1 ─────────────────
    // Only corner tiles need explicit hints; all others default to Forward.
    private static readonly Dictionary<string, TapHint> TileHints =
        new Dictionary<string, TapHint>(System.StringComparer.OrdinalIgnoreCase)
    {
        // ── LEFT turn tiles ────────────────────────────────────────────
        { "Tile_corner_4_0_-3_0",   TapHint.Left },
        { "Tile_corner_4_1_-3_0",   TapHint.Left },
        { "Tile_corner_6_0_-3_-2",  TapHint.Left },
        { "Tile_corner_6_1_-3_-2",  TapHint.Left },
        { "Tile_corner_8_0_-2_-3",  TapHint.Left },
        { "Tile_corner_8_1_-2_-3",  TapHint.Left },
        { "Tile_corner_9_0_-1_-3",  TapHint.Left },
        { "Tile_corner_9_1_-1_-3",  TapHint.Left },
        { "Tile_corner_13_0_0_-2",  TapHint.Left },
        { "Tile_corner_13_1_0_-2",  TapHint.Left },

        // ── RIGHT turn tiles ───────────────────────────────────────────
        { "Tile_corner_7_0_-2_-2",  TapHint.Right },
        { "Tile_corner_7_1_-2_-2",  TapHint.Right },
        { "Tile_corner_11_0_-1_-1", TapHint.Right },
        { "Tile_corner_11_1_-1_-1", TapHint.Right },
        { "Tile_corner_12_0_0_-1",  TapHint.Right },
        { "Tile_corner_12_1_0_-1",  TapHint.Right },
        { "Tile_corner_14_1_1_-2",  TapHint.Right },
        { "Tile_corner_14_0_1_-2",  TapHint.Right },
    };

    // ── Runtime references ─────────────────────────────────────────────
    private ProceduralLevelBuilder _builder;
    private BallController         _ball;
    private TutorialIndicator      _indicator;

    private IReadOnlyList<GameObject> _tiles;
    private int  _currentTileIndex = -1;
    private bool _active;
    private int  _levelNumber = -1;
    private Coroutine _beginDelayCoroutine;
    private Coroutine _watchCoroutine;

    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Do not disable gameObject in Awake so TutorialManager can listen to level build events.
    }

    private void Start()
    {
        _builder = FindFirstObjectByType<ProceduralLevelBuilder>();
        _ball    = FindFirstObjectByType<BallController>();

        if (_builder == null || _ball == null)
        {
            Debug.LogWarning("[Tutorial] Missing builder or ball — disabling tutorial.");
            return;
        }

        // Create the particle indicator
        GameObject indGo = new GameObject("TutorialIndicator");
        indGo.transform.SetParent(null);
        _indicator = indGo.AddComponent<TutorialIndicator>();

        // Always listen for level builds (including Next Level rebuilds)
        _builder.OnLevelBuilt += OnLevelBuilt;

        // Check if level was built prior to Start (Awake order -200)
        if (_builder.LastBuiltSeed >= 0 && _builder.SpawnedTiles != null && _builder.SpawnedTiles.Count > 0)
        {
            EvaluateCurrentLevelAndStartIfNeeded();
        }
        else
        {
            Debug.Log("[Tutorial] Waiting for level to build...");
        }
    }

    private void OnLevelBuilt(int seed, int tileCount, int coinCount)
    {
        EvaluateCurrentLevelAndStartIfNeeded();
    }

    private void EvaluateCurrentLevelAndStartIfNeeded()
    {
        if (_beginDelayCoroutine != null)
        {
            StopCoroutine(_beginDelayCoroutine);
            _beginDelayCoroutine = null;
        }
        if (_watchCoroutine != null)
        {
            StopCoroutine(_watchCoroutine);
            _watchCoroutine = null;
        }

        _active = false;
        _currentTileIndex = -1;

        if (_indicator != null)
        {
            _indicator.Hide();
        }

        _levelNumber = LevelProgress.GetSelectedMenuLevel();
        if (_levelNumber != 1 && _levelNumber != 6)
        {
            return;
        }

        string shownKey = "TutorialShown_Level" + _levelNumber;
        bool alreadySeen = PlayerPrefs.GetInt(shownKey, 0) == 1;
        if (alreadySeen)
        {
            return;
        }

        if (_builder == null || _builder.SpawnedTiles == null || _builder.SpawnedTiles.Count == 0)
        {
            return;
        }

        _tiles = _builder.SpawnedTiles;
        Debug.Log("[Tutorial] Starting tutorial for Level " + _levelNumber + " (" + _tiles.Count + " tiles).");
        _beginDelayCoroutine = StartCoroutine(BeginAfterDelay(StartDelay));
        _watchCoroutine = StartCoroutine(WatchForLevelComplete());
    }

    private IEnumerator BeginAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _active = true;
        ShowHintForTile(0);
    }

    // ── Update — track ball → closest tile ────────────────────────────
    private void Update()
    {
        if (!_active || _tiles == null || _tiles.Count == 0 || _ball == null) return;

        int closest = FindClosestTileIndex(_ball.transform.position);
        if (closest != _currentTileIndex && closest >= 0)
        {
            _currentTileIndex = closest;
            ShowHintForTile(_currentTileIndex);
        }
    }

    // ── Show hint for the tile at index ───────────────────────────────
    private void ShowHintForTile(int index)
    {
        if (_tiles == null || index < 0 || index >= _tiles.Count) return;

        GameObject tile = _tiles[index];
        if (tile == null) return;

        if (_levelNumber == 1)
        {
            // Look up the hint by tile name — default to Forward if not listed
            TapHint hint = TileHints.TryGetValue(tile.name, out TapHint h) ? h : TapHint.Forward;

            string message = hint == TapHint.Left  ? MsgLeft
                           : hint == TapHint.Right ? MsgRight
                           :                         MsgForward;

            Debug.Log("[Tutorial] Tile=" + tile.name + " Hint=" + hint);

            Vector3 indicatorPos = GetIndicatorWorldPos(tile, hint);
            _indicator.ShowAt(indicatorPos, hint, message);
        }
        else if (_levelNumber == 6)
        {
            // Level 6 specific: only show on Tile_2_0_2 or Tile_11_0_3 (case-insensitive)
            if (tile.name.IndexOf("Tile_2_0_2", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                tile.name.IndexOf("Tile_11_0_3", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                TapHint hint = TapHint.Jump;
                string message = "double tap on left or right side to jump";
                Debug.Log("[Tutorial] Level 6 Tile=" + tile.name + " Hint=" + hint);
                Vector3 indicatorPos = GetIndicatorWorldPos(tile, hint);
                _indicator.ShowAt(indicatorPos, hint, message);
            }
            else
            {
                _indicator.Hide();
            }
        }
    }

    // ── Position helpers ──────────────────────────────────────────────

    /// <summary>Returns world position of the indicator tap zone on the tile.</summary>
    private Vector3 GetIndicatorWorldPos(GameObject tile, TapHint hint)
    {
        Vector3 center = GetTileCenter(tile);
        if (hint == TapHint.Forward || hint == TapHint.Jump) return center;

        Vector3 right = tile.transform.right;
        right.y = 0f;
        right.Normalize();

        float offset = GetTileHalfWidth(tile) * SideOffset * 2f;
        return hint == TapHint.Left
            ? center - right * offset
            : center + right * offset;
    }

    private Vector3 GetTileCenter(GameObject tile)
    {
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
            Vector3 worldSize = Vector3.Scale(box.size, box.transform.lossyScale);
            return worldSize.x * 0.5f;
        }

        Collider col = tile.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.extents.x;

        return 1f;
    }

    private int FindClosestTileIndex(Vector3 ballPos)
    {
        int   best     = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _tiles.Count; i++)
        {
            if (_tiles[i] == null) continue;
            float d = Vector3.Distance(_tiles[i].transform.position, ballPos);
            if (d < bestDist) { bestDist = d; best = i; }
        }

        return best;
    }

    // ── Level complete watch ──────────────────────────────────────────
    private IEnumerator WatchForLevelComplete()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            if (this == null) yield break;

            LevelCompleteUI ui = FindFirstObjectByType<LevelCompleteUI>();
            if (ui != null && ui.gameObject.activeInHierarchy)
            {
                DismissTutorial();
                yield break;
            }
        }
    }

    private void DismissTutorial()
    {
        _active = false;
        _indicator?.Hide();
        // IMPORTANT: use _levelNumber (cached at Awake time) — NOT LevelProgress.GetSelectedMenuLevel().
        // By the time the level-complete screen shows, the selected level may already point to the
        // next level, which would wrongly mark that next level's tutorial as seen.
        string shownKey = "TutorialShown_Level" + _levelNumber;
        PlayerPrefs.SetInt(shownKey, 1);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] Tutorial dismissed permanently for key: " + shownKey);
    }

    private void OnDestroy()
    {
        if (_builder != null) _builder.OnLevelBuilt -= OnLevelBuilt;
        if (_indicator != null) Destroy(_indicator.gameObject);
    }
}
