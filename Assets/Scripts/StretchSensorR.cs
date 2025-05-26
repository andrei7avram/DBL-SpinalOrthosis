using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StretchSensorR : MonoBehaviour
{
    [Header("Sensor Settings")]
    public float sensitivity = 1.0f;
    public float maxOutputForce = 10f;
    public Transform rootBone; 
    public TextMeshProUGUI forceText; 

    private SkinnedMeshRenderer _skin;
    private Mesh _mesh;
    private List<int> _sensorVerticesRed;
    private List<int> _sensorVerticesGreen;
    private List<int> _sensorVerticesBlue;
    private List<int> _sensorVerticesPurple;
    private float _restLengthRed;
    private float _restLengthGreen;
    private float _restLengthBlue;
    private float _restLengthPurple;
    private float _currentForceRed;
    private float _currentForceGreen;
    private float _currentForceBlue;
    private float _currentForcePurple;

    void Start()
    {
        _skin = GetComponent<SkinnedMeshRenderer>();
        _mesh = _skin.sharedMesh;
        
        _sensorVerticesRed = new List<int>();
        _sensorVerticesGreen = new List<int>();
        _sensorVerticesBlue = new List<int>();
        _sensorVerticesPurple = new List<int>();
        Color[] colors = _mesh.colors;
        for (int i = 0; i < colors.Length; i++)
        {
            Color c = colors[i];
            if (c.r > 0.9f && c.g < 0.1f && c.b < 0.1f)
                _sensorVerticesRed.Add(i);
            if (c.g > 0.9f && c.r < 0.1f && c.b < 0.1f)
                _sensorVerticesGreen.Add(i);
            if (c.b > 0.9f && c.r < 0.1f && c.g < 0.1f)
                _sensorVerticesBlue.Add(i);
            // Purple: r > 0.4 && r < 0.9, b > 0.4 && b < 0.9, g < 0.1
            if (c.r > 0.4f && c.r < 0.9f && c.b > 0.4f && c.b < 0.9f && c.g < 0.1f)
                _sensorVerticesPurple.Add(i);
        }

        // Calculate rest lengths for all selections
        Mesh bakedMesh = new Mesh();
        _skin.BakeMesh(bakedMesh);
        Vector3[] vertices = bakedMesh.vertices;

        _restLengthRed = CalculateLength(_sensorVerticesRed, vertices);
        _restLengthGreen = CalculateLength(_sensorVerticesGreen, vertices);
        _restLengthBlue = CalculateLength(_sensorVerticesBlue, vertices);
        _restLengthPurple = CalculateLength(_sensorVerticesPurple, vertices);
    }

    float CalculateLength(List<int> indices, Vector3[] vertices)
    {
        float length = 0f;
        for (int i = 1; i < indices.Count; i++)
        {
            Vector3 p1 = _skin.transform.TransformPoint(vertices[indices[i - 1]]);
            Vector3 p2 = _skin.transform.TransformPoint(vertices[indices[i]]);
            length += Vector3.Distance(p1, p2);
        }
        return length;
    }

    void Update()
    {
        Mesh bakedMesh = new Mesh();
        _skin.BakeMesh(bakedMesh);
        Vector3[] vertices = bakedMesh.vertices;

        float currentLengthRed = CalculateLength(_sensorVerticesRed, vertices);
        float currentLengthGreen = CalculateLength(_sensorVerticesGreen, vertices);
        float currentLengthBlue = CalculateLength(_sensorVerticesBlue, vertices);
        float currentLengthPurple = CalculateLength(_sensorVerticesPurple, vertices);

        float stretchRed = Mathf.Max(0, currentLengthRed - _restLengthRed);
        float stretchGreen = Mathf.Max(0, currentLengthGreen - _restLengthGreen);
        float stretchBlue = Mathf.Max(0, currentLengthBlue - _restLengthBlue);
        float stretchPurple = Mathf.Max(0, currentLengthPurple - _restLengthPurple);

        _currentForceRed = Mathf.Clamp(stretchRed * sensitivity, 0, maxOutputForce);
        _currentForceGreen = Mathf.Clamp(stretchGreen * sensitivity, 0, maxOutputForce);
        _currentForceBlue = Mathf.Clamp(stretchBlue * sensitivity, 0, maxOutputForce);
        _currentForcePurple = Mathf.Clamp(stretchPurple * sensitivity * (45f / 25f), 0, maxOutputForce);

        if (forceText != null)
        {
            forceText.text =
                $"Red Force: {_currentForceRed:F2}\n" +
                "<color=green>Green Force: " + $"{_currentForceGreen:F2}</color>\n" +
                "<color=blue>Blue Force: " + $"{_currentForceBlue:F2}</color>\n" +
                "<color=#A020F0>Purple Force: " + $"{_currentForcePurple:F2}</color>";
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || _mesh == null || _skin == null) return;

        Mesh bakedMesh = new Mesh();
        _skin.BakeMesh(bakedMesh);
        Vector3[] vertices = bakedMesh.vertices;

        // Draw red sensor
        Gizmos.color = Color.red;
        for (int i = 1; i < (_sensorVerticesRed?.Count ?? 0); i++)
        {
            Vector3 p1 = _skin.transform.TransformPoint(vertices[_sensorVerticesRed[i - 1]]);
            Vector3 p2 = _skin.transform.TransformPoint(vertices[_sensorVerticesRed[i]]);
            Gizmos.DrawLine(p1, p2);
        }

        // Draw green sensor
        Gizmos.color = Color.green;
        for (int i = 1; i < (_sensorVerticesGreen?.Count ?? 0); i++)
        {
            Vector3 p1 = _skin.transform.TransformPoint(vertices[_sensorVerticesGreen[i - 1]]);
            Vector3 p2 = _skin.transform.TransformPoint(vertices[_sensorVerticesGreen[i]]);
            Gizmos.DrawLine(p1, p2);
        }

        // Draw blue sensor
        Gizmos.color = Color.blue;
        for (int i = 1; i < (_sensorVerticesBlue?.Count ?? 0); i++)
        {
            Vector3 p1 = _skin.transform.TransformPoint(vertices[_sensorVerticesBlue[i - 1]]);
            Vector3 p2 = _skin.transform.TransformPoint(vertices[_sensorVerticesBlue[i]]);
            Gizmos.DrawLine(p1, p2);
        }

        // Draw purple sensor
        Gizmos.color = new Color(0.627f, 0.125f, 0.941f); // #A020F0
        for (int i = 1; i < (_sensorVerticesPurple?.Count ?? 0); i++)
        {
            Vector3 p1 = _skin.transform.TransformPoint(vertices[_sensorVerticesPurple[i - 1]]);
            Vector3 p2 = _skin.transform.TransformPoint(vertices[_sensorVerticesPurple[i]]);
            Gizmos.DrawLine(p1, p2);
        }
    }
}
