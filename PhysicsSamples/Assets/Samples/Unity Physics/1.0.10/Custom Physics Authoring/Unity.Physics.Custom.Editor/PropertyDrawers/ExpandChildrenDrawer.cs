using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomPropertyDrawer(typeof(ExpandChildrenAttribute))]
    class ExpandChildrenDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            property.isExpanded = true;
            return EditorGUI.GetPropertyHeight(property)
                - EditorGUIUtility.standardVerticalSpacing
                - EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var endProperty = property.GetEndProperty();
            var childProperty = property.Copy();
            childProperty.NextVisible(true);
            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                position.height = EditorGUI.GetPropertyHeight(childProperty);
                OnChildPropertyGUI(position, childProperty);
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                childProperty.NextVisible(false);
            }
        }

        protected virtual void OnChildPropertyGUI(Rect position, SerializedProperty childProperty)
        {
            EditorGUI.PropertyField(position, childProperty, true);
        }
    }
}
