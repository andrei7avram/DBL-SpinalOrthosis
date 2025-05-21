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
    public Transform rootBone; // Assign your character's root/hip bone
    public TextMeshProUGUI forceText; // Assign your UI Text element

    private SkinnedMeshRenderer _skin;
    private Mesh _mesh;
    private List<int> _sensorVertices;
    private float _restLength;
    private Vector3[] _restLocalPositions;
    private float _currentForce;

    void Start()
    {
        _skin = GetComponent<SkinnedMeshRenderer>();
        _mesh = _skin.sharedMesh;
        
        // Find sensor vertices (r>0.9, g/b<0.1)
        _sensorVertices = new List<int>();
        Color[] colors = _mesh.colors;
        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i].r > 0.9f && colors[i].g < 0.1f && colors[i].b < 0.1f)
                _sensorVertices.Add(i);
        }

        // Use baked mesh for rest length calculation
        Mesh bakedMesh = new Mesh();
        _skin.BakeMesh(bakedMesh);
        Vector3[] vertices = bakedMesh.vertices;

        float restLength = 0f;
        for (int i = 1; i < _sensorVertices.Count; i++)
        {
            Vector3 p1 = _skin.transform.TransformPoint(vertices[_sensorVertices[i - 1]]);
            Vector3 p2 = _skin.transform.TransformPoint(vertices[_sensorVertices[i]]);
            restLength += Vector3.Distance(p1, p2);
        }
        _restLength = restLength;

        Debug.Log($"Stretch sensor initialized with {_sensorVertices.Count} vertices. Rest length: {_restLength}");
    }

    float CalculateCurrentLength()
    {
        float length = 0f;
        Vector3[] vertices = _mesh.vertices;
        
        for (int i = 1; i < _sensorVertices.Count; i++)
        {
            Vector3 currentPos1 = rootBone.InverseTransformPoint(
                _skin.transform.TransformPoint(vertices[_sensorVertices[i-1]])
            );
            Vector3 currentPos2 = rootBone.InverseTransformPoint(
                _skin.transform.TransformPoint(vertices[_sensorVertices[i]])
            );
            
            length += Vector3.Distance(currentPos1, currentPos2);
        }
        
        return length;
    }

    void Update()
    {
        Mesh bakedMesh = new Mesh();
        _skin.BakeMesh(bakedMesh);

        float currentLength = 0f;
        Vector3[] vertices = bakedMesh.vertices;
        for (int i = 1; i < _sensorVertices.Count; i++)
        {
            Vector3 currentPos1 = _skin.transform.TransformPoint(vertices[_sensorVertices[i - 1]]);
            Vector3 currentPos2 = _skin.transform.TransformPoint(vertices[_sensorVertices[i]]);
            currentLength += Vector3.Distance(currentPos1, currentPos2);
        }

        float stretch = Mathf.Max(0, currentLength - _restLength);
        _currentForce = Mathf.Clamp(stretch * sensitivity, 0, maxOutputForce);

        // Debug output
        Debug.Log($"Current length: {currentLength}, Rest length: {_restLength}, Stretch: {stretch}, Force: {_currentForce}");

        if (forceText != null)
        {
            forceText.text = $"Force: {_currentForce.ToString("F2")}";
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || _sensorVertices == null || _mesh == null) return;
        
        Vector3[] vertices = _mesh.vertices;
        Gizmos.color = Color.red;
        
        for (int i = 1; i < _sensorVertices.Count; i++)
        {
            Vector3 p1 = _skin.transform.TransformPoint(vertices[_sensorVertices[i-1]]);
            Vector3 p2 = _skin.transform.TransformPoint(vertices[_sensorVertices[i]]);
            Gizmos.DrawLine(p1, p2);
        }
    }

    // Call this if your character changes rest pose
    public void Recalibrate()
    {
        Vector3[] vertices = _mesh.vertices;
        for (int i = 0; i < _sensorVertices.Count; i++)
        {
            _restLocalPositions[i] = _skin.transform.TransformPoint(vertices[_sensorVertices[i]]);
        }
        _restLength = 0f;
        for (int i = 1; i < _sensorVertices.Count; i++)
        {
            _restLength += Vector3.Distance(_restLocalPositions[i - 1], _restLocalPositions[i]);
        }
    }
}
