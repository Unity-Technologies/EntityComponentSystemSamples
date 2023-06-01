using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomPropertyDrawer(typeof(SoftRangeAttribute))]
    class SoftRangeDrawer : BaseDrawer
    {
        protected override bool IsCompatible(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.Float;
        }

        protected override void DoGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as SoftRangeAttribute;
            EditorGUIControls.SoftSlider(
                position, label, property, attr.SliderMin, attr.SliderMax, attr.TextFieldMin, attr.TextFieldMax
            );
        }
    }
}
