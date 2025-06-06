using System.Collections.Generic;
using UnityEngine;

public class RandomPose : MonoBehaviour
{
    public Parent parentScript;
    public StretchSensorR stretchSensor;

    [Header("Output: Stretch values for each sensor")]
    public float[] stretchValues; // [Red, Green, Blue, Purple, Orange, Cyan]

    private List<Vector3> initialEulerAngles;

    void Start()
    {
        if (parentScript == null) parentScript = GetComponent<Parent>();
        if (stretchSensor == null) stretchSensor = GetComponent<StretchSensorR>();

        // Store initial euler angles
        initialEulerAngles = new List<Vector3>();
        foreach (var bone in parentScript.rigBones)
        {
            initialEulerAngles.Add(bone != null ? bone.localEulerAngles : Vector3.zero);
        }

    }

    [ContextMenu("Generate Random Pose and Store Stretch")]
    public void GenerateRandomPoseAndStoreStretch()
    {
        var rigBones = parentScript.rigBones;

        // --- Reset all bones to their initial rotation ---
        for (int i = 0; i < rigBones.Count; i++)
        {
            if (rigBones[i] == null) continue;
            rigBones[i].localEulerAngles = initialEulerAngles[i];
        }

        // --- Generate new random pose ---
        for (int i = 0; i < rigBones.Count; i++)
        {
            if (rigBones[i] == null) continue;
            Vector3 randomEuler = initialEulerAngles[i];
            randomEuler.x += Random.Range(-10f, 10f);
            randomEuler.y += Random.Range(-10f, 10f);
            randomEuler.z += Random.Range(-10f, 10f);
            rigBones[i].localEulerAngles = randomEuler;
        }

        // Store the stretch values from StretchSensorR
        stretchValues = new float[6];
        stretchValues[0] = stretchSensor._currentForceRed;
        stretchValues[1] = stretchSensor._currentForceGreen;
        stretchValues[2] = stretchSensor._currentForceBlue;
        stretchValues[3] = stretchSensor._currentForcePurple;
        stretchValues[4] = stretchSensor._currentForceOrange;
        stretchValues[5] = stretchSensor._currentForceCyan;

        Debug.Log($"Random pose generated. Stretch values: Red={stretchValues[0]}, Green={stretchValues[1]}, Blue={stretchValues[2]}, Purple={stretchValues[3]}, Orange={stretchValues[4]}, Cyan={stretchValues[5]}");
    }
}
