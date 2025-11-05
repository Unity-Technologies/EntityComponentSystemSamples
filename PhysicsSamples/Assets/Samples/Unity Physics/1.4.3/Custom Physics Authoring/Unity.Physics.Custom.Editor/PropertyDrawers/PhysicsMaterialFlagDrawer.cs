using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsMaterialFlag))]
    class PhysicsMaterialFlagDrawer : BaseDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        protected override bool IsCompatible(SerializedProperty property) => true;

        protected override void DoGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("Value"), label);
            EditorGUI.EndProperty();
        }
    }

}
