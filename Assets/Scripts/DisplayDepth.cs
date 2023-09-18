using UnityEngine;
using TMPro;  // Required for TextMeshPro

public class DisplayDepth : MonoBehaviour
{
    public Transform playerTransform;  // Reference to the player's transform
    private TextMeshPro textMesh;      // Reference to the TextMeshPro component

    private void Start()
    {
        // Get the TextMeshPro component attached to this object
        textMesh = GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        // Get the player's Y-coordinate and round it
        int roundedY = Mathf.RoundToInt(Mathf.Abs(playerTransform.position.y));

        // Update the text display
        textMesh.text = $"{roundedY} \nMeter tief";
    }
}
