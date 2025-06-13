using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPose : MonoBehaviour
{
    public StretchSensorR stretchSensor;
    public SelectBones selectBonesScript;

    [Header("Output: Stretch values for each sensor")]
    public float[] stretchValues; // [Red, Green, Blue, Purple, Orange, Cyan]

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
    }

    [ContextMenu("Generate Random Pose and Store Stretch")]
    public void GenerateRandomPoseAndStoreStretch()
    {
        StartCoroutine(GeneratePoseCoroutine());
    }

    private IEnumerator GeneratePoseCoroutine()
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

        // --- Generate new random pose for halfRigBones ---
        for (int i = 0; i < halfRigBones.Count; i++)
        {
            if (halfRigBones[i] == null) continue;
            Vector3 randomEuler = initialEulerAngles[i * 2];
            randomEuler.x += Random.Range(-5f, 5f);
            randomEuler.y += Random.Range(-5f, 5f);
            randomEuler.z += Random.Range(-5f, 5f);
            halfRigBones[i].localEulerAngles = randomEuler;
        }

        // CRITICAL: Wait for sensors to update after bone movements
        yield return new WaitForFixedUpdate(); // Wait for physics update
        yield return null; // Wait one more frame

        // Now read the updated sensor values
        stretchValues = new float[6];
        stretchValues[0] = stretchSensor._currentForceRed;
        stretchValues[1] = stretchSensor._currentForceGreen;
        stretchValues[2] = stretchSensor._currentForceBlue;
        stretchValues[3] = stretchSensor._currentForcePurple;
        stretchValues[4] = stretchSensor._currentForceOrange;
        stretchValues[5] = stretchSensor._currentForceCyan;

        // DEBUG: Check if we got non-zero values
        //Debug.Log($"RandomPose: Generated stretch values: [{stretchValues[0]:F3}, {stretchValues[1]:F3}, {stretchValues[2]:F3}, {stretchValues[3]:F3}, {stretchValues[4]:F3}, {stretchValues[5]:F3}]");
        
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
            //Debug.LogWarning("RandomPose: All stretch values are still zero after pose generation!");
        }
    }
}