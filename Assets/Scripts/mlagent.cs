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
    public int solvedPoses = 0; // Track how many poses have been solved

    [Header("Training Settings")]
    [SerializeField] private int maxStepsPerEpisode = 500;
    [SerializeField] private float stepPenalty = -0.002f;
    [SerializeField] private float successReward = 50f;
    [SerializeField] private float timeoutPenalty = -5f;
    [SerializeField] private float actionSmoothing = 0.8f;
    [SerializeField] private float successThreshold = 0.1f;

    [Header("Curriculum Learning")]
    [SerializeField] private float initialActionScale = 5f;
    [SerializeField] private float finalActionScale = 0.5f;
    [SerializeField] private int scaleReductionSteps = 300000;

    [Header("Pose Change Settings")]
    [SerializeField] private int episodesPerPose = 50;  // Change pose every N episodes

    private int boneCount;
    private List<Transform> rigBonesHalved;
    private List<Vector3> initialBoneEulerAngles;
    private bool needNewPose = true;
    private float[] currentForces = new float[6];
    private float[] targetForces = new float[6];
    private Vector3[] lastActions;
    private float currentActionScale;
    private int currentStepCount = 0;

    private float bestErrorThisEpisode = float.MaxValue;
    private int stepsWithoutImprovement = 0;
    private int episodesSinceLastPoseChange = 0;

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

        bestErrorThisEpisode = float.MaxValue;
        stepsWithoutImprovement = 0;

        // Pose change control: only change pose every episodesPerPose episodes
        if (needNewPose || episodesSinceLastPoseChange >= episodesPerPose)
        {
            episodesSinceLastPoseChange = 0;
            needNewPose = true;
        }
        else
        {
            needNewPose = false;
        }
        episodesSinceLastPoseChange++;

        if (needNewPose)
        {
            StartCoroutine(GenerateNewPoseCoroutine());
        }
        else
        {
            ResetToInitialPose();
            UpdateTargetForcesSafe();

            for (int i = 0; i < boneCount; i++)
            {
                lastActions[i] = Vector3.zero;
            }
        }
    }

    private IEnumerator GenerateNewPoseCoroutine()
    {
        randomPoseScript.GenerateRandomPoseAndStoreStretch();

        // Wait for mesh update & sensor refresh (adjust wait time if needed)
        yield return new WaitForEndOfFrame();

        needNewPose = false;

        ResetToInitialPose();
        UpdateTargetForcesSafe();

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

    private void UpdateTargetForcesSafe()
    {
        if (randomPoseScript.stretchValues != null && randomPoseScript.stretchValues.Length >= 6)
        {
            for (int i = 0; i < 6; i++)
            {
                targetForces[i] = randomPoseScript.stretchValues[i];
            }
        }
        else
        {
            // Fill with zeros if invalid
            for (int i = 0; i < 6; i++) targetForces[i] = 0f;
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
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        UpdateCurrentForces();

        // Add current and target forces
        for (int i = 0; i < 6; i++)
        {
            sensor.AddObservation(Mathf.Clamp(currentForces[i] * 10f, 0f, 10f));
            sensor.AddObservation(Mathf.Clamp(targetForces[i] * 10f, 0f, 10f));
        }

        // Add current bone rotations normalized [-1,1]
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
                Mathf.Clamp(initialEuler.x + smoothedAction.x * currentActionScale, initialEuler.x - 5f, initialEuler.x + 5f),
                Mathf.Clamp(initialEuler.y + smoothedAction.y * currentActionScale, initialEuler.y - 5f, initialEuler.y + 5f),
                Mathf.Clamp(initialEuler.z + smoothedAction.z * currentActionScale, initialEuler.z - 5f, initialEuler.z + 5f)
            );

            rigBonesHalved[i].localEulerAngles = newEuler;
        }

        float reward = CalculateReward() + stepPenalty;
        SetReward(reward);

        if (IsSuccessful())
        {
            AddReward(successReward);
            solvedPoses++;
            if (!Application.isBatchMode) // Avoid log spam during headless runs
            {
                Debug.Log($"Agent {gameObject.name}: SUCCESS! Steps: {currentStepCount}, Reward: {GetCumulativeReward():F2}");
                Debug.Log($"Target Forces: [{string.Join(", ", targetForces)}]");
                Debug.Log($"Current Forces: [{string.Join(", ", currentForces)}]");
            }
            needNewPose = true;
            EndEpisode();
        }
        else if (currentStepCount >= maxStepsPerEpisode)
        {
            AddReward(timeoutPenalty);
            EndEpisode();
        }
        else if (stepsWithoutImprovement > 100)
        {
            AddReward(timeoutPenalty * 0.5f);
            EndEpisode();
            
        }
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
            bestErrorThisEpisode = totalError;
            stepsWithoutImprovement = 0;
            improved = true;
        }
        else
        {
            stepsWithoutImprovement++;
        }

        return -totalError;
    }

    private float CalculateTotalError()
    {
        float total = 0f;
        for (int i = 0; i < 6; i++)
        {
            float diff = currentForces[i] - targetForces[i];
            total += Mathf.Abs(diff);
        }
        return total;
    }

    private bool IsSuccessful()
{
    float totalError = 0f;
    for (int i = 0; i < 6; i++)
    {
        totalError += Mathf.Abs(currentForces[i] - targetForces[i]);
    }
    return totalError < successThreshold;
}

}
