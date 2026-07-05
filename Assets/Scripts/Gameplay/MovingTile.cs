using UnityEngine;
using System.Collections.Generic;

public class MovingTile : MonoBehaviour
{
    public Vector3 moveAxis = new Vector3(1f, 0f, 0f);
    public float distance = 2.5f;
    public float speed = 2f;

    private Vector3 _startPos;
    private Vector3 _lastPos;

    private HashSet<Rigidbody> _riders = new HashSet<Rigidbody>();

    void Start()
    {
        _startPos = transform.localPosition;
        _lastPos = transform.position;
    }

    void FixedUpdate()
    {
        transform.localPosition = _startPos + moveAxis * (Mathf.Sin(Time.fixedTime * speed) * distance);
        
        Vector3 currentPos = transform.position;
        Vector3 delta = currentPos - _lastPos;
        _lastPos = currentPos;

        foreach (Rigidbody rb in _riders)
        {
            if (rb != null)
            {
                rb.position += delta;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody != null)
        {
            _riders.Add(collision.rigidbody);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody != null)
        {
            _riders.Remove(collision.rigidbody);
        }
    }
}
