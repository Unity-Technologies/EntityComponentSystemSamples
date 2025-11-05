using System;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    /// <summary>
    /// A structure for storing authoring data for a capsule shape. In contrast to the
    /// CapsuleGeometry struct in the run-time, this structure permits storing stable orientation
    /// values, as well as height values that can be retained when the source data are defined with
    /// respect to a non-uniformly scaled object.
    /// </summary>
    [Serializable]
    public struct CapsuleGeometryAuthoring : IEquatable<CapsuleGeometryAuthoring>
    {
        /// <summary>
        /// The local orientation of the capsule. It is aligned with the forward axis (z) when it is
        /// identity.
        /// </summary>
        public quaternion Orientation { get => m_OrientationEuler; set => m_OrientationEuler.SetValue(value); }
        internal EulerAngles OrientationEuler { get => m_OrientationEuler; set => m_OrientationEuler = value; }
        [SerializeField]
        EulerAngles m_OrientationEuler;

        /// <summary>   The local position offset of the capsule. </summary>
        public float3 Center { get => m_Center; set => m_Center = value; }
        [SerializeField]
        float3 m_Center;

        /// <summary>
        /// The height of the capsule. It may store any value, but will ultimately always be converted
        /// into a value that is at least twice the radius.
        /// </summary>
        public float Height { get => m_Height; set => m_Height = value; }
        [SerializeField]
        float m_Height;

        /// <summary>   The radius of the capsule. </summary>
        ///
        /// <value> The radius. </value>
        public float Radius { get => m_Radius; set => m_Radius = value; }
        [SerializeField]
        float m_Radius;

        public bool Equals(CapsuleGeometryAuthoring other)
        {
            return m_Height.Equals(other.m_Height)
                && m_Center.Equals(other.m_Center)
                && m_Radius.Equals(other.m_Radius)
                && m_OrientationEuler.Equals(other.m_OrientationEuler);
        }

        public override int GetHashCode()
        {
            return unchecked((int)math.hash(
                new float3x3(
                    Center,
                    m_OrientationEuler.Value,
                    new float3((float)m_OrientationEuler.RotationOrder, m_Height, m_Radius)
                )
            ));
        }
    }

    public static class CapsuleGeometryAuthoringExtensions
    {
        /// <summary>
        /// Construct a CapsuleGeometryAuthoring instance from a run-time CapsuleGeometry instance.
        /// </summary>
        public static CapsuleGeometryAuthoring ToAuthoring(this CapsuleGeometry input)
        {
            var orientationEuler = EulerAngles.Default;
            orientationEuler.SetValue(quaternion.LookRotationSafe(input.Vertex0 - input.Vertex1, new float3 { z = 1f }));
            return new CapsuleGeometryAuthoring
            {
                Height = input.GetHeight(),
                OrientationEuler = orientationEuler,
                Center = input.GetCenter(),
                Radius = input.Radius
            };
        }

        /// <summary>
        /// Construct a run-time CapsuleGeometry instance from a CapsuleGeometryAuthoring instance.
        /// </summary>
        public static CapsuleGeometry ToRuntime(this CapsuleGeometryAuthoring input)
        {
            var halfHeight   = 0.5f * input.Height;
            var halfDistance = halfHeight - input.Radius;
            var axis         = math.normalize(math.mul(input.Orientation, new float3 { z = 1f }));
            var halfAxis     = axis * halfDistance;
            var vertex0      = input.Center + halfAxis;
            var vertex1      = input.Center - halfAxis;
            return new CapsuleGeometry
            {
                Vertex0 = vertex0,
                Vertex1 = vertex1,
                Radius  = input.Radius
            };
        }
    }
}
