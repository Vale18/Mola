using UnityEngine;

public class UnderwaterLightController : MonoBehaviour
{
    public Transform playerTransform;     // Reference to the player's transform
    public float activationDepth = -120f; // Depth at which lights are activated
    public Light[] underwaterLights;      // Array to store all underwater lights

    private void Update()
    {
        // Check player's depth
        if (playerTransform.position.y <= activationDepth)
        {
            // Activate all lights
            foreach (Light light in underwaterLights)
            {
                light.enabled = true;
            }
        }
        else
        {
            // Deactivate all lights
            foreach (Light light in underwaterLights)
            {
                light.enabled = false;
            }
        }
    }
}
