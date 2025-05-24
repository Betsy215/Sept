using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject customerPrefab;    // Assign your Customer Prefab here in the Inspector.
    public float spawnInterval = 5f;     // Time between spawns in seconds.

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnCustomer();
            timer = 0f;
        }
    }

    void SpawnCustomer()
    {
        // Instantiates the customer at the current position of this spawner
        Instantiate(customerPrefab, transform.position, Quaternion.identity);
    }
}