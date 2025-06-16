using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BadPosture
{
    public string name; // "Lordosis", "Scoliosis", "Forward Head", etc.
    [Header("Bone Rotation Offsets (13 bones - X, Y, Z for each)")]
    public Vector3[] boneRotationOffsets = new Vector3[13]; // Offsets from neutral position
    [Range(0f, 5f)]
    public float variationRange = 3f; // ±3 degree random variation
    public bool affectX = true; // Affect X rotation
    public bool affectY = true; // Affect Y rotation
    public bool affectZ = true; // Affect Z rotation
}

public class RandomPose : MonoBehaviour
{
    public StretchSensorR stretchSensor;
    public SelectBones selectBonesScript;

    [Header("Medical Posture Presets")]
    public BadPosture[] badPostures = new BadPosture[5]; // 6 main bad postures

    [Header("Output: Stretch values for each sensor")]
    public float[] stretchValues; // [Red, Green, Blue, Purple, Orange, Cyan, Black, Pink]

    private List<Vector3> initialEulerAngles;
    public List<Transform> halfRigBones; // Stores every other bone from SelectBones.boneArray

    void Start()
    {
        if (selectBonesScript == null) selectBonesScript = GetComponent<SelectBones>();
        if (stretchSensor == null) stretchSensor = GetComponent<StretchSensorR>();

        // Build bone hierarchy first
        if (selectBonesScript != null)
        {
            selectBonesScript.BuildBoneHierarchy();
        }

        // Use SelectBones.boneArray instead of parentScript.rigBones
        halfRigBones = new List<Transform>();
        initialEulerAngles = new List<Vector3>();
        if (selectBonesScript != null && selectBonesScript.boneArray != null)
        {
            for (int i = 0; i < selectBonesScript.boneArray.Length; i += 2)
            {
                halfRigBones.Add(selectBonesScript.boneArray[i]);
            }
            foreach (var bone in selectBonesScript.boneArray)
            {
                initialEulerAngles.Add(bone != null ? bone.localEulerAngles : Vector3.zero);
            }
        }
        else
        {
            Debug.LogError("SelectBones script or boneArray not assigned!");
        }

        // Initialize bad posture arrays if empty
        for (int i = 0; i < badPostures.Length; i++)
        {
            if (badPostures[i] == null)
            {
                badPostures[i] = new BadPosture();
                badPostures[i].name = $"Bad Posture {i + 1}";
                badPostures[i].boneRotationOffsets = new Vector3[13];
            }
        }
    }

    [ContextMenu("Generate Medical Posture and Store Stretch")]
    public void GenerateRandomPoseAndStoreStretch()
    {
        StartCoroutine(GenerateMedicalPostureCoroutine());
    }

