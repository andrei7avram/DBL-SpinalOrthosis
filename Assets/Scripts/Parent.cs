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
        // Initialize the list to store the previous rotations of the rig bones
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
                // Get the current local rotation of the rig bone
                Quaternion currentRotation = rigBones[i].localRotation;

                // Convert the rig bone's rotation to Euler angles for clamping
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
                    rigBoneEulerRotation.x = ClampAngle(rigBoneEulerRotation.x, -25, 45); // Flexion/Extension
                    rigBoneEulerRotation.y = ClampAngle(rigBoneEulerRotation.y, -35, 35); // Axial Rotation
                    rigBoneEulerRotation.z = ClampAngle(rigBoneEulerRotation.z, -30, 30); // Lateral Flexion
                }
                else if (i < 24) // Lumbar Spine
                {
                    rigBoneEulerRotation.x = ClampAngle(rigBoneEulerRotation.x, -35, 80); // Flexion/Extension
                    rigBoneEulerRotation.y = ClampAngle(rigBoneEulerRotation.y, -10, 10); // Axial Rotation
                    rigBoneEulerRotation.z = ClampAngle(rigBoneEulerRotation.z, -20, 20); // Lateral Flexion
                }
                else // Sacrum and Coccyx (No rotation allowed)
                {
                    rigBoneEulerRotation = Vector3.zero;
                }

                // Apply the clamped rotation back to the rig bone
                rigBones[i].localRotation = Quaternion.Euler(rigBoneEulerRotation);

                // Calculate the delta rotation (change in rotation)
                Quaternion previousRotation = previousRigBoneRotations[i];
                Quaternion clampedRotation = Quaternion.Euler(rigBoneEulerRotation);
                Quaternion deltaRotation = Quaternion.Inverse(previousRotation) * clampedRotation;

                // Update the previous rotation for the next frame
                previousRigBoneRotations[i] = clampedRotation;

                // Apply the delta rotation to the vertebra's current rotation
                vertebrae[i].localRotation = vertebrae[i].localRotation * deltaRotation;

                // Convert the vertebra's rotation to Euler angles for clamping
                Vector3 eulerRotation = vertebrae[i].localRotation.eulerAngles;

                // Clamp the rotation based on the spine segment
                if (i < 7) // Cervical Spine
                {
                    eulerRotation.x = ClampAngle(eulerRotation.x, -70, 90); // Flexion/Extension
                    eulerRotation.y = ClampAngle(eulerRotation.y, -90, 90); // Axial Rotation
                    eulerRotation.z = ClampAngle(eulerRotation.z, -50, 50); // Lateral Flexion
                }
                else if (i < 19) // Thoracic Spine
                {
                    eulerRotation.x = ClampAngle(eulerRotation.x, -25, 45); // Flexion/Extension
                    eulerRotation.y = ClampAngle(eulerRotation.y, -35, 35); // Axial Rotation
                    eulerRotation.z = ClampAngle(eulerRotation.z, -30, 30); // Lateral Flexion
                }
                else if (i < 24) // Lumbar Spine
                {
                    eulerRotation.x = ClampAngle(eulerRotation.x, -35, 80); // Flexion/Extension
                    eulerRotation.y = ClampAngle(eulerRotation.y, -10, 10); // Axial Rotation
                    eulerRotation.z = ClampAngle(eulerRotation.z, -20, 20); // Lateral Flexion
                }
                else // Sacrum and Coccyx (No rotation allowed)
                {
                    eulerRotation = Vector3.zero;
                }

                // Apply the clamped rotation back to the vertebra
                vertebrae[i].localRotation = Quaternion.Euler(eulerRotation);
            }
        }
    }

    // Helper method to clamp angles between a min and max range
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180) angle -= 360; // Convert to -180 to 180 range
        return Mathf.Clamp(angle, min, max);
    }
}
