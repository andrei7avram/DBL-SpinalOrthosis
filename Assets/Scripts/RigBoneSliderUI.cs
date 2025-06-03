using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RigBoneSliderUI : MonoBehaviour
{
    [System.Serializable]
    public class BoneSliderGroup
    {
        public Transform bone;
        public Slider sliderX;
        public Slider sliderY;
        public Slider sliderZ;
    }

    public List<BoneSliderGroup> boneSliders;

    void Start()
    {
        foreach (var group in boneSliders)
        {
            if (group.bone != null)
            {
                Vector3 euler = group.bone.localEulerAngles;
                if (group.sliderX != null) group.sliderX.value = euler.x;
                if (group.sliderY != null) group.sliderY.value = euler.y;
                if (group.sliderZ != null) group.sliderZ.value = euler.z;
            }
        }
    }

    void Update()
    {
        foreach (var group in boneSliders)
        {
            if (group.bone != null)
            {
                Vector3 euler = group.bone.localEulerAngles;
                if (group.sliderX != null) euler.x = group.sliderX.value;
                if (group.sliderY != null) euler.y = group.sliderY.value;
                if (group.sliderZ != null) euler.z = group.sliderZ.value;
                group.bone.localEulerAngles = euler;
            }
        }
    }
}