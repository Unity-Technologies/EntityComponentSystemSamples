using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Miscellaneous.Splines
{
    public class SnakeSettingsAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public int        NumPartsPerSnake;
        public int        NumSnakes;
        public float      Speed;
        public float      Spacing;

        class Baker : Baker<SnakeSettingsAuthoring>
        {
            public override void Bake(SnakeSettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SnakeSettings
                {
                    Prefab           = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                    NumPartsPerSnake = authoring.NumPartsPerSnake,
                    NumSnakes        = authoring.NumSnakes,
                    Speed            = authoring.Speed,
                    Spacing          = authoring.Spacing
                });
            }
        }
    }

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
