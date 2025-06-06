using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PostureMonitor : MonoBehaviour
{
    public TextMeshProUGUI postureWarning;

    [Header("Spine Vertebrae")]
    public List<Transform> vertebrae;

    [Header("Spine Region Indices")]
    public int lumbarStart = 0;
    public int thoracicStart = 5;
    public int cervicalStart = 17;
    public int lastBone = 23;

    [Header("Angle Thresholds")]
    public float maxLumbarCurve = 15f; // lordosis
    public float maxThoracicCurve = 20f; // kyphosis
    public float maxCervicalCurve = 10f; // forward neck

    

    [Header("Timing Settings")]
    public float postureHoldTime = 5f; // wait 5 seconds
    private float lumbarTimer = 0f;
    private float thoracicTimer = 0f;
    private float cervicalTimer = 0f;

    private bool hasLordosis = false;
    private bool hasKyphosis = false;
    private bool hasForwardNeck = false;

    void Update()
    {
        float lumbarBend = CalculateCurveAngle(lumbarStart, thoracicStart);
        float thoracicBend = CalculateCurveAngle(thoracicStart, cervicalStart);
        float cervicalBend = CalculateCurveAngle(cervicalStart, lastBone);


        Debug.Log($"Lumbar Bend: {lumbarBend:F2}, Thoracic Bend: {thoracicBend:F2}, Cervical Bend: {cervicalBend:F2}");
        // Debug.Log($"Lumbar Bend: {lumbarBend:F2}, Thoracic Bend: {thoracicBend:F2}");


        // Check lumbar (lordosis)
        if (lumbarBend > maxLumbarCurve)
        {
            lumbarTimer += Time.deltaTime;
            Debug.Log($"Holding bad lumbar posture: {lumbarTimer:F1}/{postureHoldTime}");

            if (lumbarTimer >= postureHoldTime && !hasLordosis)
            {
                hasLordosis = true;
                postureWarning.text = "Lordosis!";
            }
        }
        else
        {
            lumbarTimer = 0f;
            hasLordosis = false;
        }

        // Check thoracic (kyphosis)
        if (thoracicBend > maxThoracicCurve)
        {
            thoracicTimer += Time.deltaTime;
            Debug.Log($"Holding bad thoracic posture: {thoracicTimer:F1}/{postureHoldTime}");
            if (thoracicTimer >= postureHoldTime && !hasKyphosis)
            {
                hasKyphosis = true;
                postureWarning.text = "Kyphosis!";
            }
        }
        else
        {
            thoracicTimer = 0f;
            hasKyphosis = false;
        }

        // Check cervical (forward neck)
        if (cervicalBend > maxCervicalCurve)
        {
            cervicalTimer += Time.deltaTime;
            Debug.Log($"Holding bad cervical posture: {cervicalTimer:F1}/{postureHoldTime}");

            if (cervicalTimer >= postureHoldTime && !hasForwardNeck)
            {
                hasForwardNeck = true;
                postureWarning.text = "Forward neck!";
            }
        }
        else
        {
            cervicalTimer = 0f;
            hasForwardNeck = false;
        }

        if (!hasKyphosis && !hasLordosis && !hasForwardNeck)
        {
            postureWarning.text = "";
        }
        // if (!hasKyphosis && !hasLordosis)
        // {
        //     postureWarning.text = "";
        // }

    }

    // Measures average angle between bones in a region
    float CalculateCurveAngle(int startIndex, int endIndex)
    {
        float totalAngle = 0f;
        int count = 0;

        for (int i = startIndex + 1; i < endIndex && i < vertebrae.Count; i++)
        {
            var prev = vertebrae[i - 1];
            var curr = vertebrae[i];

            if (prev == null || curr == null) continue;

            float angle = Quaternion.Angle(prev.localRotation, curr.localRotation);
            totalAngle += angle;
            count++;
        }

        return count > 0 ? totalAngle : 0f;
    }
}
