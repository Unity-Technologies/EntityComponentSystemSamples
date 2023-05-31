using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Physics.Editor
{
    class BeveledCylinderBoundsHandle : PrimitiveBoundsHandle
    {
        public float bevelRadius
        {
            get => math.min(m_BevelRadius, math.cmin(GetSize()) * 0.5f);
            set
            {
                if (!m_IsDragging)
                    m_BevelRadius = math.max(0f, value);
            }
        }

        float m_BevelRadius = ConvexHullGenerationParameters.Default.BevelRadius;

        bool m_IsDragging = false;

        public float height
        {
            get => GetSize().z;
            set
            {
                var size = GetSize();
                size.z = math.max(0f, value);
                SetSize(size);
            }
        }

        public float radius
        {
            get
            {
                var size = (float3)GetSize();
                var diameter = 0f;
                // only consider size values on enabled axes
                if (IsAxisEnabled(0)) diameter = math.max(diameter, math.abs(size.x));
                else if (IsAxisEnabled(1)) diameter = math.max(diameter, math.abs(size.y));
                return diameter * 0.5f;
            }
            set
            {
                var size = (float3)GetSize();
                size.x = size.y = math.max(0f, value * 2.0f);
                SetSize(size);
            }
        }

        public int sideCount
        {
            get => m_SideCount;
            set
            {
                if (value == m_SideCount)
                    return;

                m_SideCount = value;

                Array.Resize(ref m_TopPoints,        m_SideCount * 2);
                Array.Resize(ref m_BottomPoints,     m_SideCount * 2);
                Array.Resize(ref m_Corners,          m_SideCount * 2);
            }
        }
        int m_SideCount;

        PhysicsBoundsHandleUtility.Corner[] m_Corners = Array.Empty<PhysicsBoundsHandleUtility.Corner>();
        Vector3[] m_TopPoints = Array.Empty<Vector3>();
        Vector3[] m_BottomPoints = Array.Empty<Vector3>();

        public new void DrawHandle()
        {
            int prevHotControl = GUIUtility.hotControl;
            if (prevHotControl == 0)
                m_IsDragging = false;
            base.DrawHandle();
            int currHotcontrol = GUIUtility.hotControl;
            if (currHotcontrol != prevHotControl)
                m_IsDragging = currHotcontrol != 0;
        }

        protected override void DrawWireframe()
        {
            using (new Handles.DrawingScope(Handles.matrix))
            {
                var backfacedColor = PhysicsBoundsHandleUtility.GetStateColor(true);
                var frontfacedColor = Handles.color;
                bool isCameraInsideBox = false;

                var radius          = this.radius;
                var bevelRadius     = this.bevelRadius;

                var halfHeight      = new float3(0f, 0f, height * 0.5f);
                var ctr             = (float3)center;
                var halfAngleStep   = math.PI / m_SideCount;
                var angleStep       = 2f * halfAngleStep;
                const float kHalfPI = (math.PI * 0.5f);

                var bottom          = ctr + halfHeight + new float3 { z = bevelRadius };
                var top             = ctr - halfHeight - new float3 { z = bevelRadius };
                var tangent         = new float3(1, 0, 0);
                var binormal        = new float3(0, 1, 0);
                var topBackFaced    = PhysicsBoundsHandleUtility.IsBackfaced(top,    -tangent, binormal, axes, isCameraInsideBox);
                var bottomBackFaced = PhysicsBoundsHandleUtility.IsBackfaced(bottom,  tangent, binormal, axes, isCameraInsideBox);

                var cameraCenter = float3.zero;
                var cameraForward = new float3 { z = 1f };
                if (Camera.current != null)
                {
                    cameraCenter = Camera.current.transform.position;
                    cameraForward = Camera.current.transform.forward;
                }

                // Since the geometry is transformed by Handles.matrix during rendering, we transform the camera position
                // by the inverse matrix so that the two-shaded wireframe will have the proper orientation.
                var invMatrix   = Handles.inverseMatrix;
                cameraCenter    = invMatrix.MultiplyPoint(cameraCenter);
                cameraForward   = invMatrix.MultiplyVector(cameraForward);
                var cameraOrtho = Camera.current != null && Camera.current.orthographic;

                var noSides     = (radius - bevelRadius) < PhysicsBoundsHandleUtility.kDistanceEpsilon;
                var up          = new float3(0, 0, -1f);

                var t           = ((m_SideCount - 2) * angleStep);
                var xyAngle0    = new float3(math.cos(t), math.sin(t), 0f);

                t = (m_SideCount - 1) * angleStep;
                var xyAngle1    = new float3(math.cos(t), math.sin(t), 0f);
                var sideways1   = new float3(math.cos(t + kHalfPI - halfAngleStep), math.sin(t + kHalfPI - halfAngleStep), 0f);
                var direction1  = new float3(math.cos(t + halfAngleStep), math.sin(t + halfAngleStep), 0f);
                var bevelGreaterThanZero = bevelRadius > 0f;
                var bevelLessThanCylinderRadius = bevelRadius < radius;
                for (var i = 0; i < m_SideCount; ++i)
                {
                    t = i * angleStep;
                    var xyAngle2    = new float3(math.cos(t), math.sin(t), 0f);
                    var sideways2   = new float3(math.cos(t + kHalfPI - halfAngleStep), math.sin(t + kHalfPI - halfAngleStep), 0f);
                    var direction2  = new float3(math.cos(t + halfAngleStep), math.sin(t + halfAngleStep), 0f);

                    var offset0     = xyAngle0 * (radius - bevelRadius);
                    var offset1     = xyAngle1 * (radius - bevelRadius);
                    var offset2     = xyAngle2 * (radius - bevelRadius);

                    var top1     = ctr + offset1 - (halfHeight - new float3 { z = bevelRadius });
                    var bottom1  = ctr + offset1 + (halfHeight - new float3 { z = bevelRadius });

                    var top2     = ctr + offset2 - (halfHeight - new float3 { z = bevelRadius });
                    var bottom2  = ctr + offset2 + (halfHeight - new float3 { z = bevelRadius });

                    var startOffset     = direction1 * bevelRadius;

                    if (bevelGreaterThanZero)
                    {
                        var upOffset = up * bevelRadius;

                        // top/bottom caps
                        if (bevelLessThanCylinderRadius)
                        {
                            Handles.color = topBackFaced ? backfacedColor : frontfacedColor;
                            Handles.DrawLine(top1 + upOffset, top2 + upOffset);

                            Handles.color = bottomBackFaced ? backfacedColor : frontfacedColor;
                            Handles.DrawLine(bottom1 - upOffset, bottom2 - upOffset);
                        }

                        var currSideMidPoint     = ctr + ((top1 + bottom1 + top2 + bottom2) * 0.25f) + startOffset;
                        var currSideBackFaced    = PhysicsBoundsHandleUtility.IsBackfaced(currSideMidPoint, up, sideways2, axes, isCameraInsideBox);

                        Handles.color = currSideBackFaced ? backfacedColor : frontfacedColor;
                        if (!noSides)
                        {
                            // Square side of bevelled cylinder
                            Handles.DrawLine(top2 + startOffset, bottom2 + startOffset);
                            Handles.DrawLine(bottom2 + startOffset, bottom1 + startOffset);
                            Handles.DrawLine(bottom1 + startOffset, top1 + startOffset);
                            Handles.DrawLine(top1 + startOffset, top2 + startOffset);
                        }
                        else
                        {
                            // Square side of bevelled cylinder, when squashed to a single line
                            Handles.DrawLine(top2 + startOffset, bottom2 + startOffset);
                        }
                    }
                    else
                    {
                        var top0                = ctr + offset0 - (halfHeight - new float3 { z = bevelRadius });
                        var bottom0             = ctr + offset0 + (halfHeight - new float3 { z = bevelRadius });

                        var prevMidPoint         = ctr + ((top0 + top1 + bottom0 + bottom1) * 0.25f) + startOffset;
                        var prevSideBackFaced    = PhysicsBoundsHandleUtility.IsBackfaced(prevMidPoint, up, sideways1, axes, isCameraInsideBox);

                        var currMidPoint         = ctr + ((top1 + top2 + bottom1 + bottom2) * 0.25f) + startOffset;
                        var currSideBackFaced    = PhysicsBoundsHandleUtility.IsBackfaced(currMidPoint, up, sideways2, axes, isCameraInsideBox);

                        // Square side of bevelled cylinder
                        Handles.color = (currSideBackFaced && prevSideBackFaced) ? backfacedColor : frontfacedColor;
                        Handles.DrawLine(bottom1 + startOffset, top1 + startOffset);

                        Handles.color = (currSideBackFaced && topBackFaced) ? backfacedColor : frontfacedColor;
                        Handles.DrawLine(top1 + startOffset, top2 + startOffset);

                        Handles.color = (currSideBackFaced && bottomBackFaced) ? backfacedColor : frontfacedColor;
                        Handles.DrawLine(bottom2 + startOffset, bottom1 + startOffset);
                    }

                    if (bevelGreaterThanZero)
                    {
                        Handles.color = frontfacedColor;

                        var cornerIndex0 = i;
                        var cornerIndex1 = i + m_SideCount;
                        {
                            var orientation = quaternion.LookRotation(xyAngle2, up);
                            var cornerNormal = math.normalize(math.mul(orientation, new float3(0f, 1f, 1f)));
                            PhysicsBoundsHandleUtility.CalculateCornerHorizon(top2,
                                new float3x3(direction1, up, direction2),
                                cornerNormal, cameraCenter, cameraForward, cameraOrtho,
                                bevelRadius, out m_Corners[cornerIndex0]);
                        }
                        {
                            var orientation = quaternion.LookRotation(xyAngle2, -up);
                            var cornerNormal = math.normalize(math.mul(orientation, new float3(0f, 1f, 1f)));
                            PhysicsBoundsHandleUtility.CalculateCornerHorizon(bottom2,
                                new float3x3(direction2, -up, direction1),
                                cornerNormal, cameraCenter, cameraForward, cameraOrtho,
                                bevelRadius, out m_Corners[cornerIndex1]);
                        }
                    }

                    direction1 = direction2;
                    sideways1 = sideways2;
                    xyAngle0 = xyAngle1;
                    xyAngle1 = xyAngle2;
                }

                if (bevelGreaterThanZero)
                {
                    Handles.color = frontfacedColor;
                    for (int a = m_SideCount - 1, b = 0; b < m_SideCount; a = b, ++b)
                    {
                        var up0 = a;
                        var dn0 = a + m_SideCount;

                        var up1 = b;
                        var dn1 = b + m_SideCount;

                        // Side horizon on vertical curved edge
                        if (m_Corners[up1].splitCount > 1 &&
                            m_Corners[dn1].splitCount > 1)
                        {
                            if ((m_Corners[up1].splitAxis[0].y || m_Corners[up1].splitAxis[1].y) &&
                                (m_Corners[dn1].splitAxis[0].y || m_Corners[dn1].splitAxis[1].y))
                            {
                                var point0 = m_Corners[up1].splitAxis[0].y ? m_Corners[up1].points[0] : m_Corners[up1].points[1];
                                var point1 = m_Corners[dn1].splitAxis[0].y ? m_Corners[dn1].points[0] : m_Corners[dn1].points[1];
                                Handles.DrawLine(point0, point1);
                            }
                        }
                        // Top horizon on horizontal curved edge
                        if (m_Corners[up0].splitCount > 1 &&
                            m_Corners[up1].splitCount > 1)
                        {
                            if ((m_Corners[up0].splitAxis[0].x || m_Corners[up0].splitAxis[1].x) &&
                                (m_Corners[up1].splitAxis[0].z || m_Corners[up1].splitAxis[1].z))
                            {
                                var point0 = m_Corners[up0].splitAxis[0].x ? m_Corners[up0].points[0] : m_Corners[up0].points[1];
                                var point1 = m_Corners[up1].splitAxis[0].z ? m_Corners[up1].points[0] : m_Corners[up1].points[1];
                                Handles.DrawLine(point0, point1);
                            }
                        }
                        // Bottom horizon on horizontal curved edge
                        if (m_Corners[dn0].splitCount > 1 &&
                            m_Corners[dn1].splitCount > 1)
                        {
                            if ((m_Corners[dn0].splitAxis[0].z || m_Corners[dn0].splitAxis[1].z) &&
                                (m_Corners[dn1].splitAxis[0].x || m_Corners[dn1].splitAxis[1].x))
                            {
                                var point0 = m_Corners[dn0].splitAxis[0].z ? m_Corners[dn0].points[0] : m_Corners[dn0].points[1];
                                var point1 = m_Corners[dn1].splitAxis[0].x ? m_Corners[dn1].points[0] : m_Corners[dn1].points[1];
                                Handles.DrawLine(point0, point1);
                            }
                        }
                    }

                    for (var i = 0; i < m_Corners.Length; ++i)
                        PhysicsBoundsHandleUtility.DrawCorner(m_Corners[i], new bool3(true, true, !noSides));
                }
            }
        }

        protected override Bounds OnHandleChanged(HandleDirection handle, Bounds boundsOnClick, Bounds newBounds)
        {
            const int k_DirectionX = 0;
            const int k_DirectionY = 1;
            const int k_DirectionZ = 2;

            var changedAxis = k_DirectionX;
            var otherRadiusAxis = k_DirectionY;
            switch (handle)
            {
                case HandleDirection.NegativeY:
                case HandleDirection.PositiveY:
                    changedAxis = k_DirectionY;
                    otherRadiusAxis = k_DirectionX;
                    break;
                case HandleDirection.NegativeZ:
                case HandleDirection.PositiveZ:
                    changedAxis = k_DirectionZ;
                    break;
            }

            var upperBound = newBounds.max;
            var lowerBound = newBounds.min;

            var convexDiameter = 2f * bevelRadius;

            // ensure changed dimension cannot be made less than convex diameter
            if (upperBound[changedAxis] - lowerBound[changedAxis] < convexDiameter)
            {
                switch (handle)
                {
                    case HandleDirection.PositiveX:
                    case HandleDirection.PositiveY:
                    case HandleDirection.PositiveZ:
                        upperBound[changedAxis] = lowerBound[changedAxis] + convexDiameter;
                        break;
                    default:
                        lowerBound[changedAxis] = upperBound[changedAxis] - convexDiameter;
                        break;
                }
            }

            // ensure radius changes uniformly
            if (changedAxis != k_DirectionZ)
            {
                var rad = 0.5f * (upperBound[changedAxis] - lowerBound[changedAxis]);

                lowerBound[otherRadiusAxis] = center[otherRadiusAxis] - rad;
                upperBound[otherRadiusAxis] = center[otherRadiusAxis] + rad;
            }

            return new Bounds((upperBound + lowerBound) * 0.5f, upperBound - lowerBound);
        }
    }
}
