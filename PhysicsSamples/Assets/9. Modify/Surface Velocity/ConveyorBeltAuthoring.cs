using Unity.Physics;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics.Authoring;

#if UNITY_EDITOR
// Displays conveyor belt data in Editor.
using UnityEditor;
#endif

namespace Modify
{
    [RequireComponent(typeof(PhysicsBodyAuthoring))]
    public class ConveyorBeltAuthoring : MonoBehaviour
    {
        public float Speed = 0.0f;
        public bool IsLinear = true;
        public Vector3 LocalDirection = Vector3.forward;

        private float _Offset = 0.0f;

        public void OnDrawGizmos()
        {
            float speed = Speed;
            if (!IsLinear)
            {
                speed = math.radians(speed);
            }

            if (ConveyorBeltDisplaySystem.ComputeDebugDisplayData(
                Math.DecomposeRigidBodyTransform(transform.localToWorldMatrix), speed, LocalDirection,
                UnityEngine.Time.deltaTime, IsLinear, ref _Offset,
                out RigidTransform worldDrawingTransform, out float3 boxSize))
            {
                var originalColor = Gizmos.color;
                var originalMatrix = Gizmos.matrix;

                Gizmos.color = Color.blue;

                Matrix4x4 newMatrix = new Matrix4x4();
                newMatrix.SetTRS(worldDrawingTransform.pos, worldDrawingTransform.rot, Vector3.one);
                Gizmos.matrix = newMatrix;

                Gizmos.DrawWireCube(Vector3.zero, boxSize);

                Gizmos.color = originalColor;
                Gizmos.matrix = originalMatrix;
            }
        }

        class Baker : Baker<ConveyorBeltAuthoring>
        {
            public override void Bake(ConveyorBeltAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ConveyorBelt
                {
                    Speed = authoring.IsLinear ? authoring.Speed : math.radians(authoring.Speed),
                    IsAngular = !authoring.IsLinear,
                    LocalDirection = authoring.LocalDirection.normalized,
                });

                AddComponent(entity, new ConveyorBeltDebugDisplayData
                {
                    Offset = 0.0f
                });
            }
        }
    }

    public struct ConveyorBelt : IComponentData
    {
        public float3 LocalDirection;
        public float Speed;
        public bool IsAngular;
    }

    public struct ConveyorBeltDebugDisplayData : IComponentData
    {
        public float Offset;
    }


#if UNITY_EDITOR
    // Displays conveyor belt data in Editor.
    [CustomEditor(typeof(ConveyorBeltAuthoring)), CanEditMultipleObjects]
    public class ConveyorBeltAuthoringEditor : Editor
    {
        SerializedProperty m_Speed;
        SerializedProperty m_IsLinear;
        SerializedProperty m_LocalDirection;

        void OnEnable()
        {
            m_Speed = serializedObject.FindProperty("Speed");
            m_IsLinear = serializedObject.FindProperty("IsLinear");
            m_LocalDirection = serializedObject.FindProperty("LocalDirection");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Speed);
            EditorGUILayout.PropertyField(m_IsLinear);
            using (new EditorGUI.DisabledGroupScope(!m_IsLinear.boolValue))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LocalDirection);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
