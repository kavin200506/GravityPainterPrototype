using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Hurdle Prefabs")]
    public GameObject[] easyHurdles;
    public GameObject[] hardHurdles;

    [Header("Speed Settings")]
    public float startingSpeed = 5f;
    public float maxSpeed = 25f;
    public float accelerationRate = 0.1f;
    private float currentSpeed;

    [Header("Spawn Settings")]
    public float initialSpawnGap = 3f;
    private float currentSpawnGap;
    private float spawnTimer;

    void Start()
    {
        currentSpeed = startingSpeed;
        currentSpawnGap = initialSpawnGap;
        spawnTimer = currentSpawnGap;
    }

    void Update()
    {
        if (currentSpeed < maxSpeed)
        {
            currentSpeed += accelerationRate * Time.deltaTime;
        }

        if (currentSpawnGap > 1.2f)
        {
            currentSpawnGap -= 0.005f * Time.deltaTime;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnHurdle();
            spawnTimer = currentSpawnGap;
        }
    }

    void SpawnHurdle()
    {
        GameObject selectedPrefab;

        // Write the code here on line 51:
        if (currentSpeed < 10f)
        {
            selectedPrefab = easyHurdles[Random.Range(0, easyHurdles.Length)];
        }
        else
        {
            selectedPrefab = hardHurdles[Random.Range(0, hardHurdles.Length)];
        }
    }

}
