using System;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public float forceStrength = 20f;
    public float maxPlanarSpeed = 4f;
    [Tooltip("Opposes horizontal motion (rolling friction / drag). Higher = shorter coast.")]
    public float planarDrag = 2.75f;
    public bool dampWhenNoZone = false;
    public float idlePlanarDamping = 8f;
    public float zoneProbeDistance = 2f;
    public float zoneProbeRadius = 0.35f;
    public float zoneRetentionTime = 0.15f;

    private Rigidbody rb;
    private TileZone currentZone;
    private float timeSinceLastZoneContact;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        timeSinceLastZoneContact = 999f;
    }

    void FixedUpdate()
    {
        ResolveZoneFromGroundProbe();

        bool onPaintedTile =
            currentZone != null && currentZone.zoneType != ZoneType.None;
        bool inRetention = timeSinceLastZoneContact <= zoneRetentionTime;

        if (onPaintedTile)
        {
            Vector3 direction = currentZone.GetForceDirection();
            direction = GetContinuousDirection(direction);
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

        ApplyPlanarDrag();

        ClampPlanarSpeed();
    }

    void OnTriggerEnter(Collider other)
    {
        TileZone zone = other.GetComponent<TileZone>() ?? other.GetComponentInParent<TileZone>();
        if (zone != null)
        {
            currentZone = TileZone.GetPrimaryZone(zone.gameObject) ?? zone;
        }
    }

    void OnTriggerExit(Collider other)
    {
        TileZone zone = other.GetComponent<TileZone>() ?? other.GetComponentInParent<TileZone>();
        TileZone primary = zone != null ? TileZone.GetPrimaryZone(zone.gameObject) : null;
        if (primary != null && primary == currentZone)
        {
            currentZone = null;
        }
    }

    private void ResolveZoneFromGroundProbe()
    {
        // SphereCast starting inside/overlapping colliders often misses tiles. Cast downward from above the ball.
        float castHeight = Mathf.Max(zoneProbeDistance, 2.5f);
        Vector3 origin = transform.position + Vector3.up * castHeight;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            castHeight + 1f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        TileZone nearestZone = null;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)
            {
                continue;
            }

            if (col.attachedRigidbody == rb || col.GetComponentInParent<BallController>() != null)
            {
                continue;
            }

            TileZone zone = col.GetComponent<TileZone>() ?? col.GetComponentInParent<TileZone>();
            if (zone != null)
            {
                nearestZone = TileZone.GetPrimaryZone(zone.gameObject) ?? zone;
                break;
            }
        }

        if (nearestZone != null)
        {
            currentZone = nearestZone;
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

    private Vector3 GetContinuousDirection(Vector3 zoneDirection)
    {
        if (currentZone == null || currentZone.zoneType != ZoneType.Red)
        {
            return zoneDirection;
        }

        Vector3 planarVelocity = rb.linearVelocity;
        planarVelocity.y = 0f;
        if (planarVelocity.sqrMagnitude < 0.01f || zoneDirection.sqrMagnitude < 0.0001f)
        {
            return zoneDirection;
        }

        // Keep red movement consistent with incoming travel direction.
        if (Vector3.Dot(zoneDirection, planarVelocity.normalized) < 0f)
        {
            return -zoneDirection;
        }

        return zoneDirection;
    }
}