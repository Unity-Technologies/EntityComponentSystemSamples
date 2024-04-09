#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Graphical.Splines
{
    [CustomEditor(typeof(SnakeSettingsAuthoring))]
    public class SnakeSettingsEditor : Editor
    {
        SerializedProperty Prefab;
        SerializedProperty Length;
        SerializedProperty Count;
        SerializedProperty Speed;
        SerializedProperty Spacing;

        void OnEnable()
        {
            Prefab = serializedObject.FindProperty("Prefab");
            Length = serializedObject.FindProperty("NumPartsPerSnake");
            Count = serializedObject.FindProperty("NumSnakes");
            Speed = serializedObject.FindProperty("Speed");
            Spacing = serializedObject.FindProperty("Spacing");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUILayout.PropertyField(Prefab);
            EditorGUILayout.PropertyField(Length);
            EditorGUILayout.PropertyField(Count);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(Speed);
            EditorGUILayout.PropertyField(Spacing);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
