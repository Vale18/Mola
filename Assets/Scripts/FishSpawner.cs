using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    public Transform player;
    public float spawnRadius = 100f;
    public float minSpawnRadius = 20f;
    private float timeSinceLastSpawn = 0f;
    private int currentZoneDensity;
    private float spawnInterval;
    
    [Header("Zone 1: 0m - 200m")]
    public List<GameObject> zone1FishPrefabs;
    public int zone1Density = 1;

    [Header("Zone 2: 200m - 1000m")]
    public List<GameObject> zone2FishPrefabs;
    public int zone2Density = 1;

    [Header("Zone 3: 1000m - 4000m")]
    public List<GameObject> zone3FishPrefabs;
    public int zone3Density = 1;

    [Header("Zone 4: 4000m - 6000m")]
    public List<GameObject> zone4FishPrefabs;
    public int zone4Density = 1;

    [Header("Zone 5: 6000m and below")]
    public List<GameObject> zone5FishPrefabs;
    public int zone5Density = 1;

    void Start()
    {
        currentZoneDensity = zone1Density;

        float spawnInterval = 1f / currentZoneDensity;
        int fishCount = Mathf.FloorToInt(currentZoneDensity * spawnRadius);

        for (int i = 0; i < fishCount; i++)
        {
            SpawnRandomFishAtDepth(Random.Range(-100f, -10f));
            timeSinceLastSpawn = 0f;
        }
        InvokeRepeating(nameof(SpawnRandomFish), spawnInterval, spawnInterval);
    }


    private void UpdateCurrentZoneDensity()
    {
        float currentDepth = -player.position.y + 100;

        if (currentDepth < 200)
        {
            currentZoneDensity = zone1Density;
        }
        else if (currentDepth < 1000)
        {
            currentZoneDensity = zone2Density;
        }
        else if (currentDepth < 4000)
        {
            currentZoneDensity = zone3Density;
        }
        else if (currentDepth < 6000)
        {
            currentZoneDensity = zone4Density;
        }
        else
        {
            currentZoneDensity = zone5Density;
        }
    }


    void Update()
    {
        UpdateCurrentZoneDensity();  // Continuously update the current zone density

        timeSinceLastSpawn += Time.deltaTime;
        float spawnInterval = 1f / currentZoneDensity;

        if (timeSinceLastSpawn > spawnInterval)
        {
            SpawnRandomFish();
            timeSinceLastSpawn = 0f;
        }
    }

    private IEnumerator SpawnFishes()
    {
        while (true)
        {
            int fishCount = Mathf.FloorToInt(currentZoneDensity * spawnRadius * spawnRadius);
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

        Destroy(fish, 60f);
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
        List<GameObject> currentZoneFishPrefabs;
        int currentZoneDensity;

        float currentDepth = -player.position.y + 100;

        if (currentDepth < 200)
        {
            currentZoneFishPrefabs = zone1FishPrefabs;
            currentZoneDensity = zone1Density;
        }
        else if (currentDepth < 1000)
        {
            currentZoneFishPrefabs = zone2FishPrefabs;
            currentZoneDensity = zone2Density;
        }
        else if (currentDepth < 4000)
        {
            currentZoneFishPrefabs = zone3FishPrefabs;
            currentZoneDensity = zone3Density;
        }
        else if (currentDepth < 6000)
        {
            currentZoneFishPrefabs = zone4FishPrefabs;
            currentZoneDensity = zone4Density;
        }
        else
        {
            currentZoneFishPrefabs = zone5FishPrefabs;
            currentZoneDensity = zone5Density;
        }

        for (int i = 0; i < currentZoneDensity; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere.normalized;
            Vector3 spawnPosition;
            float distanceFromCenter;
            int maxAttempts = 10;  // Maximum number of attempts to find a spawn position
            int currentAttempt = 0;

            do
            {
                spawnPosition = player.position + randomDirection * spawnRadius;
                distanceFromCenter = Vector3.Distance(new Vector3(0, spawnPosition.y, 0), spawnPosition);
                currentAttempt++;
                if (currentAttempt >= maxAttempts)
                {
                    return;  // Exit the loop if maximum attempts reached
                }
            }
            while (distanceFromCenter < minSpawnRadius);

            spawnPosition.y = player.position.y - 100f;

            int randomFishIndex = Random.Range(0, currentZoneFishPrefabs.Count);
            Quaternion randomRotation = RandomRotationAvoidingCenter(spawnPosition);
            SpawnFish(currentZoneFishPrefabs[randomFishIndex], spawnPosition, randomRotation);
        }
    }



    // Function to spawn fish at a specific depth
    private void SpawnRandomFishAtDepth(float depth)
    {
        Vector3 randomDirection = Random.onUnitSphere;
        float randomDistance = Random.Range(minSpawnRadius, spawnRadius);
        Vector3 spawnPosition = player.position + randomDirection * randomDistance;
        float distanceFromCenter;
        int maxAttempts = 10;  // Maximum number of attempts to find a spawn position
        int currentAttempt = 0;

        do
        {
            
            distanceFromCenter = Vector3.Distance(new Vector3(0, spawnPosition.y, 0), spawnPosition);
            currentAttempt++;
            if (currentAttempt >= maxAttempts)
            {
                return;  // Exit the method if maximum attempts reached
            }
        }
        while (distanceFromCenter < minSpawnRadius);

        spawnPosition.y = depth;

        int randomFishIndex = Random.Range(0, zone1FishPrefabs.Count);

        Quaternion randomRotation = RandomRotationAvoidingCenter(spawnPosition);
        SpawnFish(zone1FishPrefabs[randomFishIndex], spawnPosition, randomRotation);
    }



    private Quaternion RandomRotationAvoidingCenter(Vector3 spawnPosition)
    {
        const float avoidAngleMargin = 5f; // Margin of degrees to avoid the center

        Vector3 directionToCenter = (new Vector3(0, spawnPosition.y, 0) - spawnPosition).normalized;
        Vector3 randomDirection;
        float angleToCenter;

        do
        {
            // Generate a random horizontal direction
            randomDirection = Vector3.ProjectOnPlane(Random.insideUnitSphere, Vector3.up).normalized;
            angleToCenter = Vector3.Angle(randomDirection, directionToCenter);
        }
        while (angleToCenter < avoidAngleMargin || angleToCenter > 180f - avoidAngleMargin);  // Check if the angle is too close to the center

        Quaternion baseRotation = Quaternion.LookRotation(randomDirection, Vector3.up);

        // Apply a random tilt up to 30 degrees
        Quaternion tilt = Quaternion.AngleAxis(Random.Range(-30f, 30f), Vector3.forward);
        return baseRotation * tilt;
    }
}
