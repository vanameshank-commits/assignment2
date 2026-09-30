using UnityEngine;
using TMPro; // Required for TextMeshPro

public class AltitudeDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private TextMeshProUGUI altitudeText; // Changed to TextMeshProUGUI

    [Header("Settings")]
    [SerializeField] private string prefix = "ALT: ";
    [SerializeField] private string suffix = "m";
    [SerializeField] private float groundLevelY = 0f;

    private void Update()
    {
        if (playerTransform != null && altitudeText != null)
        {
            // Calculate the height relative to ground level
            float currentAltitude = playerTransform.position.y - groundLevelY;

            // Update the TMP text
            altitudeText.text = prefix + currentAltitude.ToString("F1") + suffix;
        }
    }
}