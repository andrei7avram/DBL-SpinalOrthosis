using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputListener : MonoBehaviour
{
    public TextMeshProUGUI inputText;
    public float unsolvedInput = 0;
    public int[] solvedInput = new int[8];
    private Coroutine[] warningCoroutines = new Coroutine[8];
    private int[] criticalValues = new int[8] { 1000, 2000, 2000, 2000, 2000, 2000, 2000, 2000 };

    void Update()
    {
        float parsedValue;
        if (float.TryParse(inputText.text, out parsedValue))
        {
            unsolvedInput = parsedValue;
        }

        string[] parts = inputText.text.Split('\\');
        for (int i = 0; i < solvedInput.Length && i < parts.Length; i++)
        {
            int parsedInt;
            if (int.TryParse(parts[i], out parsedInt))
            {
                solvedInput[i] = parsedInt;
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
            Debug.Log($"Unsolved Input: {unsolvedInput}");
            Debug.Log("Solved Input: " + string.Join(", ", solvedInput));
        }
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