    // Made this public so MLAgent can wait for it
    public IEnumerator GenerateMedicalPostureCoroutine()
    {
        // --- Reset all bones in halfRigBones to their initial rotation ---
        for (int i = 0; i < halfRigBones.Count; i++)
        {
            if (halfRigBones[i] == null) continue;
            // Use the corresponding initialEulerAngles (every other index)
            halfRigBones[i].localEulerAngles = initialEulerAngles[i * 2];
        }

        // Wait a frame for the reset to take effect
        yield return null;

        // --- Randomly select one of the 6 bad postures ---
        int randomPostureIndex = Random.Range(0, badPostures.Length);
        BadPosture selectedPosture = badPostures[randomPostureIndex];

        Debug.Log($"RandomPose: Generating {selectedPosture.name}");

        // --- Apply the selected bad posture with random variation ---
        for (int i = 0; i < halfRigBones.Count && i < selectedPosture.boneRotationOffsets.Length; i++)
        {
            if (halfRigBones[i] == null) continue;

            // Start with initial neutral position
            Vector3 baseRotation = initialEulerAngles[i * 2];
            
            // Add the medical posture offset
            Vector3 postureOffset = selectedPosture.boneRotationOffsets[i];
            
            // Add random variation for realism
            Vector3 randomVariation = new Vector3(
                Random.Range(-selectedPosture.variationRange * (selectedPosture.affectX ? 1f: 0f), selectedPosture.variationRange * (selectedPosture.affectX ? 1f: 0f)),
                Random.Range(-selectedPosture.variationRange * (selectedPosture.affectY ? 1f: 0f), selectedPosture.variationRange * (selectedPosture.affectY ? 1f: 0f)),
                Random.Range(-selectedPosture.variationRange * (selectedPosture.affectZ ? 1f: 0f), selectedPosture.variationRange * (selectedPosture.affectZ ? 1f: 0f))
            );

            // Apply: neutral + medical offset + random variation
            Vector3 finalRotation = baseRotation + postureOffset + randomVariation;
            halfRigBones[i].localEulerAngles = finalRotation;
        }

        // CRITICAL: Wait for sensors to update after bone movements
        yield return new WaitForFixedUpdate(); // Wait for physics update
        yield return null; // Wait one more frame

        // Now read the updated sensor values
        stretchValues = new float[8];
        stretchValues[0] = stretchSensor._currentForceRed;
        stretchValues[1] = stretchSensor._currentForceGreen;
        stretchValues[2] = stretchSensor._currentForceBlue;
        stretchValues[3] = stretchSensor._currentForcePurple;
        stretchValues[4] = stretchSensor._currentForceOrange;
        stretchValues[5] = stretchSensor._currentForceCyan;
        stretchValues[6] = stretchSensor._currentForceBlack; // NEW
        stretchValues[7] = stretchSensor._currentForcePink;  // NEW

        // DEBUG: Check if we got non-zero values
        Debug.Log($"RandomPose: {selectedPosture.name} generated stretch values: [{string.Join(", ", stretchValues)}]");
        
        bool allZeros = true;
        for (int i = 0; i < stretchValues.Length; i++)
        {
            if (stretchValues[i] != 0f)
            {
                allZeros = false;
                break;
            }
        }
        
        if (allZeros)
        {
            Debug.LogWarning($"RandomPose: All stretch values are still zero after generating {selectedPosture.name}!");
        }
    }

    // Helper method to test individual postures
    [ContextMenu("Test Specific Posture (Index 0)")]
    public void TestFirstPosture()
    {
        if (badPostures.Length > 0)
        {
            StartCoroutine(TestSpecificPosture(0));
        }
    }

    private IEnumerator TestSpecificPosture(int postureIndex)
    {
        if (postureIndex >= badPostures.Length) yield break;

        BadPosture testPosture = badPostures[postureIndex];
        //Debug.Log($"Testing {testPosture.name}...");

        // Reset bones
        for (int i = 0; i < halfRigBones.Count; i++)
        {
            if (halfRigBones[i] == null) continue;
            halfRigBones[i].localEulerAngles = initialEulerAngles[i * 2];
        }

        yield return null;

        // Apply posture without variation
        for (int i = 0; i < halfRigBones.Count && i < testPosture.boneRotationOffsets.Length; i++)
        {
            if (halfRigBones[i] == null) continue;
            Vector3 finalRotation = initialEulerAngles[i * 2] + testPosture.boneRotationOffsets[i];
            halfRigBones[i].localEulerAngles = finalRotation;
        }

        yield return new WaitForFixedUpdate();
        yield return null;

        // Read sensors
        float[] testValues = new float[8];
        testValues[0] = stretchSensor._currentForceRed;
        testValues[1] = stretchSensor._currentForceGreen;
        testValues[2] = stretchSensor._currentForceBlue;
        testValues[3] = stretchSensor._currentForcePurple;
        testValues[4] = stretchSensor._currentForceOrange;
        testValues[5] = stretchSensor._currentForceCyan;
        testValues[6] = stretchSensor._currentForceBlack; // NEW
        testValues[7] = stretchSensor._currentForcePink;  // NEW

        //Debug.Log($"Test {testPosture.name}: [{testValues[0]:F3}, {testValues[1]:F3}, {testValues[2]:F3}, {testValues[3]:F3}, {testValues[4]:F3}, {testValues[5]:F3}]");
    }
}