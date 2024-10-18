using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Physics.Editor
{
    class PhysicsCapsuleBoundsHandle : CapsuleBoundsHandle
    {
        static PhysicsBoundsHandleUtility.Corner[] s_Corners = new PhysicsBoundsHandleUtility.Corner[8];

        protected override void DrawWireframe()
        {
            if (this.radius <= 0f)
            {
                base.DrawWireframe();
                return;
            }

            var cameraPos = default(float3);
            var cameraFwd = new float3 { z = 1f };
            var cameraOrtho = true;
            if (Camera.current != null)
            {
                cameraPos = Camera.current.transform.position;
                cameraFwd = Camera.current.transform.forward;
                cameraOrtho = Camera.current.orthographic;
            }

            var size        = new float3(this.radius * 2f, this.radius * 2f, height);
            var radius      = this.radius;
            var origin      = (float3)this.center;
            var bounds      = new Bounds(this.center, size);

            // Since the geometry is transformed by Handles.matrix during rendering, we transform the camera position
            // by the inverse matrix so that the two-shaded wireframe will have the proper orientation.
            var invMatrix       = Handles.inverseMatrix;
            var cameraCenter    = (float3)invMatrix.MultiplyPoint(cameraPos);
            var cameraForward   = (float3)invMatrix.MultiplyVector(cameraFwd);

            bool isCameraInsideBox = Camera.current != null
                && bounds.Contains(invMatrix.MultiplyPoint(cameraPos));

            PhysicsBoundsHandleUtility.DrawFace(origin, size * new float3(1f,  1f,  1f), radius, 0, axes, isCameraInsideBox);
            PhysicsBoundsHandleUtility.DrawFace(origin, size * new float3(-1f,  1f,  1f), radius, 0, axes, isCameraInsideBox);
            PhysicsBoundsHandleUtility.DrawFace(origin, size * new float3(1f,  1f,  1f), radius, 1, axes, isCameraInsideBox);
            PhysicsBoundsHandleUtility.DrawFace(origin, size * new float3(1f, -1f,  1f), radius, 1, axes, isCameraInsideBox);
            PhysicsBoundsHandleUtility.DrawFace(origin, size * new float3(1f,  1f,  1f), radius, 2, axes, isCameraInsideBox);
            PhysicsBoundsHandleUtility.DrawFace(origin, size * new float3(1f,  1f, -1f), radius, 2, axes, isCameraInsideBox);

            var corner = 0.5f * size - new float3(1f) * radius;
            var axisx = new float3(1f, 0f, 0f);
            var axisy = new float3(0f, 1f, 0f);
            var axisz = new float3(0f, 0f, 1f);

            PhysicsBoundsHandleUtility.CalculateCornerHorizon(origin + corner * new float3(-1f,  1f, -1f), quaternion.LookRotation(-axisz,  axisy), cameraCenter, cameraForward, cameraOrtho, radius, out s_Corners[0]);
            PhysicsBoundsHandleUtility.CalculateCornerHorizon(origin + corner * new float3(-1f,  1f,  1f), quaternion.LookRotation(-axisx,  axisy), cameraCenter, cameraForward, cameraOrtho, radius, out s_Corners[1]);
            PhysicsBoundsHandleUtility.CalculateCornerHorizon(origin + corner * new float3(1f,  1f,  1f), quaternion.LookRotation(axisz,  axisy), cameraCenter, cameraForward, cameraOrtho, radius, out s_Corners[2]);
            PhysicsBoundsHandleUtility.CalculateCornerHorizon(origin + corner * new float3(1f,  1f, -1f), quaternion.LookRotation(axisx,  axisy), cameraCenter, cameraForward, cameraOrtho, radius, out s_Corners[3]);

            PhysicsBoundsHandleUtility.CalculateCornerHorizon(origin + corner * new float3(-1f, -1f, -1f), quaternion.LookRotation(-axisx, -axisy), cameraCenter, cameraForward, cameraOrtho, radius, out s_Corners[4]);
            PhysicsBoundsHandleUtility.CalculateCornerHorizon(origin + corner * new float3(-1f, -1f,  1f), quaternion.LookRotation(axisz, -axisy), cameraCenter, cameraForward, cameraOrtho, radius, out s_Corners[5]);
            PhysicsBoundsHandleUtility.CalculateCornerHorizon(origin + corner * new float3(1f, -1f,  1f), quaternion.LookRotation(axisx, -axisy), cameraCenter, cameraForward, cameraOrtho, radius, out s_Corners[6]);
            PhysicsBoundsHandleUtility.CalculateCornerHorizon(origin + corner * new float3(1f, -1f, -1f), quaternion.LookRotation(-axisz, -axisy), cameraCenter, cameraForward, cameraOrtho, radius, out s_Corners[7]);

            PhysicsBoundsHandleUtility.DrawCorner(s_Corners[0], new bool3(false, true,  true));
            PhysicsBoundsHandleUtility.DrawCorner(s_Corners[3], new bool3(true,  false, true));
            PhysicsBoundsHandleUtility.DrawCorner(s_Corners[4], new bool3(true,  false, true));
            PhysicsBoundsHandleUtility.DrawCorner(s_Corners[7], new bool3(false, true,  true));

            PhysicsBoundsHandleUtility.DrawCorner(s_Corners[1], new bool3(true,  false, true));
            PhysicsBoundsHandleUtility.DrawCorner(s_Corners[2], new bool3(false, true,  true));
            PhysicsBoundsHandleUtility.DrawCorner(s_Corners[5], new bool3(false, true,  true));
            PhysicsBoundsHandleUtility.DrawCorner(s_Corners[6], new bool3(true,  false, true));

            // Draw the horizon edges between the corners
            for (int upA = 3, upB = 0; upB < 4; upA = upB, upB++)
            {
                int dnA = upA + 4;
                int dnB = upB + 4;

                if (s_Corners[upA].splitAxis[0].z && s_Corners[upB].splitAxis[1].x) Handles.DrawLine(s_Corners[upA].points[0], s_Corners[upB].points[1]);
                if (s_Corners[upA].splitAxis[1].z && s_Corners[upB].splitAxis[0].x) Handles.DrawLine(s_Corners[upA].points[1], s_Corners[upB].points[0]);

                if (s_Corners[dnA].splitAxis[0].x && s_Corners[dnB].splitAxis[1].z) Handles.DrawLine(s_Corners[dnA].points[0], s_Corners[dnB].points[1]);
                if (s_Corners[dnA].splitAxis[1].x && s_Corners[dnB].splitAxis[0].z) Handles.DrawLine(s_Corners[dnA].points[1], s_Corners[dnB].points[0]);

                if (s_Corners[dnA].splitAxis[0].y && s_Corners[upA].splitAxis[1].y) Handles.DrawLine(s_Corners[dnA].points[0], s_Corners[upA].points[1]);
                if (s_Corners[dnA].splitAxis[1].y && s_Corners[upA].splitAxis[0].y) Handles.DrawLine(s_Corners[dnA].points[1], s_Corners[upA].points[0]);
            }
        }
    }
}
