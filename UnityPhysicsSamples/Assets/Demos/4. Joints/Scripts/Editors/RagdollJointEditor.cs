#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Unity.Mathematics;
using Unity.Physics.Authoring;

namespace Unity.Physics.Editor
{
    [CustomEditor(typeof(RagdollJoint))]
    public class RagdollJointEditor : UnityEditor.Editor
    {
        private EditorUtilities.AxisEditor m_AxisEditor = new EditorUtilities.AxisEditor();
        private JointAngularLimitHandle m_LimitHandle = new JointAngularLimitHandle();

        public override void OnInspectorGUI()
        {
            RagdollJoint ragdoll = (RagdollJoint)target;

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginVertical();
            GUILayout.Space(10.0f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Editors:");
            ragdoll.EditPivots = GUILayout.Toggle(ragdoll.EditPivots, new GUIContent("Pivot"), "Button");
            ragdoll.EditAxes = GUILayout.Toggle(ragdoll.EditAxes, new GUIContent("Axis"), "Button");
            ragdoll.EditLimits = GUILayout.Toggle(ragdoll.EditLimits, new GUIContent("Limits"), "Button");
            GUILayout.EndHorizontal();

            GUILayout.Space(10.0f);
            GUILayout.EndVertical();

            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        private static void DrawCone(float3 point, float3 axis, float angle, Color color)
        {
            new DebugStream.Cone
            {
                Point = point,
                Axis = axis,
                Angle = angle,
                Color = color
            }.Draw();
        }

        protected virtual void OnSceneGUI()
        {
            RagdollJoint ragdoll = (RagdollJoint)target;

            bool drawCones = false;
            if (ragdoll.EditPivots)
            {
                EditorUtilities.EditPivot(ragdoll.worldFromA, ragdoll.worldFromB, ragdoll.AutoSetConnected,
                    ref ragdoll.PositionLocal, ref ragdoll.PositionInConnectedEntity, ragdoll);
            }
            if (ragdoll.EditAxes)
            {
                m_AxisEditor.Update(ragdoll.worldFromA, ragdoll.worldFromB, ragdoll.AutoSetConnected,
                    ragdoll.PositionLocal, ragdoll.PositionInConnectedEntity, ref ragdoll.TwistAxisLocal, ref ragdoll.TwistAxisInConnectedEntity,
                    ref ragdoll.PerpendicularAxisLocal, ref ragdoll.PerpendicularAxisInConnectedEntity, ragdoll);
                drawCones = true;
            }
            if (ragdoll.EditLimits)
            {
                EditorUtilities.EditLimits(ragdoll.worldFromA, ragdoll.worldFromB, ragdoll.PositionLocal, ragdoll.TwistAxisLocal, ragdoll.TwistAxisInConnectedEntity,
                    ragdoll.PerpendicularAxisLocal, ragdoll.PerpendicularAxisInConnectedEntity, ref ragdoll.MinTwistAngle, ref ragdoll.MaxTwistAngle, m_LimitHandle, ragdoll);
            }

            if (drawCones)
            {
                float3 pivotB = math.transform(ragdoll.worldFromB, ragdoll.PositionInConnectedEntity);
                float3 axisB = math.rotate(ragdoll.worldFromB, ragdoll.TwistAxisInConnectedEntity);
                DrawCone(pivotB, axisB, math.radians(ragdoll.MaxConeAngle), Color.yellow);

                float3 perpendicularB = math.rotate(ragdoll.worldFromB, ragdoll.PerpendicularAxisInConnectedEntity);
                DrawCone(pivotB, perpendicularB, math.radians(ragdoll.MinPerpendicularAngle + 90f), Color.red);
                DrawCone(pivotB, perpendicularB, math.radians(ragdoll.MaxPerpendicularAngle + 90f), Color.red);
            }
        }
    }
}

#endif
