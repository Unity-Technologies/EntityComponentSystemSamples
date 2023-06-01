using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Physics.Editor
{
    class PhysicsSphereBoundsHandle : SphereBoundsHandle
    {
        protected override void DrawWireframe()
        {
            bool x = IsAxisEnabled(Axes.X);
            bool y = IsAxisEnabled(Axes.Y);
            bool z = IsAxisEnabled(Axes.Z);

            if (x && !y && !z)
                Handles.DrawLine(Vector3.right * radius, Vector3.left * radius);
            if (!x && y && !z)
                Handles.DrawLine(Vector3.up * radius, Vector3.down * radius);
            if (!x && !y && z)
                Handles.DrawLine(Vector3.forward * radius, Vector3.back * radius);

            const float kEpsilon = 0.000001F;

            if (radius > 0)
            {
                var frontfacedColor = Handles.color;
                var backfacedColor  = Handles.color * new Color(1f, 1f, 1f, PhysicsBoundsHandleUtility.kBackfaceAlphaMultiplier);
                var discVisible = new bool[]
                {
                    y && z,
                    x && z,
                    x && y
                };
                var discOrientations = new float3[]
                {
                    Vector3.right,
                    Vector3.up,
                    Vector3.forward
                };

                // Since the geometry is transformed by Handles.matrix during rendering, we transform the camera position
                // by the inverse matrix so that the two-shaded wireframe will have the proper orientation.
                var invMatrix               = Handles.inverseMatrix;

                var cameraCenter            = Camera.current == null ? Vector3.zero : Camera.current.transform.position;
                var cameraToCenter          = center - invMatrix.MultiplyPoint(cameraCenter); // vector from camera to center
                var sqrDistCameraToCenter   = cameraToCenter.sqrMagnitude;
                var sqrRadius               = radius * radius;                  // squared radius
                var isCameraOrthographic    = Camera.current == null || Camera.current.orthographic;
                var sqrOffset               = isCameraOrthographic ? 0 : (sqrRadius * sqrRadius / sqrDistCameraToCenter);   // squared distance from actual center to drawn disc center
                var insideAmount            = sqrOffset / sqrRadius;
                if (insideAmount < 1)
                {
                    if (math.abs(sqrDistCameraToCenter) >= kEpsilon)
                    {
                        using (new Handles.DrawingScope(frontfacedColor))
                        {
                            if (isCameraOrthographic)
                            {
                                var horizonRadius = radius;
                                var horizonCenter = center;
                                Handles.DrawWireDisc(horizonCenter, cameraToCenter, horizonRadius);
                            }
                            else
                            {
                                var horizonRadius = math.sqrt(sqrRadius - sqrOffset);
                                var horizonCenter = center - sqrRadius * cameraToCenter / sqrDistCameraToCenter;
                                Handles.DrawWireDisc(horizonCenter, cameraToCenter, horizonRadius);
                            }
                        }

                        var planeNormal = cameraToCenter.normalized;
                        for (int i = 0; i < 3; i++)
                        {
                            if (!discVisible[i])
                                continue;

                            var discOrientation = discOrientations[i];

                            var angleBetweenDiscAndNormal = math.acos(math.dot(discOrientation, planeNormal));
                            angleBetweenDiscAndNormal = (math.PI * 0.5f) - math.min(angleBetweenDiscAndNormal, math.PI - angleBetweenDiscAndNormal);

                            float f = math.tan(angleBetweenDiscAndNormal);
                            float g = math.sqrt(sqrOffset + f * f * sqrOffset) / radius;
                            if (g < 1)
                            {
                                var angleToHorizon          = math.degrees(math.asin(g));
                                var discTangent             = math.cross(discOrientation, planeNormal);
                                var vectorToPointOnHorizon  = Quaternion.AngleAxis(angleToHorizon, discOrientation) * discTangent;
                                var horizonArcLength        = (90 - angleToHorizon) * 2.0f;

                                using (new Handles.DrawingScope(frontfacedColor))
                                    Handles.DrawWireArc(center, discOrientation, vectorToPointOnHorizon, horizonArcLength, radius);
                                using (new Handles.DrawingScope(backfacedColor))
                                    Handles.DrawWireArc(center, discOrientation, vectorToPointOnHorizon, horizonArcLength - 360, radius);
                            }
                            else
                            {
                                using (new Handles.DrawingScope(backfacedColor))
                                    Handles.DrawWireDisc(center, discOrientation, radius);
                            }
                        }
                    }
                }
                else
                {
                    using (new Handles.DrawingScope(backfacedColor))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var discOrientation = discOrientations[i];
                            Handles.DrawWireDisc(center, discOrientation, radius);
                        }
                    }
                }
            }
        }
    }
}
