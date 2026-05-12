using UnityEngine;

/// <summary>
/// Attach to Main Camera. Assign the ball (or any target). Uses LateUpdate so it runs after movement.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("World-space offset from target (e.g. back and up).")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 8f, -10f);
    [Tooltip("Higher = snappier follow.")]
    [SerializeField] private float smoothTime = 0.2f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        transform.LookAt(target.position + Vector3.up * 0.5f);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
