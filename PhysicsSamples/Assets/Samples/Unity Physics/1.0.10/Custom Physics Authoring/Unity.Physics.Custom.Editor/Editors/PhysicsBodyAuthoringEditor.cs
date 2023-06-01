using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomEditor(typeof(PhysicsBodyAuthoring))]
    [CanEditMultipleObjects]
    class PhysicsBodyAuthoringEditor : BaseEditor
    {
        static class Content
        {
            public static readonly GUIContent MassLabel = EditorGUIUtility.TrTextContent("Mass");
            public static readonly GUIContent CenterOfMassLabel = EditorGUIUtility.TrTextContent(
                "Center of Mass", "Center of mass in the space of this body's transform."
            );
            public static readonly GUIContent InertiaTensorLabel = EditorGUIUtility.TrTextContent(
                "Inertia Tensor", "Resistance to angular motion about each axis of rotation."
            );
            public static readonly GUIContent OrientationLabel = EditorGUIUtility.TrTextContent(
                "Orientation", "Orientation of the body's inertia tensor in the space of its transform."
            );
            public static readonly GUIContent AdvancedLabel = EditorGUIUtility.TrTextContent(
                "Advanced", "Advanced options"
            );
        }

        #pragma warning disable 649
        [AutoPopulate] SerializedProperty m_MotionType;
        [AutoPopulate] SerializedProperty m_Smoothing;
        [AutoPopulate] SerializedProperty m_Mass;
        [AutoPopulate] SerializedProperty m_GravityFactor;
        [AutoPopulate] SerializedProperty m_LinearDamping;
        [AutoPopulate] SerializedProperty m_AngularDamping;
        [AutoPopulate] SerializedProperty m_InitialLinearVelocity;
        [AutoPopulate] SerializedProperty m_InitialAngularVelocity;
        [AutoPopulate] SerializedProperty m_OverrideDefaultMassDistribution;
        [AutoPopulate] SerializedProperty m_CenterOfMass;
        [AutoPopulate] SerializedProperty m_Orientation;
        [AutoPopulate] SerializedProperty m_InertiaTensor;
        [AutoPopulate] SerializedProperty m_WorldIndex;
        [AutoPopulate] SerializedProperty m_CustomTags;
        #pragma warning restore 649

        bool showAdvanced;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_MotionType);

            if (m_MotionType.intValue != (int)BodyMotionType.Static)
                EditorGUILayout.PropertyField(m_Smoothing);

            var dynamic = m_MotionType.intValue == (int)BodyMotionType.Dynamic;

            if (dynamic)
                EditorGUILayout.PropertyField(m_Mass, Content.MassLabel);
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                var position = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
                EditorGUI.BeginProperty(position, Content.MassLabel, m_Mass);
                EditorGUI.FloatField(position, Content.MassLabel, float.PositiveInfinity);
                EditorGUI.EndProperty();
                EditorGUI.EndDisabledGroup();
            }

            if (m_MotionType.intValue == (int)BodyMotionType.Dynamic)
            {
                EditorGUILayout.PropertyField(m_LinearDamping, true);
                EditorGUILayout.PropertyField(m_AngularDamping, true);
            }

            if (m_MotionType.intValue != (int)BodyMotionType.Static)
            {
                EditorGUILayout.PropertyField(m_InitialLinearVelocity, true);
                EditorGUILayout.PropertyField(m_InitialAngularVelocity, true);
            }

            if (m_MotionType.intValue == (int)BodyMotionType.Dynamic)
            {
                EditorGUILayout.PropertyField(m_GravityFactor, true);
            }

            showAdvanced = EditorGUILayout.Foldout(showAdvanced, Content.AdvancedLabel);
            if (showAdvanced)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(m_WorldIndex);
                if (m_MotionType.intValue != (int)BodyMotionType.Static)
                {
                    EditorGUILayout.PropertyField(m_OverrideDefaultMassDistribution);
                    if (m_OverrideDefaultMassDistribution.boolValue)
                    {
                        ++EditorGUI.indentLevel;
                        EditorGUILayout.PropertyField(m_CenterOfMass, Content.CenterOfMassLabel);

                        EditorGUI.BeginDisabledGroup(!dynamic);
                        if (dynamic)
                        {
                            EditorGUILayout.PropertyField(m_Orientation, Content.OrientationLabel);
                            EditorGUILayout.PropertyField(m_InertiaTensor, Content.InertiaTensorLabel);
                        }
                        else
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            var position =
                                EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(m_InertiaTensor));
                            EditorGUI.BeginProperty(position, Content.InertiaTensorLabel, m_InertiaTensor);
                            EditorGUI.Vector3Field(position, Content.InertiaTensorLabel,
                                Vector3.one * float.PositiveInfinity);
                            EditorGUI.EndProperty();
                            EditorGUI.EndDisabledGroup();
                        }

                        EditorGUI.EndDisabledGroup();

                        --EditorGUI.indentLevel;
                    }
                }
                EditorGUILayout.PropertyField(m_CustomTags);
                --EditorGUI.indentLevel;
            }

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            DisplayStatusMessages();
        }

        MessageType m_Status;
        List<string> m_StatusMessages = new List<string>(8);

        void DisplayStatusMessages()
        {
            m_Status = MessageType.None;
            m_StatusMessages.Clear();

            var hierarchyStatus = StatusMessageUtility.GetHierarchyStatusMessage(targets, out var hierarchyStatusMessage);
            if (!string.IsNullOrEmpty(hierarchyStatusMessage))
            {
                m_StatusMessages.Add(hierarchyStatusMessage);
                m_Status = (MessageType)math.max((int)m_Status, (int)hierarchyStatus);
            }

            if (m_StatusMessages.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n\n", m_StatusMessages), m_Status);
        }
    }
}
