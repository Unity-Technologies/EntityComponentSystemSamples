using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Physics.Authoring
{
    static partial class PhysicsShapeExtensions
    {
        public static class BakeBoxJobExtension
        {
            internal static float4x4 GetBakeToShape(PhysicsShapeAuthoring shape, float3 center, EulerAngles orientation)
            {
                var transform = shape.transform;
                var localToWorld = (float4x4)transform.localToWorldMatrix;
                var shapeToWorld = shape.GetShapeToWorldMatrix();
                quaternion tmpOri = orientation;
                return Math.GetBakeToShape(localToWorld, shapeToWorld, ref center,
                    ref tmpOri);
            }
        }

        public static class BakeCapsuleJobExtension
        {
            internal static float4x4 GetBakeToShape(PhysicsShapeAuthoring shape, float3 center, EulerAngles orientation)
            {
                var transform = shape.transform;
                var localToWorld = (float4x4)transform.localToWorldMatrix;
                var shapeToWorld = shape.GetShapeToWorldMatrix();
                quaternion tmpOri = orientation;
                return Math.GetBakeToShape(localToWorld, shapeToWorld, ref center,
                    ref tmpOri, bakeUniformScale: true, makeZAxisPrimaryBasis: true);
            }
        }

        public static void SetBakedCapsuleSize(this PhysicsShapeAuthoring shape, float height, float radius)
        {
            var capsule = shape.GetCapsuleProperties();
            var center = capsule.Center;

            var bakeToShape = BakeCapsuleJobExtension.GetBakeToShape(shape, center, capsule.OrientationEuler);
            var scale = bakeToShape.DecomposeScale();

            var newRadius = radius / math.cmax(scale.xy);
            if (math.abs(capsule.Radius - newRadius) > kMinimumChange)
                capsule.Radius = newRadius;

            height /= scale.z;

            if (math.abs(math.length(capsule.Height - height)) > kMinimumChange)
                capsule.Height = height;

            shape.SetCapsule(capsule);
        }

        internal static CapsuleGeometryAuthoring BakeToBodySpace(
            this CapsuleGeometryAuthoring capsule, float4x4 localToWorld, float4x4 shapeToWorld, bool bakeUniformScale = true
        )
        {
            using (var geometry = new NativeArray<CapsuleGeometryAuthoring>(1, Allocator.TempJob) { [0] = capsule })
            {
                var job = new BakeCapsuleJob
                {
                    Capsule = geometry,
                    LocalToWorld = localToWorld,
                    ShapeToWorld = shapeToWorld,
                    BakeUniformScale = bakeUniformScale
                };
                job.Run();
                return geometry[0];
            }
        }

        public static class BakeCylinderJobExtension
        {
            internal static float4x4 GetBakeToShape(PhysicsShapeAuthoring shape, float3 center, EulerAngles orientation)
            {
                var transform = shape.transform;
                var localToWorld = (float4x4)transform.localToWorldMatrix;
                var shapeToWorld = shape.GetShapeToWorldMatrix();
                quaternion tmpOri = orientation;
                return Math.GetBakeToShape(localToWorld, shapeToWorld, ref center,
                    ref tmpOri, bakeUniformScale: true, makeZAxisPrimaryBasis: true);
            }
        }

        public static CylinderGeometry GetBakedCylinderProperties(this PhysicsShapeAuthoring shape)
        {
            var cylinder = shape.GetCylinderProperties(out var orientation);
            return cylinder.BakeToBodySpace(shape.transform.localToWorldMatrix, shape.GetShapeToWorldMatrix(),
                orientation);
        }

        public static void SetBakedSphereRadius(this PhysicsShapeAuthoring shape, float radius)
        {
            var sphere = shape.GetSphereProperties(out EulerAngles eulerAngles);
            quaternion orientation = eulerAngles;
            var center = sphere.Center;
            radius = math.abs(radius);

            var basisToWorld    = Math.GetBasisToWorldMatrix(shape.transform.localToWorldMatrix, center, orientation, 1f);
            var basisPriority   = basisToWorld.HasShear() ? Math.GetBasisAxisPriority(basisToWorld) : Math.Constants.DefaultAxisPriority;
            var bakeToShape     = Math.GetPrimitiveBakeToShapeMatrix(shape.transform.localToWorldMatrix, shape.GetShapeToWorldMatrix(), ref center, ref orientation, basisPriority);

            var scale = math.cmax(bakeToShape.DecomposeScale());

            var newRadius = radius / scale;
            sphere.Radius = newRadius;
            shape.SetSphere(sphere);
        }

        public static void SetBakedPlaneSize(this PhysicsShapeAuthoring shape, float2 size)
        {
            shape.GetPlaneProperties(out var center, out var planeSize, out EulerAngles orientation);

            var prevSize = math.abs(planeSize);
            size = math.abs(size);

            if (math.abs(size[0] - prevSize[0]) < kMinimumChange) size[0] = prevSize[0];
            if (math.abs(size[1] - prevSize[1]) < kMinimumChange) size[1] = prevSize[1];

            planeSize = size;

            shape.SetPlane(center, planeSize, orientation);
        }
    }
}
