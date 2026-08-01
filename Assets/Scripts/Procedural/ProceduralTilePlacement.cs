using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Edge-aligned placement for consecutive path tiles. World positions are derived from
/// verified tile footprint dimensions; overlap validation rejects paths that loop back
/// into the same lane.
/// </summary>
public static class ProceduralTilePlacement
{
    private const float OverlapMargin = 0.04f;

    public struct PlacedTile
    {
        public Vector3 Center;
        public float YRotation;
        public bool IsCornerPad;
        public int PathIndex;
        public int TurnIndex;
        public int PadIndex;
    }

    public static Vector3 ComputeCenterPosition(int index, IReadOnlyList<LevelCell> cells, LevelGenConfig config)
    {
        if (index <= 0 || cells == null || cells.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 position = Vector3.zero;
        for (int i = 1; i <= index; i++)
        {
            position += ComputeStepOffset(i, cells, config);
        }

        return position;
    }

    public static void ApplyPathTransform(
        Transform tile,
        int index,
        IReadOnlyList<LevelCell> cells,
        LevelGenConfig config)
    {
        tile.localPosition = ComputeCenterPosition(index, cells, config);
        tile.localRotation = Quaternion.Euler(0f, cells[index].YRotation, 0f);
        tile.localScale = config.tileLocalScale;
    }

    public static List<PlacedTile> BuildPlacementPlan(IReadOnlyList<LevelCell> cells, LevelGenConfig config)
    {
        var plan = new List<PlacedTile>();
        if (cells == null || config == null)
        {
            return plan;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            plan.Add(new PlacedTile
            {
                Center = ComputeCenterPosition(i, cells, config),
                YRotation = cells[i].YRotation,
                IsCornerPad = false,
                PathIndex = i,
                TurnIndex = -1,
                PadIndex = -1
            });
        }

        if (!config.addCornerPads)
        {
            return plan;
        }

        for (int turnIndex = 2; turnIndex < cells.Count; turnIndex++)
        {
            if (!IsTurnIndex(turnIndex, cells))
            {
                continue;
            }

            int padCount = CountCornerPadsForTurn(turnIndex, cells, config);
            float padRotation = cells[turnIndex - 1].YRotation;
            for (int padIndex = 0; padIndex < padCount; padIndex++)
            {
                plan.Add(new PlacedTile
                {
                    Center = ComputeCornerPadPosition(turnIndex, padIndex, padCount, cells, config),
                    YRotation = padRotation,
                    IsCornerPad = true,
                    PathIndex = -1,
                    TurnIndex = turnIndex,
                    PadIndex = padIndex
                });
            }
        }

        return plan;
    }

    public static bool HasMainTileOverlaps(IReadOnlyList<LevelCell> cells, LevelGenConfig config)
    {
        return HasOverlaps(BuildPlacementPlan(cells, config), config, includeCornerPads: false);
    }

    public static bool HasAnyTileOverlaps(IReadOnlyList<LevelCell> cells, LevelGenConfig config)
    {
        return HasOverlaps(BuildPlacementPlan(cells, config), config, includeCornerPads: true);
    }

    /// <summary>
    /// Stage 2 basic reachability: the ball can reach the finish only if the main path is
    /// contiguous (every consecutive pair is a single grid step) and every consecutive main
    /// tile is edge-aligned so the ball cannot fall through a seam. Dropped corner pads never
    /// strand the ball because the main path stays connected.
    /// </summary>
    public static bool IsFinishable(IReadOnlyList<LevelCell> cells, LevelGenConfig config)
    {
        if (cells == null || cells.Count < 2 || config == null)
        {
            return false;
        }

        for (int i = 1; i < cells.Count; i++)
        {
            Vector2Int diff = cells[i].GridPos - cells[i - 1].GridPos;
            if (Mathf.Abs(diff.x) + Mathf.Abs(diff.y) != 1)
            {
                return false;
            }
        }

        List<PlacedTile> plan = BuildPlacementPlan(cells, config);
        for (int i = 1; i < cells.Count; i++)
        {
            if (!AreEdgeAlignedNeighbors(plan[i - 1], plan[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Final build plan after the local pad-drop repair: identical to the runtime builder's
    /// output. Any corner pad that would truly overlap a tile it is not edge-aligned with is
    /// dropped (that is the overlap fallback); the main path is never changed. Generator and
    /// builder share this so Stage 1/Stage 2 validation can never disagree with the build.
    /// </summary>
    public static List<PlacedTile> BuildRepairedPlan(IReadOnlyList<LevelCell> cells, LevelGenConfig config)
    {
        List<PlacedTile> plan = BuildPlacementPlan(cells, config);
        var repaired = new List<PlacedTile>();
        if (plan == null || plan.Count == 0)
        {
            return repaired;
        }

        for (int i = 0; i < plan.Count; i++)
        {
            PlacedTile candidate = plan[i];
            if (WouldOverlapAny(candidate, repaired, config))
            {
                continue;
            }

            repaired.Add(candidate);
        }

        return repaired;
    }

    private static bool WouldOverlapAny(PlacedTile candidate, IReadOnlyList<PlacedTile> placed, LevelGenConfig config)
    {
        const float margin = 0.04f;
        Bounds candidateBounds = ProceduralTileFootprint.ComputeWorldBounds(
            candidate.Center,
            candidate.YRotation,
            config);

        for (int i = 0; i < placed.Count; i++)
        {
            PlacedTile other = placed[i];
            if (AreEdgeAlignedNeighbors(candidate, other))
            {
                continue;
            }

            Bounds otherBounds = ProceduralTileFootprint.ComputeWorldBounds(
                other.Center,
                other.YRotation,
                config);

            if (ProceduralTileFootprint.BoundsOverlap(candidateBounds, otherBounds, margin))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsTurnIndex(int index, IReadOnlyList<LevelCell> cells)
    {
        if (cells == null || index < 2 || index >= cells.Count)
        {
            return false;
        }

        Vector2Int step = cells[index].GridPos - cells[index - 1].GridPos;
        Vector2Int previousStep = cells[index - 1].GridPos - cells[index - 2].GridPos;
        return step != previousStep;
    }

    public static int CountTurns(IReadOnlyList<LevelCell> cells)
    {
        int count = 0;
        if (cells == null)
        {
            return count;
        }

        for (int i = 2; i < cells.Count; i++)
        {
            if (IsTurnIndex(i, cells))
            {
                count++;
            }
        }

        return count;
    }

    public static int CountCornerPadsForTurn(int turnIndex, IReadOnlyList<LevelCell> cells, LevelGenConfig config)
    {
        if (cells == null || config == null || !config.addCornerPads
            || turnIndex < 2 || turnIndex >= cells.Count
            || !IsTurnIndex(turnIndex, cells))
        {
            return 0;
        }

        return Mathf.Max(0, config.cornerPadTileCount);
    }

    /// <summary>
    /// Filler tile at a turn: same orientation as the incoming tile, stepped forward along
    /// that leg so the ball can carry momentum through left/right corners.
    /// </summary>
    public static Vector3 ComputeCornerPadPosition(
        int turnIndex,
        int padIndex,
        int padCount,
        IReadOnlyList<LevelCell> cells,
        LevelGenConfig config)
    {
        Vector2Int previousLeg = cells[turnIndex - 1].GridPos - cells[turnIndex - 2].GridPos;
        LevelCell forwardTile = cells[turnIndex - 1];
        Vector3 legDirection = GridStepToWorld(previousLeg);
        float halfForward = ProceduralTileFootprint.GetPlanarHalfExtentAlong(
            forwardTile.YRotation,
            legDirection,
            config);
        float halfPad = halfForward;

        Vector3 position = ComputeCenterPosition(turnIndex - 1, cells, config);
        for (int k = 0; k <= padIndex; k++)
        {
            float halfFrom = k == 0 ? halfForward : halfPad;
            position += legDirection * (halfFrom + halfPad + config.tileGap);
        }

        return position;
    }

    public static void ApplyCornerPadTransform(
        Transform tile,
        int turnIndex,
        int padIndex,
        int padCount,
        IReadOnlyList<LevelCell> cells,
        LevelGenConfig config)
    {
        LevelCell forwardTile = cells[turnIndex - 1];
        tile.localPosition = ComputeCornerPadPosition(turnIndex, padIndex, padCount, cells, config);
        tile.localRotation = Quaternion.Euler(0f, forwardTile.YRotation, 0f);
        tile.localScale = config.tileLocalScale;
    }

    public static void HidePathPreview()
    {
        ProceduralPathVisualizer[] visualizers = Object.FindObjectsByType<ProceduralPathVisualizer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < visualizers.Length; i++)
        {
            ProceduralPathVisualizer visualizer = visualizers[i];
            if (visualizer == null)
            {
                continue;
            }

            Transform parent = visualizer.parent != null ? visualizer.parent : visualizer.transform;
            for (int c = parent.childCount - 1; c >= 0; c--)
            {
                Object.Destroy(parent.GetChild(c).gameObject);
            }

            visualizer.gameObject.SetActive(false);
        }
    }

    private static bool HasOverlaps(
        IReadOnlyList<PlacedTile> tiles,
        LevelGenConfig config,
        bool includeCornerPads)
    {
        if (tiles == null || tiles.Count < 2 || config == null)
        {
            return false;
        }

        for (int a = 0; a < tiles.Count; a++)
        {
            PlacedTile tileA = tiles[a];
            if (!includeCornerPads && tileA.IsCornerPad)
            {
                continue;
            }

            Bounds boundsA = ProceduralTileFootprint.ComputeWorldBounds(tileA.Center, tileA.YRotation, config);

            for (int b = a + 1; b < tiles.Count; b++)
            {
                PlacedTile tileB = tiles[b];
                if (!includeCornerPads && tileB.IsCornerPad)
                {
                    continue;
                }

                if (AreEdgeAlignedNeighbors(tileA, tileB))
                {
                    continue;
                }

                Bounds boundsB = ProceduralTileFootprint.ComputeWorldBounds(tileB.Center, tileB.YRotation, config);
                if (ProceduralTileFootprint.BoundsOverlap(boundsA, boundsB, OverlapMargin))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// True when two placed tiles are intentionally edge-aligned (they meet along an edge
    /// with only the configured gap between them) and must be allowed to touch:
    /// consecutive main path tiles, pads of the same turn, and a pad with its own
    /// incoming/turn main tile. Every other pair must not overlap.
    /// </summary>
    public static bool AreEdgeAlignedNeighbors(PlacedTile a, PlacedTile b)
    {
        if (!a.IsCornerPad && !b.IsCornerPad)
        {
            return Mathf.Abs(a.PathIndex - b.PathIndex) == 1;
        }

        if (a.IsCornerPad && b.IsCornerPad)
        {
            return a.TurnIndex == b.TurnIndex;
        }

        PlacedTile pad = a.IsCornerPad ? a : b;
        PlacedTile main = a.IsCornerPad ? b : a;
        return main.PathIndex == pad.TurnIndex || main.PathIndex == pad.TurnIndex - 1;
    }

    private static Vector3 ComputeStepOffset(int index, IReadOnlyList<LevelCell> cells, LevelGenConfig config)
    {
        Vector2Int step = cells[index].GridPos - cells[index - 1].GridPos;
        LevelCell previous = cells[index - 1];
        LevelCell current = cells[index];

        float halfPrevious = ProceduralTileFootprint.GetPlanarHalfExtentAlong(
            previous.YRotation,
            GridStepToWorld(step),
            config);
        float halfCurrent = ProceduralTileFootprint.GetPlanarHalfExtentAlong(
            current.YRotation,
            GridStepToWorld(step),
            config);

        Vector3 delta = GridStepToWorld(step) * (halfPrevious + halfCurrent + config.tileGap);

        if (index >= 2)
        {
            Vector2Int previousStep = cells[index - 1].GridPos - cells[index - 2].GridPos;
            if (previousStep != step)
            {
                Vector3 previousLeg = GridStepToWorld(previousStep);
                float exitAlongPrevious = ProceduralTileFootprint.GetPlanarHalfExtentAlong(
                    previous.YRotation,
                    previousLeg,
                    config);
                float enterAlongPrevious = ProceduralTileFootprint.GetPlanarHalfExtentAlong(
                    current.YRotation,
                    previousLeg,
                    config);
                delta += previousLeg * (exitAlongPrevious + enterAlongPrevious);
            }
        }

        return delta;
    }

    private static Vector3 GridStepToWorld(Vector2Int gridStep)
    {
        return new Vector3(gridStep.x, 0f, gridStep.y);
    }
}
