using UnityEngine;

public class MovingHurdles : MonoBehaviour
{
    [HideInInspector]
    public float moveSpeed = 5f; // Set dynamically by the level manager

    void Update()
    {
        // Move the obstacle backward toward the player
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);

        // If the hurdle goes far behind the player (Z = -10), delete it
        if (transform.position.z < -10f)
        {
            Destroy(gameObject);
        }
    }
}
