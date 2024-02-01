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
        // Bestimmen der Tiefe basierend auf dem Y-Wert des Spielers
        int depth;
        if (playerTransform.position.y > 0)
        {
            // Wenn der Y-Wert positiv ist, Tiefe als 0 anzeigen
            depth = 0;
        }
        else
        {
            // Wenn der Y-Wert negativ ist, den absoluten Wert für die Tiefe verwenden
            depth = Mathf.RoundToInt(Mathf.Abs(playerTransform.position.y));
        }

        // Update the text display
        textMesh.text = $"{depth} \nMeter tief";
    }
}
