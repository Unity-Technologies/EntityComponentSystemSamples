using UnityEngine;

namespace Unity.Physics.Authoring
{
    sealed class EnumFlagsAttribute : PropertyAttribute {}
    sealed class ExpandChildrenAttribute : PropertyAttribute {}
    sealed class SoftRangeAttribute : PropertyAttribute
    {
        public readonly float SliderMin;
        public readonly float SliderMax;
        public float TextFieldMin { get; set; }
        public float TextFieldMax { get; set; }

        public SoftRangeAttribute(float min, float max)
        {
            SliderMin = TextFieldMin = min;
            SliderMax = TextFieldMax = max;
        }
    }
}
