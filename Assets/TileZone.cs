using UnityEngine;

public enum ZoneType { None, Red, Blue, Yellow }

public class TileZone : MonoBehaviour
{
    public ZoneType zoneType = ZoneType.None;

    [Tooltip("If enabled, colors always push the same way on the board: Red = world +Z, Blue = world -X, Yellow = world +X. Turn off only if you rotate tiles and want forces to follow each tile.")]
    [SerializeField] private bool useWorldSpaceDirections = true;

    public Material redMat;
    public Material blueMat;
    public Material yellowMat;
    public Material noneMat;

    private Renderer tileRenderer;

    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();

        if (HasAllMaterials())
        {
            SyncZoneTypeFromPrimary();
            UpdateVisual();
            return;
        }

        foreach (TileZone other in GetComponents<TileZone>())
        {
            if (other == this)
            {
                continue;
            }

            if (other.HasAllMaterials())
            {
                Destroy(this);
                return;
            }
        }

        SyncZoneTypeFromPrimary();
        UpdateVisual();
    }

    private bool HasAllMaterials()
    {
        return redMat != null && blueMat != null && yellowMat != null && noneMat != null;
    }

    /// <summary>Prefer the instance that owns material refs (duplicate components on one tile).</summary>
    public static TileZone GetPrimaryZone(GameObject go)
    {
        if (go == null)
        {
            return null;
        }

        TileZone[] zones = go.GetComponents<TileZone>();
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].HasAllMaterials())
            {
                return zones[i];
            }
        }

        return zones.Length > 0 ? zones[0] : null;
    }

    private void Start()
    {
        if (tileRenderer == null)
        {
            tileRenderer = GetComponent<Renderer>();
        }

        SyncZoneTypeFromPrimary();
        UpdateVisual();
    }

    private void SyncZoneTypeFromPrimary()
    {
        TileZone primary = GetPrimaryZone(gameObject);
        if (primary == null)
        {
            return;
        }

        ZoneType t = primary.zoneType;
        foreach (TileZone z in GetComponents<TileZone>())
        {
            z.zoneType = t;
        }
    }

    /// <summary>Tap order: Grey → Red → Blue → Yellow → Grey.</summary>
    public void CycleZone()
    {
        TileZone[] zones = GetComponents<TileZone>();
        TileZone primary = GetPrimaryZone(gameObject);
        if (primary == null && zones.Length > 0)
        {
            primary = zones[0];
        }

        ZoneType t = primary != null ? primary.zoneType : zoneType;
        if (t == ZoneType.None)
        {
            t = ZoneType.Red;
        }
        else if (t == ZoneType.Red)
        {
            t = ZoneType.Blue;
        }
        else if (t == ZoneType.Blue)
        {
            t = ZoneType.Yellow;
        }
        else
        {
            t = ZoneType.None;
        }

        for (int i = 0; i < zones.Length; i++)
        {
            zones[i].zoneType = t;
            zones[i].UpdateVisual();
        }
    }

    public void UpdateVisual()
    {
        if (tileRenderer == null)
        {
            tileRenderer = GetComponent<Renderer>();
        }

        Material next = zoneType switch
        {
            ZoneType.Red => redMat,
            ZoneType.Blue => blueMat,
            ZoneType.Yellow => yellowMat,
            _ => noneMat,
        };

        if (next != null)
        {
            tileRenderer.material = next;
        }
    }

    public Vector3 GetForceDirection()
    {
        if (useWorldSpaceDirections)
        {
            switch (zoneType)
            {
                case ZoneType.Red:
                    return Vector3.forward;
                case ZoneType.Blue:
                    return Vector3.left;
                case ZoneType.Yellow:
                    return Vector3.right;
                default:
                    return Vector3.zero;
            }
        }

        switch (zoneType)
        {
            case ZoneType.Red:
                return GetPlanarDirection(transform.forward);
            case ZoneType.Blue:
                return GetPlanarDirection(-transform.right);
            case ZoneType.Yellow:
                return GetPlanarDirection(transform.right);
            default:
                return Vector3.zero;
        }
    }

    private static Vector3 GetPlanarDirection(Vector3 direction)
    {
        Vector3 planar = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (planar.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        return planar.normalized;
    }
}
