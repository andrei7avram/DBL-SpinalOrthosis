using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class mlagent : Agent
{
    public Parent parentScript;
    public StretchSensorR stretchSensor;
    public RandomPose randomPoseScript;

    private int boneCount;
    private int actionSizePerBone = 3;
    private List<Vector3> initialBoneEulerAngles;

    public override void Initialize()
    {
        boneCount = parentScript.rigBones.Count;
        Debug.Log($"[mlagent] Initialized with {boneCount} bones.");

        // Store initial bone rotations
        initialBoneEulerAngles = new List<Vector3>();
        foreach (var bone in parentScript.rigBones)
        {
            initialBoneEulerAngles.Add(bone != null ? bone.localEulerAngles : Vector3.zero);
        }
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("[mlagent] OnEpisodeBegin: Generating new random pose.");
        randomPoseScript.GenerateRandomPoseAndStoreStretch();

        // Reset model to initial pose
        for (int i = 0; i < boneCount; i++)
        {
            if (parentScript.rigBones[i] != null)
            {
                parentScript.rigBones[i].localEulerAngles = initialBoneEulerAngles[i];
                Debug.Log($"[mlagent] Bone {i} reset to initial rotation: {initialBoneEulerAngles[i]}");
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("[mlagent] CollectObservations called.");
        // Observe current stretch sensor values
        sensor.AddObservation(stretchSensor._currentForceRed);
        sensor.AddObservation(stretchSensor._currentForceGreen);
        sensor.AddObservation(stretchSensor._currentForceBlue);
        sensor.AddObservation(stretchSensor._currentForcePurple);
        sensor.AddObservation(stretchSensor._currentForceOrange);
        sensor.AddObservation(stretchSensor._currentForceCyan);

        // Observe target stretch values from random pose
        foreach (float target in randomPoseScript.stretchValues)
            sensor.AddObservation(target);

        // Optionally, observe current bone rotations
        for (int i = 0; i < parentScript.rigBones.Count; i++)
        {
            var bone = parentScript.rigBones[i];
            if (bone != null)
            {
                Vector3 euler = bone.localEulerAngles;
                sensor.AddObservation(euler.x);
                sensor.AddObservation(euler.y);
                sensor.AddObservation(euler.z);
                Debug.Log($"[mlagent] Bone {i} rotation observed: {euler}");
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                Debug.Log($"[mlagent] Bone {i} is null, observed as zero.");
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var actionArray = actions.ContinuousActions;
        int idx = 0;
        Debug.Log("[mlagent] OnActionReceived called.");
        for (int i = 0; i < boneCount; i++)
        {
            if (parentScript.rigBones[i] == null)
            {
                Debug.Log($"[mlagent] Bone {i} is null, skipping action.");
                idx += 3;
                continue;
            }
            Vector3 euler = parentScript.rigBones[i].localEulerAngles;
            float dx = Mathf.Clamp(actionArray[idx++], -10f, 10f);
            float dy = Mathf.Clamp(actionArray[idx++], -10f, 10f);
            float dz = Mathf.Clamp(actionArray[idx++], -10f, 10f);
            euler.x += dx;
            euler.y += dy;
            euler.z += dz;
            parentScript.rigBones[i].localEulerAngles = euler;
            Debug.Log($"[mlagent] Bone {i} action: Δx={dx:F2}, Δy={dy:F2}, Δz={dz:F2} -> new rotation: {euler}");
        }

        // Calculate reward: negative distance between current and target stretch values
        float reward = 0f;
        float[] currentForces = {
            stretchSensor._currentForceRed,
            stretchSensor._currentForceGreen,
            stretchSensor._currentForceBlue,
            stretchSensor._currentForcePurple,
            stretchSensor._currentForceOrange,
            stretchSensor._currentForceCyan
        };
        for (int i = 0; i < 6; i++)
        {
            float diff = Mathf.Abs(currentForces[i] - randomPoseScript.stretchValues[i]);
            reward -= diff;
            Debug.Log($"[mlagent] Sensor {i}: current={currentForces[i]:F3}, target={randomPoseScript.stretchValues[i]:F3}, diff={diff:F3}");
        }
        SetReward(-reward);
        Debug.Log($"[mlagent] Step reward: {-reward:F3}");

        // Optionally, end episode if close enough
        if (Mathf.Abs(reward) < 0.05f)
        {
            Debug.Log("[mlagent] Episode ended: reward threshold reached.");
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Optional: for manual testing
    }
}
