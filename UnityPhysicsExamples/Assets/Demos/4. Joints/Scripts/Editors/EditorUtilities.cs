using UnityEngine;
using Unity.Mathematics;
using static Unity.Physics.Math;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Unity.Physics.Editor
{
    /// <summary>
    /// Provides utilities that use Handles to set positions and axes, 
    /// </summary>
    public class EditorUtilities
    {
        // Editor for a joint pivot or pivot pair
        public static void EditPivot(RigidTransform worldFromA, RigidTransform worldFromB, bool lockBtoA,
            ref float3 pivotA, ref float3 pivotB, Object target)
        {
            EditorGUI.BeginChangeCheck();
            float3 pivotAinW = Handles.PositionHandle(math.transform(worldFromA, pivotA), quaternion.identity);
            float3 pivotBinW;
            
            if (lockBtoA)
            {
                pivotBinW = pivotAinW;
                pivotB = math.transform(math.inverse(worldFromB), pivotBinW);
            }
            else
            {
                pivotBinW = Handles.PositionHandle(math.transform(worldFromB, pivotB), quaternion.identity);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Edit joint pivot");
                pivotA = math.transform(math.inverse(worldFromA), pivotAinW);
                pivotB = math.transform(math.inverse(worldFromB), pivotBinW);
            }

            Handles.DrawLine(worldFromA.pos, pivotAinW);
            Handles.DrawLine(worldFromB.pos, pivotBinW);
        }

        // Editor for a joint axis or axis pair
        public class AxisEditor
        {
            // Even though we're only editing axes and not rotations, we need to track a full rotation in order to keep the rotation handle stable
            private quaternion m_RefA = quaternion.identity;
            private quaternion m_RefB = quaternion.identity;

            // Detect changes in the object being edited to reset the reference orientations
            private Object m_LastTarget;

            private static bool NormalizeSafe(ref float3 x)
            {
                float lengthSq = math.lengthsq(x);
                const float epsSq = 1e-8f;
                if (math.abs(lengthSq - 1) > epsSq)
                {
                    if (lengthSq > epsSq)
                    {
                        x *= math.rsqrt(lengthSq);
                    }
                    else
                    {
                        x = new float3(1, 0, 0);
                    }

                    return true;
                }

                return false;
            }

            private static bool NormalizePerpendicular(float3 axis, ref float3 perpendicular)
            {
                // make sure perpendicular is actually perpendicular to direction
                float dot = math.dot(axis, perpendicular);
                float absDot = math.abs(dot);
                if (absDot > 1.0f - 1e-5f)
                {
                    // parallel, choose an arbitrary perpendicular
                    float3 dummy;
                    CalculatePerpendicularNormalized(axis, out perpendicular, out dummy);
                    return true;
                }

                if (absDot > 1e-5f)
                {
                    // reject direction
                    perpendicular -= dot * axis;
                    NormalizeSafe(ref perpendicular);
                    return true;
                }

                return NormalizeSafe(ref perpendicular);
            }

            public void Update(RigidTransform worldFromA, RigidTransform worldFromB, bool lockBtoA, float3 pivotA, float3 pivotB,
                ref float3 directionA, ref float3 directionB, ref float3 perpendicularA, ref float3 perpendicularB, Object target)
            {
                // Work in world space
                float3 directionAinW = math.rotate(worldFromA, directionA);
                float3 directionBinW = math.rotate(worldFromB, directionB);
                float3 perpendicularAinW = math.rotate(worldFromB, perpendicularA);
                float3 perpendicularBinW = math.rotate(worldFromB, perpendicularB);
                bool changed = false;

                // If the target changed, fix up the inputs and reset the reference orientations to align with the new target's axes
                if (target != m_LastTarget)
                {
                    m_LastTarget = target;

                    // Enforce normalized directions
                    changed |= NormalizeSafe(ref directionAinW);
                    changed |= NormalizeSafe(ref directionBinW);

                    // Enforce normalized perpendiculars, orthogonal to their respective directions
                    changed |= NormalizePerpendicular(directionAinW, ref perpendicularAinW);
                    changed |= NormalizePerpendicular(directionBinW, ref perpendicularBinW);

                    // Calculate the rotation of the joint in A from direction and perpendicular
                    float3x3 rotationA = new float3x3(directionAinW, perpendicularAinW, math.cross(directionAinW, perpendicularAinW));
                    m_RefA = new quaternion(rotationA);

                    if (lockBtoA)
                    {
                        m_RefB = m_RefA;
                    }
                    else
                    {
                        // Calculate the rotation of the joint in B from direction and perpendicular
                        float3x3 rotationB = new float3x3(directionBinW, perpendicularBinW, math.cross(directionBinW, perpendicularBinW));
                        m_RefB = new quaternion(rotationB);
                    }
                }

                EditorGUI.BeginChangeCheck();

                // Make rotators
                quaternion oldRefA = m_RefA;
                quaternion oldRefB = m_RefB;

                float3 pivotAinW = math.transform(worldFromA, pivotA);
                m_RefA = Handles.RotationHandle(m_RefA, pivotAinW);

                float3 pivotBinW;
                if (lockBtoA)
                {
                    directionB = math.rotate(math.inverse(worldFromB), directionAinW);
                    perpendicularB = math.rotate(math.inverse(worldFromB), perpendicularAinW);
                    pivotBinW = pivotAinW;
                    m_RefB = m_RefA;
                }
                else
                {
                    pivotBinW = math.transform(worldFromB, pivotB);
                    m_RefB = Handles.RotationHandle(m_RefB, pivotBinW);
                }

                // Apply changes from the rotators
                if (EditorGUI.EndChangeCheck())
                {
                    quaternion dqA = math.mul(m_RefA, math.inverse(oldRefA));
                    quaternion dqB = math.mul(m_RefB, math.inverse(oldRefB));
                    directionAinW = math.mul(dqA, directionAinW);
                    directionBinW = math.mul(dqB, directionBinW);
                    perpendicularAinW = math.mul(dqB, perpendicularAinW);
                    perpendicularBinW = math.mul(dqB, perpendicularBinW);
                    changed = true;
                }

                // Write back if the axes changed
                if (changed)
                {
                    Undo.RecordObject(target, "Edit joint axis");
                    directionA = math.rotate(math.inverse(worldFromA), directionAinW);
                    directionB = math.rotate(math.inverse(worldFromB), directionBinW);
                    perpendicularA = math.rotate(math.inverse(worldFromB), perpendicularAinW);
                    perpendicularB = math.rotate(math.inverse(worldFromB), perpendicularBinW);
                }

                // Draw the updated axes
                float3 z = new float3(0, 0, 1); // ArrowHandleCap() draws an arrow pointing in (0, 0, 1)
                Handles.ArrowHandleCap(0, pivotAinW, Quaternion.FromToRotation(z, directionAinW), HandleUtility.GetHandleSize(pivotAinW) * 0.75f, Event.current.type);
                Handles.ArrowHandleCap(0, pivotAinW, Quaternion.FromToRotation(z, perpendicularAinW), HandleUtility.GetHandleSize(pivotAinW) * 0.75f, Event.current.type);
                if (!lockBtoA)
                {
                    Handles.ArrowHandleCap(0, pivotBinW, Quaternion.FromToRotation(z, directionBinW), HandleUtility.GetHandleSize(pivotBinW) * 0.75f, Event.current.type);
                    Handles.ArrowHandleCap(0, pivotBinW, Quaternion.FromToRotation(z, perpendicularBinW), HandleUtility.GetHandleSize(pivotBinW) * 0.75f, Event.current.type);
                }
            }
        }

        public static void EditLimits(RigidTransform worldFromA, RigidTransform worldFromB, float3 pivotA, float3 axisA, float3 axisB, float3 perpendicularA, float3 perpendicularB,
            ref float minLimit, ref float maxLimit, JointAngularLimitHandle limitHandle, Object target)
        {
            // Transform to world space
            float3 pivotAinW = math.transform(worldFromA, pivotA);
            float3 axisAinW = math.rotate(worldFromA, axisA);
            float3 perpendicularAinW = math.rotate(worldFromA, perpendicularA);
            float3 axisBinW = math.rotate(worldFromA, axisB);
            float3 perpendicularBinW = math.rotate(worldFromB, perpendicularB);

            // Get rotations from joint space
            // JointAngularLimitHandle uses axis = (1, 0, 0) with angle = 0 at (0, 0, 1), so choose the rotations to point those in the directions of our axis and perpendicular
            float3x3 worldFromJointA = new float3x3(axisAinW, -math.cross(axisAinW, perpendicularAinW), perpendicularAinW);
            float3x3 worldFromJointB = new float3x3(axisBinW, -math.cross(axisBinW, perpendicularBinW), perpendicularBinW);
            float3x3 jointBFromA = math.mul(math.transpose(worldFromJointB), worldFromJointA);

            // Set orientation for the angular limit control
            float angle = CalculateTwistAngle(new quaternion(jointBFromA), 0); // index = 0 because axis is the first column in worldFromJoint
            quaternion limitOrientation = math.mul(quaternion.AxisAngle(axisAinW, angle), new quaternion(worldFromJointA));
            Matrix4x4 handleMatrix = Matrix4x4.TRS(pivotAinW, limitOrientation, Vector3.one);

            float size = HandleUtility.GetHandleSize(pivotAinW) * 0.75f;

            limitHandle.xMin = -maxLimit;
            limitHandle.xMax = -minLimit;
            limitHandle.xMotion = ConfigurableJointMotion.Limited;
            limitHandle.yMotion = ConfigurableJointMotion.Locked;
            limitHandle.zMotion = ConfigurableJointMotion.Locked;
            limitHandle.yHandleColor = new Color(0, 0, 0, 0);
            limitHandle.zHandleColor = new Color(0, 0, 0, 0);
            limitHandle.radius = size;

            using (new Handles.DrawingScope(handleMatrix))
            {
                // Draw the reference axis
                float3 z = new float3(0, 0, 1); // ArrowHandleCap() draws an arrow pointing in (0, 0, 1)
                Handles.ArrowHandleCap(0, float3.zero, Quaternion.FromToRotation(z, new float3(1, 0, 0)), size, Event.current.type);

                // Draw the limit editor handle
                EditorGUI.BeginChangeCheck();
                limitHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    // Record the target object before setting new limits so changes can be undone/redone
                    Undo.RecordObject(target, "Edit joint angular limits");
                    minLimit = -limitHandle.xMax;
                    maxLimit = -limitHandle.xMin;
                }
            }
        }
    }
}
#endif
