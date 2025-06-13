using System.Collections.Generic;
using UnityEngine;

public class SelectBones : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform spineRoot; // Assign spine34 directly here
    [SerializeField] private Transform rootObject; // Assign your root object here
    
    [Header("Generated Bone Array")]
    [SerializeField] public Transform[] boneArray = new Transform[26];
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    void Start()
    {
        if (spineRoot != null && rootObject != null)
        {
            BuildBoneHierarchy();
        }
        else
        {
            Debug.LogError("Required transforms not assigned!");
        }
    }

    [ContextMenu("Build Bone Hierarchy")]
    public void BuildBoneHierarchy()
    {
        // Clear array
        System.Array.Clear(boneArray, 0, boneArray.Length);

        if (spineRoot == null || rootObject == null)
        {
            Debug.LogError("Spine root or root object is null!");
            return;
        }

        // Get all bones in order from spine34 to deepest child
        List<Transform> hierarchyChain = new List<Transform>();
        GetAllChildrenRecursive(spineRoot, hierarchyChain);

        if (hierarchyChain.Count == 0)
        {
            Debug.LogError("No spine bones found in hierarchy!");
            return;
        }

        // Reverse to get deepest child first
        hierarchyChain.Reverse();

        // Shift all elements down by one and add rootObject at the end
        int countToCopy = Mathf.Min(hierarchyChain.Count, boneArray.Length - 1);
        for (int i = 0; i < countToCopy; i++)
        {
            boneArray[i] = hierarchyChain[i + 1]; // Skip first element (i+1) and shift down
        }

        // Add rootObject to the last available spot
        if (hierarchyChain.Count < boneArray.Length)
        {
            boneArray[hierarchyChain.Count] = rootObject;
        }
        else
        {
            boneArray[boneArray.Length - 1] = rootObject;
        }

        if (showDebugInfo)
        {
            //Debug.Log("=== Final Bone Array ===");
            //Debug.Log("(First element removed, all shifted down, rootObject added last)");
            for (int i = 0; i < boneArray.Length; i++)
            {
                //Debug.Log($"Index {i}: {(boneArray[i] != null ? boneArray[i].name : "null")}");
            }
        }
    }

    private void GetAllChildrenRecursive(Transform parent, List<Transform> bones)
    {
        bones.Add(parent);
        
        foreach (Transform child in parent)
        {
            if (IsSpineBone(child.name))
            {
                GetAllChildrenRecursive(child, bones);
                break;
            }
        }
    }

    private bool IsSpineBone(string boneName)
    {
        return boneName.ToLower().StartsWith("spine");
    }

    public Transform[] GetBoneArray()
    {
        return boneArray;
    }
}