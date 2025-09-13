using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.MLAgents;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InputListener : MonoBehaviour
{
    public TextMeshProUGUI inputText;
    public float[] solvedInput = new float[8];
    private Coroutine[] warningCoroutines = new Coroutine[8];
    private int[] criticalValues = new int[8] { 1000, 2000, 2000, 2000, 2000, 2000, 2000, 2000 };

    private Coroutine part5Coroutine;

    public RandomPose randomPoseScript;

    public AgentManager agentManager;

    public GameObject objectToActivate;

    public bool isReady = false;

    void Update()
    {
        // Split by backslash and get first 8 numbers between '\'
        string[] parts = inputText.text.Split(new[] { '\\' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < solvedInput.Length; i++)
        {
            if (i < parts.Length && float.TryParse(parts[i], out float parsedInt))
            {
                solvedInput[i] = parsedInt;
            }
            else
            {
                solvedInput[i] = 0;
            }
        }

        // Start or stop coroutine for part[5] > 45 for 3 seconds
        if (parts.Length > 0 && float.TryParse(parts[0], out float part0Value) && part0Value < 0.5f)
        {
            if (part5Coroutine == null)
                part5Coroutine = StartCoroutine(Part5Above45Coroutine());
        }
        else
        {
            if (part5Coroutine != null)
            {
                StopCoroutine(part5Coroutine);
                part5Coroutine = null;
            }
        }

        for (int i = 0; i < solvedInput.Length; i++)
        {
            if (solvedInput[i] <= criticalValues[i])
            {
                if (warningCoroutines[i] == null)
                    warningCoroutines[i] = StartCoroutine(CriticalWarning(i));
            }
            else
            {
                if (warningCoroutines[i] != null)
                {
                    StopCoroutine(warningCoroutines[i]);
                    warningCoroutines[i] = null;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Solved Input: " + string.Join(", ", solvedInput));
        }
    }

    private IEnumerator Part5Above45Coroutine()
    {
        float timer = 0f;
        while (true)
        {
            string[] parts = inputText.text.Split(new[] { '\\' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && float.TryParse(parts[0], out float part5Value) && part5Value < 0.6f)
            {
                timer += Time.deltaTime;
                if (timer >= 3f && isReady == false)
                {
                    OnPart5Above45For3Seconds();
                    isReady = true;
                    yield break;
                }
            }
            else
            {
                yield break;
            }
            yield return null;
        }
    }

    // Call your custom function here
    private void OnPart5Above45For3Seconds()
    {
        Debug.Log("part[5] > 45 for 3 seconds! Call your function here.");
        //randomPoseScript.SetisLordosisTrue();
        //agentManager.StartParallelInference();
        agentManager.SetVisualizationToFirstBadPosture();

        objectToActivate.SetActive(false);
        Debug.Log("Starting parallel inference after scoliosis detected.");
        
    }

    public IEnumerator CriticalWarning(int index)
    {
        float timer = 0f;
        while (solvedInput[index] <= criticalValues[index])
        {
            timer += Time.deltaTime;
            if (timer >= 3f)
            {
                Debug.LogWarning($"Warning: Input at index {index} has been at or below critical value ({criticalValues[index]}) for 3 seconds!");
                yield break;
            }
            yield return null;
        }
    }
}
