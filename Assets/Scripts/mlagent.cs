using System;
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
    [SerializeField] private float successThreshold = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = false;

    [Header("Inference Mode")]
    public bool inferenceMode = false;
    private bool waitingForTest = false;
    private bool testInProgress = false;
    private bool hasSucceeded = false;
    private bool checkingSuccess = false;

    // Event fired when agent solves in inference mode
    public event Action<mlagent> OnInferenceSuccess;

    private int boneCount;
    private List<Transform> rigBonesHalved;
    private List<Vector3> initialBoneEulerAngles;
    private float[] currentForces = new float[8];
    private float[] targetForces = new float[8];
    private Vector3[] lastActions;
    private int currentStepCount = 0;
    private float bestErrorThisEpisode = float.MaxValue;
    private int stepsWithoutImprovement = 0;

    public override void Initialize()
    {
        if (selectBonesScript == null)
        {
            Debug.LogError($"SelectBones script not assigned on {gameObject.name}!");
            return;
        }
        selectBonesScript.BuildBoneHierarchy();

        rigBonesHalved = new List<Transform>();
        for (int i = 0; i < selectBonesScript.boneArray.Length; i += 2)
        {
            if (selectBonesScript.boneArray[i] != null)
                rigBonesHalved.Add(selectBonesScript.boneArray[i]);
        }

        boneCount = rigBonesHalved.Count;
        initialBoneEulerAngles = new List<Vector3>(boneCount);
        foreach (var bone in rigBonesHalved)
            initialBoneEulerAngles.Add(bone.localEulerAngles);

        lastActions = new Vector3[boneCount];
    }

    private void ResetAgentState()
    {
        StopAllCoroutines();
        hasSucceeded = false;
        testInProgress = false;
        waitingForTest = false;
        checkingSuccess = false;
        currentStepCount = 0;
        stepsWithoutImprovement = 0;
        bestErrorThisEpisode = float.MaxValue;

        ResetToInitialPose();
        UpdateTargetForces();
        for (int i = 0; i < boneCount; i++)
            lastActions[i] = Vector3.zero;
    }

    public void StartTestPose()
    {
        ResetAgentState();
        StartCoroutine(PreparePoseThenStartAgent());
    }

    private IEnumerator PreparePoseThenStartAgent()
    {
        waitingForTest = true;

        // Shared target forces already in randomPoseScript.stretchValues
        UpdateTargetForces();

        // Reset bones
        ResetToInitialPose();

        // Brief pause for solver
        yield return new WaitForSeconds(0.1f);

        waitingForTest = false;
        testInProgress = true;
        RequestDecision();
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
            if (!testInProgress) return;
            waitingForTest = false;
            for (int i = 0; i < boneCount; i++)
                lastActions[i] = Vector3.zero;
        }
    }

    private void ResetToInitialPose()
    {
        for (int i = 0; i < boneCount; i++)
            rigBonesHalved[i].localEulerAngles = initialBoneEulerAngles[i];
    }

    private void UpdateTargetForces()
    {
        if (randomPoseScript.stretchValues != null && randomPoseScript.stretchValues.Length >= 8)
        {
            Array.Copy(randomPoseScript.stretchValues, targetForces, 8);
            if (enableDebug)
                Debug.Log("Updated Target Forces: " + string.Join(", ", targetForces));
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
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        UpdateCurrentForces();
        for (int i = 0; i < 8; i++)
        {
            sensor.AddObservation(Mathf.Clamp(currentForces[i] * 10f, 0f, 10f));
            sensor.AddObservation(Mathf.Clamp(targetForces[i] * 10f, 0f, 10f));
        }
        foreach (var bone in rigBonesHalved)
        {
            var e = bone.localEulerAngles;
            sensor.AddObservation(NormalizeAngle(e.x));
            sensor.AddObservation(NormalizeAngle(e.y));
            sensor.AddObservation(NormalizeAngle(e.z));
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (inferenceMode && (!testInProgress || waitingForTest || hasSucceeded || checkingSuccess))
            return;

        currentStepCount++;
        var arr = actions.ContinuousActions;
        int idx = 0;

        for (int i = 0; i < boneCount; i++)
        {
            Vector3 raw = new Vector3(
                Mathf.Clamp(arr[idx++], -1f, 1f),
                Mathf.Clamp(arr[idx++], -1f, 1f),
                Mathf.Clamp(arr[idx++], -1f, 1f)
            );
            var sm = Vector3.Lerp(lastActions[i], raw, actionSmoothing);
            lastActions[i] = sm;
            var init = initialBoneEulerAngles[i];
            rigBonesHalved[i].localEulerAngles = new Vector3(
                Mathf.Clamp(init.x + sm.x * 20f, init.x - 20f, init.x + 20f),
                Mathf.Clamp(init.y + sm.y * 3f,  init.y -  3f, init.y +  3f),
                Mathf.Clamp(init.z + sm.z * 10f, init.z - 10f, init.z + 10f)
            );
        }

        if (!checkingSuccess)
            StartCoroutine(WaitForSensorUpdateThenCheckSuccess());
    }

    private IEnumerator WaitForSensorUpdateThenCheckSuccess()
    {
        checkingSuccess = true;
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();

        bool stable = false;
        int checks = 3;
        float[] f1 = new float[8], f2 = new float[8];
        for (int c = 0; c < checks && !stable; c++)
        {
            UpdateCurrentForces(); Array.Copy(currentForces, f1, 8);
            yield return new WaitForFixedUpdate();
            UpdateCurrentForces(); Array.Copy(currentForces, f2, 8);
            stable = true;
            for (int i = 0; i < 8; i++)
                if (Mathf.Abs(f1[i] - f2[i]) > 0.01f) { stable = false; break; }
            if (!stable && c < checks - 1)
                yield return new WaitForFixedUpdate();
        }

        UpdateCurrentForces();
        if (enableDebug)
            Debug.Log($"Sensor stable. Final: {string.Join(", ", currentForces)}");

        SetReward(CalculateReward() + stepPenalty);

        if (IsSuccessful())
        {
            hasSucceeded = true;
            if (inferenceMode)
            {
                testInProgress = false;
                OnInferenceSuccess?.Invoke(this);
                yield break;
            }
            EndEpisode();
        }
        else if (currentStepCount >= maxStepsPerEpisode)
        {
            AddReward(timeoutPenalty);
            testInProgress = false;
            EndEpisode();
        }

        checkingSuccess = false;
    }

    private float CalculateReward()
    {
        float err = 0f;
        for (int i = 0; i < 8; i++) err += Mathf.Abs(currentForces[i] - targetForces[i]);

        if (bestErrorThisEpisode == float.MaxValue)
        {
            bestErrorThisEpisode = err;
            return 0f;
        }
        if (err < bestErrorThisEpisode)
        {
            float imp = bestErrorThisEpisode - err;
            bestErrorThisEpisode = err;
            stepsWithoutImprovement = 0;
            return Mathf.Clamp(imp * 20f, 0f, 10f);
        }
        stepsWithoutImprovement++;
        return 1f/(1f+err) - 0.01f;
    }

    private bool IsSuccessful()
    {
        float err = 0f; for (int i = 0; i < 8; i++) err += Mathf.Abs(currentForces[i] - targetForces[i]);
        return err < successThreshold;
    }

    private float NormalizeAngle(float a)
    {
        if (a > 180f) a -= 360f;
        return a / 180f;
    }

    // Methods for manager
    public void CancelInference()
    {
        testInProgress = false;
        StopAllCoroutines();
    }

    public List<Transform> GetRigBones()
    {
        return rigBonesHalved;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        for (int i = 0; i < boneCount * 3; i++)
            cont[i] = UnityEngine.Random.Range(-0.2f, 0.2f);
    }

    void OnDestroy()
    {
        if (OnInferenceSuccess != null)
        {
            foreach (Delegate d in OnInferenceSuccess.GetInvocationList())
                OnInferenceSuccess -= (Action<mlagent>)d;
        }
    }
}
