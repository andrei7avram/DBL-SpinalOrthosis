using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parent : MonoBehaviour
{
    [Header("List A: Rig Bones")]
    public List<Transform> rigBones;

    [Header("List B: Spine Vertebrae")]
    public List<Transform> vertebrae;

    private List<Quaternion> previousRigBoneRotations;

    void Start()
    {
        previousRigBoneRotations = new List<Quaternion>(rigBones.Count);
        foreach (var rigBone in rigBones)
        {
            previousRigBoneRotations.Add(rigBone != null ? rigBone.localRotation : Quaternion.identity);
        }
    }

    void LateUpdate()
    {
        int count = Mathf.Min(rigBones.Count, vertebrae.Count);

        for (int i = 0; i < count; i++)
        {
            if (rigBones[i] != null && vertebrae[i] != null)
            {
                Quaternion currentRotation = rigBones[i].localRotation;

                Vector3 rigBoneEulerRotation = currentRotation.eulerAngles;

                // Clamp the rotation based on the spine segment
                if (i < 7) // Cervical Spine
                {
                    rigBoneEulerRotation.x = ClampAngle(rigBoneEulerRotation.x, -70, 90); // Flexion/Extension
                    rigBoneEulerRotation.y = ClampAngle(rigBoneEulerRotation.y, -90, 90); // Axial Rotation
                    rigBoneEulerRotation.z = ClampAngle(rigBoneEulerRotation.z, -50, 50); // Lateral Flexion
                }
                else if (i < 19) // Thoracic Spine
                {
                    rigBoneEulerRotation.x = ClampAngle(rigBoneEulerRotation.x, -25, 45); 
                    rigBoneEulerRotation.y = ClampAngle(rigBoneEulerRotation.y, -35, 35); 
                    rigBoneEulerRotation.z = ClampAngle(rigBoneEulerRotation.z, -30, 30); 
                }
                else if (i < 24) // Lumbar Spine
                {
                    rigBoneEulerRotation.x = ClampAngle(rigBoneEulerRotation.x, -35, 80); 
                    rigBoneEulerRotation.y = ClampAngle(rigBoneEulerRotation.y, -10, 10); 
                    rigBoneEulerRotation.z = ClampAngle(rigBoneEulerRotation.z, -20, 20); 
                }
                else 
                {
                    rigBoneEulerRotation = Vector3.zero;
                }

                
                rigBones[i].localRotation = Quaternion.Euler(rigBoneEulerRotation);

                
                Quaternion previousRotation = previousRigBoneRotations[i];
                Quaternion clampedRotation = Quaternion.Euler(rigBoneEulerRotation);
                Quaternion deltaRotation = Quaternion.Inverse(previousRotation) * clampedRotation;

                
                previousRigBoneRotations[i] = clampedRotation;

                
                vertebrae[i].localRotation = vertebrae[i].localRotation * deltaRotation;

                
                Vector3 eulerRotation = vertebrae[i].localRotation.eulerAngles;

                
                if (i < 7) 
                {
                    eulerRotation.x = ClampAngle(eulerRotation.x, -70, 90); 
                    eulerRotation.y = ClampAngle(eulerRotation.y, -90, 90);
                    eulerRotation.z = ClampAngle(eulerRotation.z, -50, 50); 
                }
                else if (i < 19) // Thoracic Spine
                {
                    eulerRotation.x = ClampAngle(eulerRotation.x, -25, 45); 
                    eulerRotation.y = ClampAngle(eulerRotation.y, -35, 35); 
                    eulerRotation.z = ClampAngle(eulerRotation.z, -30, 30); 
                }
                else if (i < 24) // Lumbar Spine
                {
                    eulerRotation.x = ClampAngle(eulerRotation.x, -35, 80); 
                    eulerRotation.y = ClampAngle(eulerRotation.y, -10, 10); 
                    eulerRotation.z = ClampAngle(eulerRotation.z, -20, 20); 
                }
                else 
                {
                    eulerRotation = Vector3.zero;
                }

                
                vertebrae[i].localRotation = Quaternion.Euler(eulerRotation);
            }
        }
    }

    
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180) angle -= 360; 
        return Mathf.Clamp(angle, min, max);
    }
}
