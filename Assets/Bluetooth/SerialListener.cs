using UnityEngine;
using System.IO.Ports;
using System.Linq;
using System.Threading;

public class SerialListener : MonoBehaviour {
    SerialPort serialPort;

    public string latestRead;

    private string buffer;
    
    void Start() {
        serialPort = new SerialPort("COM8", 9600);
        serialPort.DtrEnable = true;
        serialPort.Open();
        if (!serialPort.IsOpen) {
            Debug.LogError("Failed to open serial port.");
            return;
        }
        Debug.Log("Serial port opened successfully.");
    }

    void Update() {
        while (serialPort.IsOpen && serialPort.BytesToRead > 0) {
            // Debug.Log("Data available on serial port.");
            int c = serialPort.ReadByte();

            if (c == '\n') {
                latestRead = buffer.Trim();
                buffer = ""; 
                // Debug.Log("Received: " + latestRead);
            } else {
                buffer += (char)c;
            }
        }
        
        
        
        
    }

    void OnApplicationQuit() {
        if (serialPort.IsOpen) serialPort.Close();
    }
}
