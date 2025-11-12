using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsMaterialCoefficient))]
    class PhysicsMaterialCoefficientDrawer : BaseDrawer
    {
        static class Styles
        {
            public const float PopupWidth = 100f;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        protected override bool IsCompatible(SerializedProperty property) => true;

        protected override void DoGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(
                new Rect(position) { xMax = position.xMax - Styles.PopupWidth },
                property.FindPropertyRelative("Value"),
                label
            );

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(
                new Rect(position) { xMin = position.xMax - Styles.PopupWidth + EditorGUIUtility.standardVerticalSpacing },
                property.FindPropertyRelative("CombineMode"),
                GUIContent.none
            );
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
