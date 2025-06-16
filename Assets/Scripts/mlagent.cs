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

    [Header("Training Settings")]
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

    [Header("Inference Mode")]
    public bool inferenceMode = false;
    private bool waitingForTest = false;
    private bool testInProgress = false;
    private bool hasSucceeded = false;
    private bool checkingSuccess = false; // New flag to prevent multiple coroutines

    private int boneCount;
    private List<Transform> rigBonesHalved;
    private List<Vector3> initialBoneEulerAngles;
    private bool needNewPose = true;
    private float[] currentForces = new float[8];
    private float[] targetForces = new float[8];
    private Vector3[] lastActions;
    private float currentActionScale;
    private int currentStepCount = 0;
    
    private float bestErrorThisEpisode = float.MaxValue;
    private float lastTotalError = float.MaxValue;
    private int stepsWithoutImprovement = 0;

    public override void Initialize()
    {
        if (selectBonesScript != null)
        {
            selectBonesScript.BuildBoneHierarchy();
        }
        else
        {
            Debug.LogError($"SelectBones script not assigned on {gameObject.name}!");
            return;
        }

        rigBonesHalved = new List<Transform>();
        for (int i = 0; i < selectBonesScript.boneArray.Length; i += 2)
        {
            if (selectBonesScript.boneArray[i] != null)
            {
                rigBonesHalved.Add(selectBonesScript.boneArray[i]);
            }
        }

        boneCount = rigBonesHalved.Count;
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

    public void StartTestPose()
    {
        // Reset all test state flags before starting new test
        hasSucceeded = false;
        testInProgress = false;
        waitingForTest = false;
        checkingSuccess = false;
        
        Debug.Log("Starting new test pose...");
        StartCoroutine(PreparePoseThenStartAgent());
    }
    
    private IEnumerator PreparePoseThenStartAgent()
    {
        waitingForTest = true;
        
        // 1. Generate the pose visually and store stretch data
        // Wait for the RandomPose coroutine to complete
        yield return StartCoroutine(randomPoseScript.GenerateMedicalPostureCoroutine());
        
        // 2. Assign the target forces AFTER the pose generation is complete
        UpdateTargetForces();

        // 4. Reset agent's bones to initial pose
        ResetToInitialPose();

        // 5. Wait briefly so the model can visually catch up
        yield return new WaitForSeconds(1f);
        
        //debug log chosen sensor targets
        Debug.Log("Target Forces Before: " + string.Join(", ", targetForces));

        // 6. Start the test
        waitingForTest = false;
        testInProgress = true;
        
        RequestDecision(); // 🔥 Start the agent's inference step
    }

    public override void OnEpisodeBegin()
    {
        bestErrorThisEpisode = float.MaxValue;
        stepsWithoutImprovement = 0;
        currentStepCount = 0;
        hasSucceeded = false;
        checkingSuccess = false;

        if (inferenceMode)
        {
            if (!testInProgress)
            {
                return; // Do nothing unless test is in progress
            }

            // In inference mode, we DON'T generate a new pose here
            // The pose and target forces are already set in PreparePoseThenStartAgent()
            if (waitingForTest)
            {
                waitingForTest = false;
                // Don't generate new pose or update targets here - already done in coroutine
            }

            // Reset action smoothing
            for (int i = 0; i < boneCount; i++)
            {
                lastActions[i] = Vector3.zero;
            }
            return;
        }

        // Training mode - generate new pose
        if (needNewPose)
        {
            randomPoseScript.GenerateRandomPoseAndStoreStretch();
            needNewPose = false;
        }

        ResetToInitialPose();
        UpdateTargetForces();

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
            
            // Debug log to verify target forces are updated correctly
            if (enableDebug)
            {
                Debug.Log("Updated Target Forces: " + string.Join(", ", targetForces));
            }
        }
        else
        {
            Debug.LogWarning("StretchValues not ready or insufficient length!");
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
        currentForces[6] = stretchSensor._currentForceBlack;
        currentForces[7] = stretchSensor._currentForcePink;
        
        if (enableDebug)
        {
            //Debug.Log($"Force update at frame {Time.frameCount}: {string.Join(", ", currentForces)}");
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        UpdateCurrentForces();

        for (int i = 0; i < 8; i++)
        {
            sensor.AddObservation(Mathf.Clamp(currentForces[i] * 10f, 0f, 10f));
            sensor.AddObservation(Mathf.Clamp(targetForces[i] * 10f, 0f, 10f));
        }

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
        // Don't process actions if already succeeded, not in active test, or already checking success
        if (inferenceMode && (!testInProgress || waitingForTest || hasSucceeded || checkingSuccess))
        {
            return;
        }

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

            Vector3 initialEuler = initialBoneEulerAngles[i];
            Vector3 newEuler = new Vector3(
                Mathf.Clamp(initialEuler.x + smoothedAction.x * 20f, initialEuler.x - 20f, initialEuler.x + 20f),
                Mathf.Clamp(initialEuler.y + smoothedAction.y * 3f, initialEuler.y - 3f, initialEuler.y + 3f),
                Mathf.Clamp(initialEuler.z + smoothedAction.z * 10f, initialEuler.z - 10f, initialEuler.z + 10f)
            );

            rigBonesHalved[i].localEulerAngles = newEuler;
        }

        // Start delayed success check to let StretchSensor react to bone changes
        if (!checkingSuccess)
        {
            StartCoroutine(WaitForSensorUpdateThenCheckSuccess());
        }
    }

    private IEnumerator WaitForSensorUpdateThenCheckSuccess()
    {
        checkingSuccess = true;
    
    // Wait for initial sensor processing
    yield return new WaitForEndOfFrame();
    yield return new WaitForFixedUpdate();
    
    // Check for sensor stability by comparing readings over multiple frames
    float[] previousForces = new float[8];
    float[] currentForces1 = new float[8];
    float[] currentForces2 = new float[8];
    
    int stabilityChecks = 3;
    bool sensorStable = false;
    
    for (int check = 0; check < stabilityChecks && !sensorStable; check++)
    {
        // First reading
        UpdateCurrentForces();
        System.Array.Copy(currentForces, currentForces1, 8);
        
        yield return new WaitForFixedUpdate();
        
        // Second reading
        UpdateCurrentForces();
        System.Array.Copy(currentForces, currentForces2, 8);
        
        // Check if readings are stable (difference < threshold)
        sensorStable = true;
        for (int i = 0; i < 8; i++)
        {
            if (Mathf.Abs(currentForces1[i] - currentForces2[i]) > 0.01f) // Stability threshold
            {
                sensorStable = false;
                break;
            }
        }
        
        if (!sensorStable && check < stabilityChecks - 1)
        {
            yield return new WaitForFixedUpdate();
        }
    }
    
    // Final force update after stability check
    UpdateCurrentForces();
    
    if (enableDebug)
    {
        Debug.Log($"Sensor stable after {stabilityChecks} checks. Final forces: {string.Join(", ", currentForces)}");
    }
    
    // Calculate reward with stabilized forces
    float reward = CalculateReward() + stepPenalty;
    SetReward(reward);

    // Check success with the properly stabilized sensor values
    if (IsSuccessful())
    {   
        hasSucceeded = true;
        
        if (inferenceMode)
        {
            testInProgress = false;
            Debug.Log("Test completed successfully - ready for new test");
        }
        else
        {
            needNewPose = true;
            EndEpisode();
        }
        Debug.Log("Success!");
        Debug.Log("Target Forces: " + string.Join(", ", targetForces));
        Debug.Log("Current Forces: " + string.Join(", ", currentForces));
        
        AddReward(successReward);
    }
    else if (currentStepCount >= maxStepsPerEpisode)
    {
        AddReward(timeoutPenalty);
        if (inferenceMode)
        {
            testInProgress = false;
            Debug.Log("Test timed out - ready for new test");
        }
        else
        {
            EndEpisode();
        }
    }
    else if (stepsWithoutImprovement > 100 && !inferenceMode)
    {
        AddReward(timeoutPenalty * 0.5f);
        EndEpisode();
    }
    
    checkingSuccess = false;
    }

    private float CalculateReward()
    {
        float totalError = CalculateTotalError();
        
        if (bestErrorThisEpisode == float.MaxValue)
        {
            bestErrorThisEpisode = totalError;
            return 0f;
        }
        
        bool improved = false;
        if (totalError < bestErrorThisEpisode)
        {
            float improvement = bestErrorThisEpisode - totalError;
            bestErrorThisEpisode = totalError;
            stepsWithoutImprovement = 0;
            improved = true;
            return Mathf.Clamp(improvement * 20f, 0f, 10f);
        }
        else
        {
            stepsWithoutImprovement++;
        }

        float baseReward = 1f / (1f + totalError);
        float improvementPenalty = improved ? 0f : -0.01f;
        float finalReward = baseReward + improvementPenalty;
        
        if (float.IsNaN(finalReward)) return 0f;
        return finalReward;
    }

    private float CalculateTotalError()
    {
        float totalError = 0f;
        for (int i = 0; i < 8; i++)
        {
            totalError += Mathf.Abs(currentForces[i] - targetForces[i]);
        }
        return totalError;
    }

    private bool IsSuccessful()
    {
        float totalError = CalculateTotalError();
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