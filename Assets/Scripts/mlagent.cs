using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class mlagent : Agent
{
    [Header("References")]
    public StretchSensorR stretchSensor;
    public RandomPose randomPoseScript;
    public SelectBones selectBonesScript;

    [Header("New Training Approach")]
    [SerializeField] private int maxStepsPerEpisode = 500;
    [SerializeField] private float stepPenalty = -0.002f;
    [SerializeField] private float successReward = 50f;
    [SerializeField] private float timeoutPenalty = -5f;
    [SerializeField] private float actionSmoothing = 0.8f;
    [SerializeField] private float improvementThreshold = 0.05f;
    [SerializeField] private float successThreshold = 0.1f;

    [Header("Curriculum Learning")]
    [SerializeField] private float initialActionScale = 5f;
    [SerializeField] private float finalActionScale = 0.5f;
    [SerializeField] private int thresholdReductionSteps = 500000;
    [SerializeField] private int scaleReductionSteps = 300000;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = false;

    private int boneCount;
    private List<Transform> rigBonesHalved;
    private List<Vector3> initialBoneEulerAngles;
    private bool needNewPose = true;
    private float[] currentForces = new float[8]; // Updated for 8 sensors
    private float[] targetForces = new float[8];  // Updated for 8 sensors
    private Vector3[] lastActions;
    private float currentActionScale;
    private int currentStepCount = 0;
    
    // New tracking variables
    private float bestErrorThisEpisode = float.MaxValue;
    private float lastTotalError = float.MaxValue;
    private int stepsWithoutImprovement = 0;

    public override void Initialize()
    {
        // Build bone hierarchy first
        if (selectBonesScript != null)
        {
            selectBonesScript.BuildBoneHierarchy();
        }
        else
        {
            Debug.LogError($"SelectBones script not assigned on {gameObject.name}!");
            return;
        }

        // Initialize rigBonesHalved: every other bone from SelectBones.boneArray
        rigBonesHalved = new List<Transform>();
        for (int i = 0; i < selectBonesScript.boneArray.Length; i += 2)
        {
            if (selectBonesScript.boneArray[i] != null)
            {
                rigBonesHalved.Add(selectBonesScript.boneArray[i]);
            }
        }

        boneCount = rigBonesHalved.Count;

        if (boneCount == 0)
        {
            Debug.LogError($"Agent {gameObject.name}: NO BONES FOUND! Check SelectBones setup.");
            return;
        }

        initialBoneEulerAngles = new List<Vector3>();
        foreach (var bone in rigBonesHalved)
        {
            initialBoneEulerAngles.Add(bone != null ? bone.localEulerAngles : Vector3.zero);
        }

        lastActions = new Vector3[boneCount];
        currentActionScale = initialActionScale;
    }

    private void UpdateCurriculumParameters()
    {
        float totalSteps = Academy.Instance.TotalStepCount;
        currentActionScale = Mathf.Lerp(initialActionScale, finalActionScale, 
            Mathf.Clamp01(totalSteps / scaleReductionSteps));
    }

    public override void OnEpisodeBegin()
    {
        currentStepCount = 0;
        UpdateCurriculumParameters();

        // Reset episode tracking
        bestErrorThisEpisode = float.MaxValue;
        lastTotalError = float.MaxValue;
        stepsWithoutImprovement = 0;

        if (needNewPose)
        {
            StartCoroutine(GenerateNewPoseCoroutine());
        }
        else
        {
            ResetToInitialPose();
            UpdateTargetForces();
            
            // Check if target forces are set
            bool allZeros = true;
            for (int i = 0; i < 6; i++)
            {
                if (targetForces[i] != 0f)
                {
                    allZeros = false;
                    break;
                }
            }
            
            if (allZeros)
            {
                Debug.LogError($"Agent {gameObject.name}: TARGET FORCES ARE ALL ZEROS!");
            }

            for (int i = 0; i < boneCount; i++)
            {
                lastActions[i] = Vector3.zero;
            }
        }
    }

    private IEnumerator GenerateNewPoseCoroutine()
    {
        randomPoseScript.GenerateRandomPoseAndStoreStretch();
        
        // Wait for the pose generation coroutine to complete
        yield return new WaitForSeconds(0.1f);
        
        needNewPose = false;
        
        ResetToInitialPose();
        UpdateTargetForces();

        // Check if target forces are set
        bool allZeros = true;
        for (int i = 0; i < 6; i++)
        {
            if (targetForces[i] != 0f)
            {
                allZeros = false;
                break;
            }
        }
        
        if (allZeros)
        {
            Debug.LogError($"Agent {gameObject.name}: TARGET FORCES ARE ALL ZEROS!");
        }

        for (int i = 0; i < boneCount; i++)
        {
            lastActions[i] = Vector3.zero;
        }
    }

    private void ResetToInitialPose()
    {
        for (int i = 0; i < boneCount; i++)
        {
            if (rigBonesHalved[i] != null)
            {
                rigBonesHalved[i].localEulerAngles = initialBoneEulerAngles[i];
            }
        }
    }

    private void UpdateTargetForces()
    {
        if (randomPoseScript.stretchValues != null && randomPoseScript.stretchValues.Length >= 8)
        {
            System.Array.Copy(randomPoseScript.stretchValues, targetForces, 8);
        }
    }

    private void UpdateCurrentForces()
    {
        currentForces[0] = stretchSensor._currentForceRed;
        currentForces[1] = stretchSensor._currentForceGreen;
        currentForces[2] = stretchSensor._currentForceBlue;
        currentForces[3] = stretchSensor._currentForcePurple;
        currentForces[4] = stretchSensor._currentForceOrange;
        currentForces[5] = stretchSensor._currentForceCyan;
        currentForces[6] = stretchSensor._currentForceBlack; // NEW
        currentForces[7] = stretchSensor._currentForcePink;  // NEW
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        UpdateCurrentForces();

        // Add current and target forces
        for (int i = 0; i < 8; i++) // Updated for 8 sensors
        {
            sensor.AddObservation(Mathf.Clamp(currentForces[i] * 10f, 0f, 10f));
            sensor.AddObservation(Mathf.Clamp(targetForces[i] * 10f, 0f, 10f));
        }

        // Add current bone rotations
        for (int i = 0; i < rigBonesHalved.Count; i++)
        {
            var bone = rigBonesHalved[i];
            if (bone != null)
            {
                Vector3 euler = bone.localEulerAngles;
                sensor.AddObservation(NormalizeAngle(euler.x));
                sensor.AddObservation(NormalizeAngle(euler.y));
                sensor.AddObservation(NormalizeAngle(euler.z));
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle / 180f;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        currentStepCount++;
        var actionArray = actions.ContinuousActions;
        int idx = 0;

        // Apply actions to bones
        for (int i = 0; i < boneCount; i++)
        {
            if (rigBonesHalved[i] == null)
            {
                idx += 3;
                continue;
            }

            Vector3 rawAction = new Vector3(
                Mathf.Clamp(actionArray[idx++], -1f, 1f),
                Mathf.Clamp(actionArray[idx++], -1f, 1f),
                Mathf.Clamp(actionArray[idx++], -1f, 1f)
            );

            Vector3 smoothedAction = Vector3.Lerp(lastActions[i], rawAction, actionSmoothing);
            lastActions[i] = smoothedAction;

            // Calculate new euler angles within ±5 degrees of initial pose
            Vector3 initialEuler = initialBoneEulerAngles[i];
            Vector3 newEuler = new Vector3(
                Mathf.Clamp(initialEuler.x + smoothedAction.x * 20f, initialEuler.x - 20f, initialEuler.x + 20f),
                Mathf.Clamp(initialEuler.y + smoothedAction.y * 3f, initialEuler.y - 3f, initialEuler.y + 3f),
                Mathf.Clamp(initialEuler.z + smoothedAction.z * 10f, initialEuler.z - 10f, initialEuler.z + 10f)
            );

            rigBonesHalved[i].localEulerAngles = newEuler;
        }

        // Calculate reward based on improvement
        float reward = CalculateReward() + stepPenalty;
        SetReward(reward);

        // Check for success or early termination
        if (IsSuccessful())
        {
            Debug.Log($"Agent {gameObject.name}: SUCCESS! Steps: {currentStepCount}, Final Error: {CalculateTotalError():F3}");
            AddReward(successReward);
            needNewPose = true; // Only change pose when threshold is reached
            EndEpisode();
        }
        else if (currentStepCount >= maxStepsPerEpisode)
        {
            AddReward(timeoutPenalty);
            // Don't change pose on timeout - keep trying same pose
            EndEpisode();
        }
        else if (stepsWithoutImprovement > 100)
        {
            AddReward(timeoutPenalty * 0.5f);
            // Don't change pose on early termination - keep trying same pose
            EndEpisode();
        }
    }

    private float CalculateReward()
    {
        float totalError = CalculateTotalError();
        
        // Initialize bestError on first step
        if (bestErrorThisEpisode == float.MaxValue)
        {
            bestErrorThisEpisode = totalError;
            return 0f; // Neutral reward for first step
        }
        
        // Track improvement
        bool improved = false;
        if (totalError < bestErrorThisEpisode)
        {
            float improvement = bestErrorThisEpisode - totalError;
            bestErrorThisEpisode = totalError;
            stepsWithoutImprovement = 0;
            improved = true;
            
            // Big reward for improvement - clamp to prevent infinity
            return Mathf.Clamp(improvement * 20f, 0f, 10f);
        }
        else
        {
            stepsWithoutImprovement++;
        }

        // Base reward for being close to target
        float baseReward = 1f / (1f + totalError);
        
        // Penalty for no improvement
        float improvementPenalty = improved ? 0f : -0.01f;
        
        float finalReward = baseReward + improvementPenalty;
        
        // Safety check - prevent NaN or infinity
        if (float.IsNaN(finalReward) || float.IsInfinity(finalReward))
        {
            return 0f;
        }
        
        return finalReward;
    }

    private float CalculateTotalError()
    {
        float totalError = 0f;
        for (int i = 0; i < 8; i++) // Updated for 8 sensors
        {
            totalError += Mathf.Abs(currentForces[i] - targetForces[i]);
        }
        return totalError;
    }

    private bool IsSuccessful()
    {
        float totalError = CalculateTotalError();
        // Success ONLY if we meet the threshold
        return totalError < successThreshold;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        for (int i = 0; i < boneCount * 3; i++)
        {
            continuousActionsOut[i] = Random.Range(-0.2f, 0.2f);
        }
    }
}