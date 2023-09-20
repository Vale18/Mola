using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour 
{
    public float minIntensity = 0.25f;
    public float maxIntensity = 0.5f;

    public float flickerSpeed = 0.07f;
    

    private Light lightSource;
    private float randomizer;

    private void Start()
    {
        lightSource = GetComponent<Light>();
        randomizer = Random.Range(0.0f, 0.2f);
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(randomizer, Time.time * flickerSpeed);
        lightSource.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}