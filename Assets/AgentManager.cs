using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    [Header("Shared Pose Generator")]
    [SerializeField] private RandomPose randomPoseScript;

    [Header("Parallel Agents (6)")]
    [SerializeField] private List<mlagent> agents;

    [Header("Visualization Bones (13 Transforms)")]
    [SerializeField] private Transform[] visualizationBones = new Transform[13];

    private bool testRunning = false;

    private void Awake()
    {
        // Subscribe to each agent's success event
        foreach (var agent in agents)
        {
            agent.OnInferenceSuccess += HandleAgentSuccess;
        }
    }

    /// <summary>
    /// Call this to start one random pose and six parallel inference runs.
    /// </summary>
    public void StartParallelInference()
    {
        if (testRunning) return;
        StartCoroutine(RunParallelInference());
    }

    private IEnumerator RunParallelInference()
    {
        testRunning = true;

        // 1) Generate one random posture and store stretchValues
        yield return StartCoroutine(randomPoseScript.GenerateMedicalPostureCoroutine());

        // 2) Enable inference on all agents
        foreach (var agent in agents)
        {
            agent.inferenceMode = true;
        }

        // 3) Start test on each agent
        foreach (var agent in agents)
        {
            agent.StartTestPose();
        }
    }

    /// <summary>
    /// Called when one agent succeeds.
    /// </summary>
    private void HandleAgentSuccess(mlagent winner)
    {
        if (!testRunning) return;
        testRunning = false;

        // Stop all other agents
        foreach (var agent in agents)
        {
            if (agent != winner)
                agent.CancelInference();
        }

        // Copy winning bone angles into the 13-length visualization array
        var winningBones = winner.GetRigBones();
        int count = Mathf.Min(visualizationBones.Length, winningBones.Count);
        for (int i = 0; i < count; i++)
        {
            if (visualizationBones[i] != null && winningBones[i] != null)
            {
                visualizationBones[i].localEulerAngles = winningBones[i].localEulerAngles;
            }
        }

        // Debug: log sensor forces from the winning agent
        float[] sensorForces = new float[]
        {
            winner.stretchSensor._currentForceRed,
            winner.stretchSensor._currentForceGreen,
            winner.stretchSensor._currentForceBlue,
            winner.stretchSensor._currentForcePurple,
            winner.stretchSensor._currentForceOrange,
            winner.stretchSensor._currentForceCyan,
            winner.stretchSensor._currentForceBlack,
            winner.stretchSensor._currentForcePink
        };
        Debug.Log($"Agent '{winner.name}' succeeded. Visualization array updated.");
        Debug.Log("Sensor Forces: " + string.Join(", ", sensorForces));
    }

    private void OnDestroy()
    {
        // Unsubscribe
        foreach (var agent in agents)
        {
            agent.OnInferenceSuccess -= HandleAgentSuccess;
        }
    }
}
