using System;
using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
    class EnumFlagsDrawer : BaseDrawer
    {
        protected override bool IsCompatible(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.Enum;
        }

        protected override void DoGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var value = property.longValue;
            EditorGUI.BeginChangeCheck();
            value = Convert.ToInt64(
                EditorGUI.EnumFlagsField(position, label, (Enum)Enum.ToObject(fieldInfo.FieldType, value))
            );
            if (EditorGUI.EndChangeCheck())
                property.longValue = value;

            EditorGUI.EndProperty();
        }
    }
}
