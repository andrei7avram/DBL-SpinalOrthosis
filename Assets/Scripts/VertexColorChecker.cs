using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexColorChecker : MonoBehaviour
{
  void Start()
    {
        SkinnedMeshRenderer skin = GetComponent<SkinnedMeshRenderer>();
        if (skin != null && skin.sharedMesh != null)
        {
            Color[] colors = skin.sharedMesh.colors;
            Debug.Log($"Mesh has {colors.Length} vertex colors");       
            
            // Count red vertices (assuming you painted them red)
            int redCount = 0;
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i].r > 0.9f && colors[i].b < 0.1f && colors[i].g < 0.1f) redCount++;
            }
            Debug.Log($"Found {redCount} red vertices");
        }
    }
}
