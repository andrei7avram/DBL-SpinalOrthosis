using TMPro;
using UnityEngine;

public class SensorShow : MonoBehaviour
{
    
    public SerialListener serialListener;
    private TextMeshProUGUI text;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = this.GetComponent<TextMeshProUGUI>();

    }

    // Update is called once per frame
    void Update()
    {
        text.text = serialListener.latestRead;
    }
}
