using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class StretchSensorR : MonoBehaviour
{
    [Header("Sensor Settings")]
    public float sensitivity = 1.0f;
    public float maxOutputForce = 10f;
    public Transform rootBone;
    public TextMeshProUGUI forceText;

    private SkinnedMeshRenderer _skin;
    private Mesh _mesh;
    private Mesh _bakedMesh;
    private List<int> _sensorVerticesRed;
    private List<int> _sensorVerticesGreen;
    private List<int> _sensorVerticesBlue;
    private List<int> _sensorVerticesPurple;
    private List<int> _sensorVerticesOrange;
    private List<int> _sensorVerticesCyan;
    private List<int> _sensorVerticesBlack;
    private List<int> _sensorVerticesPink;
    private float _restLengthRed;
    private float _restLengthGreen;
    private float _restLengthBlue;
    private float _restLengthPurple;
    private float _restLengthOrange;
    private float _restLengthCyan;
    private float _restLengthBlack;
    private float _restLengthPink;
    public float _currentForceRed;
    public float _currentForceGreen;
    public float _currentForceBlue;
    public float _currentForcePurple;
    public float _currentForceOrange;
    public float _currentForceCyan;
    public float _currentForceBlack;
    public float _currentForcePink;

    void Start()
    {
        _skin = GetComponent<SkinnedMeshRenderer>();
        _mesh = _skin.sharedMesh;
        _bakedMesh = new Mesh();

        _sensorVerticesRed = new List<int>();
        _sensorVerticesGreen = new List<int>();
        _sensorVerticesBlue = new List<int>();
        _sensorVerticesPurple = new List<int>();
        _sensorVerticesOrange = new List<int>();
        _sensorVerticesCyan = new List<int>();
        _sensorVerticesBlack = new List<int>();   // Black selection
        _sensorVerticesPink = new List<int>();    // Pink selection

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

            if (c.r > 0.4f && c.r < 0.9f && c.b > 0.4f && c.b < 0.9f && c.g < 0.1f)
                _sensorVerticesPurple.Add(i);

            if (c.r > 0.4f && c.r < 0.9f && c.b < 0.1f && c.g > 0.4f && c.g < 0.9f)
                _sensorVerticesOrange.Add(i);
            if (c.r < 0.1f && c.g > 0.4f && c.g < 0.9f && c.b > 0.4f && c.b < 0.9f)
                _sensorVerticesCyan.Add(i);

            // Black: r < 0.1, g < 0.1, b < 0.1
            if (c.r < 0.1f && c.g < 0.1f && c.b < 0.1f)
                _sensorVerticesBlack.Add(i);

            // Pink: r > 0.9, b > 0.9, g < 0.1
            if (c.r > 0.9f && c.b > 0.9f && c.g < 0.1f)
                _sensorVerticesPink.Add(i);
        }

        // ORDER BLUE VERTICES 
        _sensorVerticesBlue.Sort((a, b) =>
        {
            Vector3 va = _mesh.vertices[a];
            Vector3 vb = _mesh.vertices[b];
            int cmp = va.y.CompareTo(vb.y);
            if (cmp != 0) return cmp;
            cmp = va.x.CompareTo(vb.x);
            if (cmp != 0) return cmp;
            return va.z.CompareTo(vb.z);
        });

        // ORDER PURPLE VERTICES AS MIRROR OF BLUE 
        List<int> orderedPurple = new List<int>();
        foreach (int blueIdx in _sensorVerticesBlue)
        {
            Vector3 bluePos = _mesh.vertices[blueIdx];
            Vector3 mirrored = new Vector3(-bluePos.x, bluePos.y, bluePos.z);

            float minDist = float.MaxValue;
            int bestPurple = -1;
            foreach (int purpleIdx in _sensorVerticesPurple)
            {
                Vector3 purplePos = _mesh.vertices[purpleIdx];
                float dist = Vector3.Distance(mirrored, purplePos);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestPurple = purpleIdx;
                }
            }
            if (bestPurple != -1)
                orderedPurple.Add(bestPurple);
        }
        _sensorVerticesPurple = orderedPurple;

        // ORDER BLACK VERTICES (like blue: by y, then x, then z)
        _sensorVerticesBlack.Sort((a, b) =>
        {
            Vector3 va = _mesh.vertices[a];
            Vector3 vb = _mesh.vertices[b];
            int cmp = va.y.CompareTo(vb.y);
            if (cmp != 0) return cmp;
            cmp = va.x.CompareTo(vb.x);
            if (cmp != 0) return cmp;
            return va.z.CompareTo(vb.z);
        });

        // ORDER PINK VERTICES AS MIRROR OF BLACK
        List<int> orderedPink = new List<int>();
        foreach (int blackIdx in _sensorVerticesBlack)
        {
            Vector3 blackPos = _mesh.vertices[blackIdx];
            Vector3 mirrored = new Vector3(-blackPos.x, blackPos.y, blackPos.z);

            float minDist = float.MaxValue;
            int bestPink = -1;
            foreach (int pinkIdx in _sensorVerticesPink)
            {
                Vector3 pinkPos = _mesh.vertices[pinkIdx];
                float dist = Vector3.Distance(mirrored, pinkPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestPink = pinkIdx;
                }
            }
            if (bestPink != -1)
                orderedPink.Add(bestPink);
        }
        _sensorVerticesPink = orderedPink;

        // --- CALCULATE REST LENGTHS ---
        Mesh bakedMesh = new Mesh();
        _skin.BakeMesh(bakedMesh);
        Vector3[] vertices = bakedMesh.vertices;

        _restLengthRed = CalculateLength(_sensorVerticesRed, vertices);
        _restLengthGreen = CalculateLength(_sensorVerticesGreen, vertices);
        _restLengthBlue = CalculateLength(_sensorVerticesBlue, vertices);
        _restLengthPurple = CalculateLength(_sensorVerticesPurple, vertices);
        _restLengthOrange = CalculateLength(_sensorVerticesOrange, vertices);
        _restLengthCyan = CalculateLength(_sensorVerticesCyan, vertices);
        _restLengthBlack = CalculateLength(_sensorVerticesBlack, vertices);
        _restLengthPink = CalculateLength(_sensorVerticesPink, vertices);
        
        // print number of pink and black vertices
        Debug.Log($"Black vertices: {_sensorVerticesBlack.Count}, Pink vertices: {_sensorVerticesPink.Count}");
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
        _bakedMesh.Clear(); 
        _skin.BakeMesh(_bakedMesh);
        Vector3[] vertices = _bakedMesh.vertices;

        float currentLengthRed = CalculateLength(_sensorVerticesRed, vertices);
        float currentLengthGreen = CalculateLength(_sensorVerticesGreen, vertices);
        float currentLengthBlue = CalculateLength(_sensorVerticesBlue, vertices);
        float currentLengthPurple = CalculateLength(_sensorVerticesPurple, vertices);
        float currentLengthOrange = CalculateLength(_sensorVerticesOrange, vertices);
        float currentLengthCyan = CalculateLength(_sensorVerticesCyan, vertices);
        float currentLengthBlack = CalculateLength(_sensorVerticesBlack, vertices);
        float currentLengthPink = CalculateLength(_sensorVerticesPink, vertices);

        float stretchRed = Mathf.Max(0, currentLengthRed - _restLengthRed);
        float stretchGreen = Mathf.Max(0, currentLengthGreen - _restLengthGreen);
        float stretchBlue = Mathf.Max(0, currentLengthBlue - _restLengthBlue);
        float stretchPurple = Mathf.Max(0, currentLengthPurple - _restLengthPurple);
        float stretchOrange = Mathf.Max(0, currentLengthOrange - _restLengthOrange);
        float stretchCyan = Mathf.Max(0, currentLengthCyan - _restLengthCyan);
        float stretchBlack = Mathf.Max(0, currentLengthBlack - _restLengthBlack);
        float stretchPink = Mathf.Max(0, currentLengthPink - _restLengthPink);

        _currentForceRed = Mathf.Clamp(stretchRed * sensitivity*10, 0, maxOutputForce);
        _currentForceGreen = Mathf.Clamp(stretchGreen * sensitivity*10, 0, maxOutputForce);
        _currentForceBlue = Mathf.Clamp(stretchBlue * sensitivity*10, 0, maxOutputForce);
        _currentForcePurple = Mathf.Clamp(stretchPurple * sensitivity*10, 0, maxOutputForce);
        _currentForceOrange = Mathf.Clamp(stretchOrange * sensitivity*10, 0, maxOutputForce);
        _currentForceCyan = Mathf.Clamp(stretchCyan * sensitivity*10, 0, maxOutputForce);
        _currentForceBlack = Mathf.Clamp(stretchBlack * sensitivity * 10, 0, maxOutputForce);
        _currentForcePink = Mathf.Clamp(stretchPink * sensitivity * 10, 0, maxOutputForce);

        if (forceText != null)
        {
            forceText.text =
                $"Red Force: {_currentForceRed:F2}\n" +
                "<color=green>Green Force: " + $"{_currentForceGreen:F2}</color>\n" +
                "<color=blue>Blue Force: " + $"{_currentForceBlue:F2}</color>\n" +
                "<color=#A020F0>Purple Force: " + $"{_currentForcePurple:F2}</color>\n" +
                "<color=#FFA500>Orange Force: " + $"{_currentForceOrange:F2}</color>\n" +
                "<color=#00FFFF>Cyan Force: " + $"{_currentForceCyan:F2}</color>\n" +
                "<color=#222222>Black Force: " + $"{_currentForceBlack:F2}</color>\n" +
                "<color=#FF00FF>Pink Force: " + $"{_currentForcePink:F2}</color>";
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

        // Draw orange sensor
        Gizmos.color = new Color(1f, 0.647f, 0f); // #FFA500
        for (int i = 1; i < (_sensorVerticesOrange?.Count ?? 0); i++)
        {
            Vector3 p1 = _skin.transform.TransformPoint(vertices[_sensorVerticesOrange[i - 1]]);
            Vector3 p2 = _skin.transform.TransformPoint(vertices[_sensorVerticesOrange[i]]);
            Gizmos.DrawLine(p1, p2);
        }

        // Draw cyan sensor
        Gizmos.color = Color.cyan;
        for (int i = 1; i < (_sensorVerticesCyan?.Count ?? 0); i++)
        {
            Vector3 p1 = _skin.transform.TransformPoint(vertices[_sensorVerticesCyan[i - 1]]);
            Vector3 p2 = _skin.transform.TransformPoint(vertices[_sensorVerticesCyan[i]]);
            Gizmos.DrawLine(p1, p2);
        }
    }
}