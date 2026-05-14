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

    private void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        UpdateVisual();
    }

    public void CycleZone()
    {
        if (zoneType == ZoneType.None) zoneType = ZoneType.Red;
        else if (zoneType == ZoneType.Red) zoneType = ZoneType.Blue;
        else if (zoneType == ZoneType.Blue) zoneType = ZoneType.Yellow;
        else if (zoneType == ZoneType.Yellow) zoneType = ZoneType.None;

        UpdateVisual();
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
