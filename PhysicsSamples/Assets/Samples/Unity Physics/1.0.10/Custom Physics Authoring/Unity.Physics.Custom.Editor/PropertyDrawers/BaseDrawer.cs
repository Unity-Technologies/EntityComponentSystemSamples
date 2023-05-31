using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    abstract class BaseDrawer : PropertyDrawer
    {
        protected abstract bool IsCompatible(SerializedProperty property);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return IsCompatible(property)
                ? EditorGUI.GetPropertyHeight(property)
                : EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsCompatible(property))
                DoGUI(position, property, label);
            else
                EditorGUIControls.DisplayCompatibilityWarning(position, label, ObjectNames.NicifyVariableName(GetType().Name));
        }

        protected abstract void DoGUI(Rect position, SerializedProperty property, GUIContent label);
    }
}
