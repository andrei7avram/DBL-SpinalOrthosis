using TMPro;
using UnityEngine;

public class TimeShow : MonoBehaviour
{
    private TextMeshProUGUI text;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = this.GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        string currentTime = System.DateTime.Now.ToString("HH:mm:ss") + "." + System.DateTime.Now.Millisecond.ToString("D3");
        text.text = currentTime;
        
    }
}
