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
        for (float depth = -5; depth >= -50f; depth -= 10f) // You can adjust the step value as needed
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

    private GameObject SpawnFish(GameObject fishPrefab, Vector3 spawnPosition, Quaternion rotation)
    {
        GameObject fish = Instantiate(fishPrefab, spawnPosition, rotation);
    
        // Attach the FishMover script to the instantiated fish
        if (fish.GetComponent<FishMover>() == null)
        {
            fish.AddComponent<FishMover>();
        }

        Destroy(fish, 14f);
        return fish;
    }

    private Quaternion InitialSpawnRotation(Vector3 spawnPosition)
    {
        Vector3 directionToCenter = (new Vector3(0, spawnPosition.y, 0) - spawnPosition).normalized;
        Vector3 randomRotationAxis = Vector3.Cross(Vector3.up, Random.insideUnitSphere).normalized;

        // This will ensure the fish doesn't face upwards, but still has a random orientation in the horizontal plane
        return Quaternion.LookRotation(Vector3.ProjectOnPlane(directionToCenter, Vector3.up), Vector3.up);
    }

    private void SpawnRandomFish()
    {
        Vector3 randomDirection = Random.insideUnitSphere.normalized;
        Vector3 spawnPosition = player.position + randomDirection * spawnRadius;
        spawnPosition.y = player.position.y - 50f;


        int randomFishIndex = Random.Range(0, fishPrefabs.Length);
       // SpawnFish(fishPrefabs[randomFishIndex], spawnPosition, randomRotation);

        Quaternion randomRotation = RandomRotationAvoidingCenter(spawnPosition);
        SpawnFish(fishPrefabs[randomFishIndex], spawnPosition, randomRotation);
    }

    // Function to spawn fish at a specific depth
    private void SpawnRandomFishAtDepth(float depth)
    {
        Vector3 randomDirection = Random.insideUnitSphere.normalized;
        Vector3 spawnPosition = player.position + randomDirection * spawnRadius;
        spawnPosition.y = depth;

        // Quaternion randomRotation = RandomRotationAvoidingPlayer(spawnPosition);

        int randomFishIndex = Random.Range(0, fishPrefabs.Length);
        //SpawnFish(fishPrefabs[randomFishIndex], spawnPosition, randomRotation);
        

        Quaternion rotation;
        if (depth >= -100f)
        {
            rotation = InitialSpawnRotation(spawnPosition);
        }
        else
        {
            rotation = RandomRotationAvoidingCenter(spawnPosition);
        }
    
        SpawnFish(fishPrefabs[randomFishIndex], spawnPosition, rotation);
    }


    private Quaternion RandomRotationAvoidingCenter(Vector3 spawnPosition)
    {
        Vector3 directionToCenter = (new Vector3(0, spawnPosition.y, 0) - spawnPosition).normalized;
        Vector3 randomRotationAxis = Vector3.Cross(directionToCenter, Random.insideUnitSphere).normalized;

        return Quaternion.AngleAxis(Random.Range(0, 360f), randomRotationAxis);
    }
}
