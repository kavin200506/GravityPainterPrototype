using UnityEngine;

public enum ZoneType { None, Red, Blue, Yellow }

public class TileZone : MonoBehaviour
{
    public ZoneType zoneType = ZoneType.None;

    public Material redMat;
    public Material blueMat;
    public Material yellowMat;
    public Material noneMat;

    private Renderer tileRenderer;

    private void Awake()
    {
        // Prefab instances sometimes get an extra TileZone with no material refs.
        // That copy breaks taps/physics (GetComponent returns the wrong instance). Remove it.
        if (HasAllMaterials())
        {
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
    }

    private bool HasAllMaterials()
    {
        return redMat != null && blueMat != null && yellowMat != null && noneMat != null;
    }

    /// <summary>Prefab-backed instance (scene duplicates often have null material refs).</summary>
    public bool HasAuthoritativeMaterials()
    {
        return HasAllMaterials();
    }

    public static TileZone GetPrimaryZone(GameObject go)
    {
        if (go == null)
        {
            return null;
        }

        TileZone[] zones = go.GetComponents<TileZone>();
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].HasAuthoritativeMaterials())
            {
                return zones[i];
            }
        }

        return zones.Length > 0 ? zones[0] : null;
    }

    private void Start()
    {
        SyncZoneTypeFromPrimary();
        tileRenderer = GetComponent<Renderer>();
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

        // Never assign null — Unity uses the pink error material and breaks the look.
        if (next != null)
        {
            tileRenderer.material = next;
        }
    }

    public Vector3 GetForceDirection()
    {
        switch (zoneType)
        {
            case ZoneType.Red:
                return GetLocalPlanarDirection(Vector3.forward);
            case ZoneType.Blue:
                return GetLocalPlanarDirection(Vector3.left);
            case ZoneType.Yellow:
                return GetLocalPlanarDirection(Vector3.right);
            default:
                return Vector3.zero;
        }
    }

    private Vector3 GetLocalPlanarDirection(Vector3 localDirection)
    {
        Vector3 worldDirection = transform.TransformDirection(localDirection);
        return GetPlanarDirection(worldDirection);
    }

    private static Vector3 GetPlanarDirection(Vector3 direction)
    {
        // Ignore tile tilt and keep force along the ground plane only.
        Vector3 planar = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (planar.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        return planar.normalized;
    }
}
