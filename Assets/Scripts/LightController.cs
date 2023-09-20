using UnityEngine;
using System.Collections.Generic;
using System;
public class LightController : MonoBehaviour
{
    public GameObject player; // Reference to the player's transform
    private bool lightsOn = false;
    private List<Light> pointLights = new List<Light>();
    private List<Light> spotLights = new List<Light>();
    private Light directionalLight;

    private List<FlickeringLight> flickeringLights = new List<FlickeringLight>();

    [Header("Light Intensity Settings")]
    public float desiredIntensity = 1.0f;
    public float activationDepth = -30f; // Depth at which lights are activated
    void Awake()
    {
        // Alle Lichter in der Szene sammeln
        Light[] allLights = FindObjectsOfType<Light>();

        foreach (Light light in allLights)
        {
            switch (light.type)
            {
                case LightType.Point:
                    pointLights.Add(light);
                    break;
                case LightType.Spot:
                    spotLights.Add(light);
                    break;
                case LightType.Directional:
                    directionalLight = light;
                    break;
            }
        }
        ToggleAllLights(false);

        // Alle FlickeringLight-Komponenten in der Szene sammeln
        flickeringLights.AddRange(FindObjectsOfType<FlickeringLight>());

    }
    void Update()
    {
        // Check player's depth
        if (player.transform.position.y <= activationDepth)
        {
            if (!lightsOn)
            {
                lightsOn = true;
                ToggleAllLights(true);
            }
            FadeOutDirectionalLight();
        }
    }

    private void ExecuteOnEachLight(List<Light> lights, Action<Light> action)
    {
        foreach (Light light in lights)
        {
            action(light);
        }
    }

    public void ToggleFlickering(bool isActive)
    {
        foreach (FlickeringLight flickeringLight in flickeringLights)
        {
            flickeringLight.enabled = isActive;
        }
    }

    public void SetLightIntensity(float intensity)
    {
        ExecuteOnEachLight(pointLights, light => light.intensity = intensity);
        ExecuteOnEachLight(spotLights, light => light.intensity = intensity);
    }

    [ContextMenu("Apply Desired Intensity")]
    public void ApplyDesiredIntensity()
    {
        SetLightIntensity(desiredIntensity);
    }

    public void ToggleAllLights(bool isActive)
    {
        ExecuteOnEachLight(pointLights, light => light.enabled = isActive);
        ExecuteOnEachLight(spotLights, light => light.enabled = isActive);
    }
    private void FadeOutDirectionalLight()
    {
        if (directionalLight != null && directionalLight.enabled)
        {
            directionalLight.intensity -= 0.001f;
            if(directionalLight.intensity <= 0)
            {
                directionalLight.enabled = false;
            }
        }
    }
}
