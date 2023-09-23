using UnityEngine;
using StylizedWater2;

public class DepthAdjuster : MonoBehaviour
{
    public Transform player;  // Reference to the player's transform
    private Component underwaterRenderer;  // Reference to the UnderwaterRenderer component as a generic Component
    private bool hasAdjusted = false;  // Flag to ensure we only adjust once
    public float activationDepth = 200f;

    private void Start()
    {
        underwaterRenderer = GetComponent<UnderwaterRenderer>();  // Get the UnderwaterRenderer component
        
        if (underwaterRenderer == null)
        {
            Debug.LogError("Underwater Renderer component not found on the object!");
        }
    }

    private void Update()
    {
        if (!hasAdjusted && player.position.y <= - activationDepth)  // Check player's depth
        {
            if (underwaterRenderer == null)
            {
                Debug.LogError("Underwater Renderer is null!");
                return;
            }

            // Use reflection to set the property
            System.Type type = underwaterRenderer.GetType();
            System.Reflection.FieldInfo field1 = type.GetField("fogDensity");
            if (field1 != null)
            {
                field1.SetValue(underwaterRenderer, 3f);
            }
            else
            {
                Debug.LogError("fogDensity property not found on the Underwater Renderer!");
            }
              
            //-----------------------------------------------
            System.Reflection.FieldInfo field2 = type.GetField("heightFogDepth");
            if (field2 != null)
            {
                field2.SetValue(underwaterRenderer, 0f);
            }
            else
            {
                Debug.LogError("heightFogDepth property not found on the Underwater Renderer!");
            }
            
            //-----------------------------------------------
            System.Reflection.FieldInfo field3 = type.GetField("heightFogDensity");
            if (field3 != null)
            {
                field3.SetValue(underwaterRenderer, 0.001f);
            }
            else
            {
                Debug.LogError("heightFogDensity property not found on the Underwater Renderer!");
            }
            
            //-----------------------------------------------
            System.Reflection.FieldInfo field4 = type.GetField("fogBrightness");
            if (field4 != null)
            {
                field4.SetValue(underwaterRenderer, 0f);
            }
            else
            {
                Debug.LogError("fogBrightness property not found on the Underwater Renderer!");
            }
            hasAdjusted = true; // Set the flag to true to prevent further adjustments
        }
    }
}
