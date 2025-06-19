using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputListener : MonoBehaviour
{
    public TextMeshProUGUI inputText;
    public float[] solvedInput = new float[8];
    private Coroutine[] warningCoroutines = new Coroutine[8];
    private int[] criticalValues = new int[8] { 1000, 2000, 2000, 2000, 2000, 2000, 2000, 2000 };

    void Update()
    {
        // Split by backslash and get first 8 numbers between '\'
        //Debug.Log("Input Text: " + inputText.text);
        string[] parts = inputText.text.Split(new[] { '\\' }, System.StringSplitOptions.RemoveEmptyEntries);
        //Debug.Log(parts[2]);
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
