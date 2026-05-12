using UnityEngine;

public class BallController : MonoBehaviour
{
    public float forceStrength = 20f;
    public float maxPlanarSpeed = 4f;
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

        if (currentZone != null || timeSinceLastZoneContact <= zoneRetentionTime)
        {
            Vector3 direction = currentZone != null ? currentZone.GetForceDirection() : Vector3.zero;
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
        else if (dampWhenNoZone)
        {
            ApplyIdleDamping();
        }

        ClampPlanarSpeed();
    }

    void OnTriggerEnter(Collider other)
    {
        TileZone zone = other.GetComponent<TileZone>() ?? other.GetComponentInParent<TileZone>();
        if (zone != null)
        {
            currentZone = zone;
        }
    }

    void OnTriggerExit(Collider other)
    {
        TileZone zone = other.GetComponent<TileZone>() ?? other.GetComponentInParent<TileZone>();
        if (zone == currentZone)
        {
            currentZone = null;
        }
    }

    private void ResolveZoneFromGroundProbe()
    {
        Vector3 origin = transform.position + Vector3.up * 0.15f;
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            zoneProbeRadius,
            Vector3.down,
            zoneProbeDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        TileZone nearestZone = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.attachedRigidbody == rb)
            {
                continue;
            }

            TileZone zone = hits[i].collider.GetComponent<TileZone>() ?? hits[i].collider.GetComponentInParent<TileZone>();
            if (zone != null && hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                nearestZone = zone;
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