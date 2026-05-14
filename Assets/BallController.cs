using System;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public float forceStrength = 20f;
    public float maxPlanarSpeed = 4f;
    [Tooltip("Horizontal drag on grey tiles / in air. Lower = freer roll; 0 = no extra drag.")]
    public float planarDrag = 0.55f;
    public bool dampWhenNoZone = false;
    public float idlePlanarDamping = 8f;
    public float zoneProbeRadius = 0.48f;
    public float zoneRetentionTime = 0.15f;

    private Rigidbody rb;
    private TileZone currentZone;
    private float timeSinceLastZoneContact;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.WakeUp();
        timeSinceLastZoneContact = 999f;
    }

    private void FixedUpdate()
    {
        ResolveCurrentTileZone();

        bool onPaintedTile = currentZone != null && currentZone.zoneType != ZoneType.None;
        bool inRetention = timeSinceLastZoneContact <= zoneRetentionTime;

        if (onPaintedTile)
        {
            Vector3 direction = currentZone.GetForceDirection();
            if (direction.sqrMagnitude > 0.0001f)
            {
                rb.AddForce(direction * forceStrength, ForceMode.Acceleration);
            }
            else if (dampWhenNoZone)
            {
                ApplyIdleDamping();
            }
        }
        else if (dampWhenNoZone && (currentZone != null || inRetention))
        {
            ApplyIdleDamping();
        }

        if (!onPaintedTile)
        {
            ApplyPlanarDrag();
        }

        ClampPlanarSpeed();
    }

    /// <summary>
    /// Pick the tile the ball is standing on by scanning under the ball and choosing the closest on XZ.
    /// Avoids raycasts that grab a neighbor's tall trigger first.
    /// </summary>
    private void ResolveCurrentTileZone()
    {
        float probeR = Mathf.Max(0.35f, zoneProbeRadius);
        Vector3 sphereCenter = transform.position + Vector3.down * 0.35f;

        Collider[] cols = Physics.OverlapSphere(
            sphereCenter,
            probeR,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        TileZone best = null;
        float bestDistSq = float.MaxValue;
        Vector3 ballXZ = transform.position;
        ballXZ.y = 0f;

        for (int i = 0; i < cols.Length; i++)
        {
            Collider col = cols[i];
            if (col == null || col.attachedRigidbody == rb || col.GetComponentInParent<BallController>() != null)
            {
                continue;
            }

            TileZone hit = col.GetComponent<TileZone>() ?? col.GetComponentInParent<TileZone>();
            if (hit == null)
            {
                continue;
            }

            TileZone primary = TileZone.GetPrimaryZone(hit.gameObject) ?? hit;
            Vector3 tileXZ = primary.transform.position;
            tileXZ.y = 0f;
            float d = (tileXZ - ballXZ).sqrMagnitude;
            if (d < bestDistSq)
            {
                bestDistSq = d;
                best = primary;
            }
        }

        if (best == null)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                transform.position + Vector3.up * 0.15f,
                Vector3.down,
                2.5f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null || col.attachedRigidbody == rb || col.GetComponentInParent<BallController>() != null)
                {
                    continue;
                }

                TileZone z = col.GetComponent<TileZone>() ?? col.GetComponentInParent<TileZone>();
                if (z != null)
                {
                    best = TileZone.GetPrimaryZone(z.gameObject) ?? z;
                    break;
                }
            }
        }

        if (best != null)
        {
            currentZone = best;
            timeSinceLastZoneContact = 0f;
        }
        else
        {
            timeSinceLastZoneContact += Time.fixedDeltaTime;
            if (timeSinceLastZoneContact > zoneRetentionTime)
            {
                currentZone = null;
            }
        }
    }

    private void ApplyPlanarDrag()
    {
        if (planarDrag <= 0f)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
        if (planar.sqrMagnitude < 1e-8f)
        {
            return;
        }

        rb.AddForce(-planar * planarDrag, ForceMode.Acceleration);
    }

    private void ClampPlanarSpeed()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);

        float speed = planarVelocity.magnitude;
        if (speed > maxPlanarSpeed)
        {
            planarVelocity = planarVelocity.normalized * maxPlanarSpeed;
            rb.linearVelocity = new Vector3(planarVelocity.x, velocity.y, planarVelocity.z);
        }
    }

    private void ApplyIdleDamping()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 dampedPlanar = Vector3.Lerp(planarVelocity, Vector3.zero, idlePlanarDamping * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(dampedPlanar.x, velocity.y, dampedPlanar.z);
    }
}
