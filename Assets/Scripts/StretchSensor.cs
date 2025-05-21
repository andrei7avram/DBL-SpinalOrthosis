using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StretchSensor : MonoBehaviour
{
     public Transform shoulderPoint;  // Assign shoulder bone in inspector
    
    [Header("Sensor Settings")]
    public float restZPosition;     // Will be set automatically
    public float restXPosition; // Add this line with your other fields
    public float sensitivity = 1.0f;
    public float maxOutputForce = 10f;
    public bool invertDirection = false; // For left/right shoulder
    
    private float currentZPosition;
    private float outputForce;

    public TextMeshProUGUI forceText; // Optional: for displaying output in UI
    
    void Start()
    {
        // Record initial positions as the rest positions
        restZPosition = shoulderPoint.position.z;
        restXPosition = shoulderPoint.position.x;
    }

    void Update()
    {
        // Get current positions (world space)
        currentZPosition = shoulderPoint.position.z;
        float currentXPosition = shoulderPoint.position.x;

        // Calculate differences from rest positions
        float zDifference = restZPosition - currentZPosition;
        float xDifference = (currentXPosition - restXPosition) * (1f / 3f); // Scale X impact

        // Optionally invert for different shoulder sides
        if (invertDirection) {
            zDifference = -zDifference;
            xDifference = -xDifference;
        }

        // Use the greater positive difference (if both are negative, force is zero)
        float effectiveDifference = Mathf.Max(zDifference, xDifference, 0f);

        // Calculate output force (zero if moving backward/left)
        outputForce = Mathf.Clamp(effectiveDifference * sensitivity, 0f, maxOutputForce);
        
        // Optional: Update UI text
        if (forceText != null)
        {
            forceText.text = $"Force: {outputForce.ToString("F2")}";
        }
    }
    
    // Public method to get the current force reading
    public float GetForceOutput()
    {
        return outputForce;
    }
    
    // Call this to reset the rest position (e.g., if character changes posture)
    public void ResetRestPosition()
    {
        restZPosition = shoulderPoint.position.z;
    }
    
    
}
