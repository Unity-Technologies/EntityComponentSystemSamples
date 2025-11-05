using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    // put static UnityObject buffers in separate utility class so other methods can Burst compile
    static class PhysicsShapeExtensions_NonBursted
    {
        internal static readonly List<PhysicsBodyAuthoring> s_PhysicsBodiesBuffer = new List<PhysicsBodyAuthoring>(16);
        internal static readonly List<PhysicsShapeAuthoring> s_ShapesBuffer = new List<PhysicsShapeAuthoring>(16);
        internal static readonly List<Rigidbody> s_RigidbodiesBuffer = new List<Rigidbody>(16);
        internal static readonly List<UnityEngine.Collider> s_CollidersBuffer = new List<UnityEngine.Collider>(16);
    }

    public static partial class PhysicsShapeExtensions
    {
        // avoids drift in axes we're not actually changing
        public const float kMinimumChange = HashableShapeInputs.k_DefaultLinearPrecision;

        internal static CollisionFilter GetFilter(this PhysicsShapeAuthoring shape)
        {
            // TODO: determine optimal workflow for specifying group index
            return new CollisionFilter
            {
                BelongsTo = shape.BelongsTo.Value,
                CollidesWith = shape.CollidesWith.Value
            };
        }

        internal static Material GetMaterial(this PhysicsShapeAuthoring shape)
        {
            // TODO: TBD how we will author editor content for other shape flags
            return new Material
            {
                Friction = shape.Friction.Value,
                FrictionCombinePolicy = shape.Friction.CombineMode,
                Restitution = shape.Restitution.Value,
                RestitutionCombinePolicy = shape.Restitution.CombineMode,
                CollisionResponse = shape.CollisionResponse,
                CustomTags = shape.CustomTags.Value,
                EnableDetailedStaticMeshCollision = shape.DetailedStaticMeshCollision.Value
            };
        }

        public static GameObject FindTopmostEnabledAncestor<T>(GameObject shape, List<T> buffer) where T : Component
        {
            // include inactive in case the supplied shape GameObject is a prefab that has not been instantiated
            shape.GetComponentsInParent(true, buffer);
            GameObject result = null;
            for (var i = buffer.Count - 1; i >= 0; --i)
            {
                if (
                    (buffer[i] as UnityEngine.Collider)?.enabled ??
                    (buffer[i] as MonoBehaviour)?.enabled ?? true
                )
                {
                    result = buffer[i].gameObject;
                    break;
                }
            }
            buffer.Clear();
            return result;
        }

        public static GameObject GetPrimaryBody(this PhysicsShapeAuthoring shape) => GetPrimaryBody(shape.gameObject);

        public static GameObject GetPrimaryBody(GameObject shape)
        {
            var pb = ColliderExtensions.FindFirstEnabledAncestor(shape, PhysicsShapeExtensions_NonBursted.s_PhysicsBodiesBuffer);
            var rb = ColliderExtensions.FindFirstEnabledAncestor(shape, PhysicsShapeExtensions_NonBursted.s_RigidbodiesBuffer);

            if (pb != null)
            {
                return rb == null ? pb.gameObject :
                    pb.transform.IsChildOf(rb.transform) ? pb.gameObject : rb.gameObject;
            }

            if (rb != null)
                return rb.gameObject;

            // for implicit static shape, first see if it is part of static optimized hierarchy
            ColliderExtensions.FindTopmostStaticEnabledAncestor(shape, out GameObject topStatic);
            if (topStatic != null)
                return topStatic;

            // otherwise, find topmost enabled Collider or PhysicsShapeAuthoring
            var topCollider = FindTopmostEnabledAncestor(shape, PhysicsShapeExtensions_NonBursted.s_CollidersBuffer);
            var topShape = FindTopmostEnabledAncestor(shape, PhysicsShapeExtensions_NonBursted.s_ShapesBuffer);

            return topCollider == null
                ? topShape == null ? shape.gameObject : topShape
                : topShape == null
                ? topCollider
                : topShape.transform.IsChildOf(topCollider.transform)
                ? topCollider
                : topShape;
        }

        public static BoxGeometry GetBakedBoxProperties(this PhysicsShapeAuthoring shape)
        {
            var box = shape.GetBoxProperties(out var orientation);
            return box.BakeToBodySpace(shape.transform.localToWorldMatrix, shape.GetShapeToWorldMatrix(), orientation);
        }

        internal static BoxGeometry BakeToBodySpace(
            this BoxGeometry box, float4x4 localToWorld, float4x4 shapeToWorld, EulerAngles orientation, bool bakeUniformScale = true
        )
        {
            using (var geometry = new NativeArray<BoxGeometry>(1, Allocator.TempJob) { [0] = box })
            {
                var job = new BakeBoxJob
                {
                    Box = geometry,
                    LocalToWorld = localToWorld,
                    ShapeToWorld = shapeToWorld,
                    Orientation = orientation,
                    BakeUniformScale = bakeUniformScale
                };
                job.Run();
                return geometry[0];
            }
        }

        public static void SetBakedBoxSize(this PhysicsShapeAuthoring shape, float3 size, float bevelRadius)
        {
            var box         = shape.GetBoxProperties(out var orientation);
            var center      = box.Center;
            var prevSize    = math.abs(box.Size);
            size = math.abs(size);

            var bakeToShape = BakeBoxJobExtension.GetBakeToShape(shape, center, orientation);
            var scale = bakeToShape.DecomposeScale();

            size /= scale;

            if (math.abs(size[0] - prevSize[0]) < kMinimumChange) size[0] = prevSize[0];
            if (math.abs(size[1] - prevSize[1]) < kMinimumChange) size[1] = prevSize[1];
            if (math.abs(size[2] - prevSize[2]) < kMinimumChange) size[2] = prevSize[2];

            box.BevelRadius = bevelRadius;
            box.Size = size;

            shape.SetBox(box, orientation);
        }

        internal static CapsuleGeometryAuthoring GetBakedCapsuleProperties(this PhysicsShapeAuthoring shape)
        {
            var capsule = shape.GetCapsuleProperties();
            return capsule.BakeToBodySpace(shape.transform.localToWorldMatrix, shape.GetShapeToWorldMatrix());
        }

        public static void SetBakedCylinderSize(this PhysicsShapeAuthoring shape, float height, float radius, float bevelRadius)
        {
            var cylinder    = shape.GetCylinderProperties(out EulerAngles orientation);
            var center      = cylinder.Center;

            var bakeToShape = BakeCylinderJobExtension.GetBakeToShape(shape, center, orientation);
            var scale = bakeToShape.DecomposeScale();

            var newRadius = radius / math.cmax(scale.xy);
            if (math.abs(cylinder.Radius - newRadius) > kMinimumChange) cylinder.Radius = newRadius;
            if (math.abs(cylinder.BevelRadius - bevelRadius) > kMinimumChange) cylinder.BevelRadius = bevelRadius;


            var newHeight = math.max(0, height / scale.z);
            if (math.abs(cylinder.Height - newHeight) > kMinimumChange) cylinder.Height = newHeight;
            shape.SetCylinder(cylinder, orientation);
        }

        internal static SphereGeometry GetBakedSphereProperties(this PhysicsShapeAuthoring shape, out EulerAngles orientation)
        {
            var sphere = shape.GetSphereProperties(out orientation);
            return sphere.BakeToBodySpace(shape.transform.localToWorldMatrix, shape.GetShapeToWorldMatrix(), ref orientation);
        }

        internal static void GetBakedPlaneProperties(
            this PhysicsShapeAuthoring shape, out float3 vertex0, out float3 vertex1, out float3 vertex2, out float3 vertex3
        )
        {
            var bakeToShape = shape.GetLocalToShapeMatrix();
            shape.GetPlaneProperties(out var center, out var size, out EulerAngles orientation);
            BakeToBodySpace(
                center, size, orientation, bakeToShape,
                out vertex0, out vertex1, out vertex2, out vertex3
            );
        }

        public static void GetBakedConvexProperties(this PhysicsShapeAuthoring shape, NativeList<float3> pointCloud)
        {
            shape.GetConvexHullProperties(pointCloud, true, default, default, default, default);
            shape.BakePoints(pointCloud.AsArray());
        }

        public static void GetBakedMeshProperties(
            this PhysicsShapeAuthoring shape, NativeList<float3> vertices, NativeList<int3> triangles,
            HashSet<UnityEngine.Mesh> meshAssets = null
        )
        {
            shape.GetMeshProperties(vertices, triangles, true, default, meshAssets);
            shape.BakePoints(vertices.AsArray());
        }

        const float k_HashFloatTolerance = 0.01f;

        // used to hash convex hull generation properties in a way that is robust to imprecision
        public static uint GetStableHash(
            this ConvexHullGenerationParameters generationParameters,
            ConvexHullGenerationParameters hashedParameters,
            float tolerance = k_HashFloatTolerance
        )
        {
            var differences = new float3(
                generationParameters.BevelRadius - hashedParameters.BevelRadius,
                generationParameters.MinimumAngle - hashedParameters.MinimumAngle,
                generationParameters.SimplificationTolerance - hashedParameters.SimplificationTolerance
            );
            return math.cmax(math.abs(differences)) < tolerance
                ? unchecked((uint)hashedParameters.GetHashCode())
                : unchecked((uint)generationParameters.GetHashCode());
        }

        // used to hash an array of points in a way that is robust to imprecision
        public static unsafe uint GetStableHash(
            this NativeList<float3> points, NativeArray<float3> hashedPoints, float tolerance = k_HashFloatTolerance
        )
        {
            if (points.Length != hashedPoints.Length)
                return math.hash(points.GetUnsafePtr(), UnsafeUtility.SizeOf<float3>() * points.Length);

            for (int i = 0, count = points.Length; i < count; ++i)
            {
                if (math.cmax(math.abs(points[i] - hashedPoints[i])) > tolerance)
                    return math.hash(points.GetUnsafePtr(), UnsafeUtility.SizeOf<float3>() * points.Length);
            }
            return math.hash(hashedPoints.GetUnsafePtr(), UnsafeUtility.SizeOf<float3>() * hashedPoints.Length);
        }

        public static int GetMaxAxis(this float3 v)
        {
            var cmax = math.cmax(v);
            return cmax == v.z ? 2 : cmax == v.y ? 1 : 0;
        }

        public static int GetDeviantAxis(this float3 v)
        {
            var deviation = math.abs(v - math.csum(v) / 3f);
            return math.cmax(deviation) == deviation.z ? 2 : math.cmax(deviation) == deviation.y ? 1 : 0;
        }
    }
}
