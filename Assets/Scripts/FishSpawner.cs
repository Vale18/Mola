using System.Collections;
using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    public GameObject[] fishPrefabs; // Assign your 3 fish models here
    public Transform player; // Assign the player's transform here
    public float spawnRadius = 50f; // Distance around the player where fishes will spawn
    public float density = 0.01f; // The density of fishes, adjust this to spawn more or fewer fishes
    public float spawnInterval = 1f; // How often to spawn fishes

    private void Start()
    {
        // Initial spawn to fill the area from 0 Y to -50 Y
        for (float depth = 0; depth >= -50f; depth -= 10f) // You can adjust the step value as needed
        {
            int fishCount = Mathf.FloorToInt(density * spawnRadius * spawnRadius);
            for (int i = 0; i < fishCount; i++)
            {
                SpawnRandomFishAtDepth(depth);
            }
        }

        StartCoroutine(SpawnFishes());
    }

    private IEnumerator SpawnFishes()
    {
        while (true)
        {
            int fishCount = Mathf.FloorToInt(density * spawnRadius * spawnRadius);
            for (int i = 0; i < fishCount; i++)
            {
                SpawnRandomFish();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnRandomFish()
    {
        Vector3 randomDirection = Random.insideUnitSphere.normalized;
        Vector3 spawnPosition = player.position + randomDirection * spawnRadius;
        
        // Making sure the fish spawns 50 meters below the player
        spawnPosition.y = player.position.y - 50f;

        int randomFishIndex = Random.Range(0, fishPrefabs.Length);
        GameObject fish = Instantiate(fishPrefabs[randomFishIndex], spawnPosition, Quaternion.identity);

        // Optionally destroy the fish after a certain amount of time to prevent too many objects in the scene
        Destroy(fish, 14f); //change depending on the speed (time for 50 meters * 2)
    }

    // Function to spawn fish at a specific depth
    private void SpawnRandomFishAtDepth(float depth)
    {
        Vector3 randomDirection = Random.insideUnitSphere.normalized;
        Vector3 spawnPosition = player.position + randomDirection * spawnRadius;

        // Set the fish's depth
        spawnPosition.y = depth;

        int randomFishIndex = Random.Range(0, fishPrefabs.Length);
        GameObject fish = Instantiate(fishPrefabs[randomFishIndex], spawnPosition, Quaternion.identity);
        
        // Optionally destroy the fish after a certain amount of time to prevent too many objects in the scene
        Destroy(fish, 14f); //change depending on the speed (time for 50 meters *2)
    }
}
