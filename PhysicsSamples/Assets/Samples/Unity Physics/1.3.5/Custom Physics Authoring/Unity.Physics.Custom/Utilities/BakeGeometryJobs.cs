using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    static partial class PhysicsShapeExtensions
    {
        #region Box
        [BurstCompile]
        internal struct BakeBoxJob : IJob
        {
            public NativeArray<BoxGeometry> Box;

            public float4x4 LocalToWorld;
            public float4x4 ShapeToWorld;
            public EulerAngles Orientation;
            public bool BakeUniformScale;

            public void Execute()
            {
                var center = Box[0].Center;
                var size = Box[0].Size;
                var bevelRadius = Box[0].BevelRadius;
                quaternion orientation = Orientation;

                var bakeToShape = Math.GetBakeToShape(LocalToWorld, ShapeToWorld, ref center, ref orientation, BakeUniformScale);
                bakeToShape = math.mul(bakeToShape, float4x4.Scale(size));

                var scale = bakeToShape.DecomposeScale();

                size = scale;

                Box[0] = new BoxGeometry
                {
                    Center = center,
                    Orientation = orientation,
                    Size = size,
                    BevelRadius = math.clamp(bevelRadius, 0f, 0.5f * math.cmin(size)),
                };
            }
        }
        #endregion

        #region Capsule
        [BurstCompile]
        internal struct BakeCapsuleJob : IJob
        {
            public NativeArray<CapsuleGeometryAuthoring> Capsule;

            public float4x4 LocalToWorld;
            public float4x4 ShapeToWorld;
            public bool BakeUniformScale;

            public void Execute()
            {
                var radius = Capsule[0].Radius;
                var center = Capsule[0].Center;
                var height = Capsule[0].Height;
                quaternion orientation = Capsule[0].OrientationEuler;

                var bakeToShape = Math.GetBakeToShape(LocalToWorld, ShapeToWorld, ref center, ref orientation, BakeUniformScale, makeZAxisPrimaryBasis: true);
                var scale = bakeToShape.DecomposeScale();

                radius *= math.cmax(scale.xy);
                height = math.max(0, height * scale.z);

                Capsule[0] = new CapsuleGeometryAuthoring
                {
                    OrientationEuler = orientation,
                    Center = center,
                    Height = height,
                    Radius = radius
                };
            }
        }

        #endregion

        #region Cylinder
        [BurstCompile]
        internal struct BakeCylinderJob : IJob
        {
            public NativeArray<CylinderGeometry> Cylinder;

            public float4x4 LocalToWorld;
            public float4x4 ShapeToWorld;
            public EulerAngles Orientation;
            public bool BakeUniformScale;

            public void Execute()
            {
                var center = Cylinder[0].Center;
                var height = Cylinder[0].Height;
                var radius = Cylinder[0].Radius;
                var bevelRadius = Cylinder[0].BevelRadius;
                quaternion orientation = Orientation;

                var bakeToShape = Math.GetBakeToShape(LocalToWorld, ShapeToWorld, ref center, ref orientation, BakeUniformScale, makeZAxisPrimaryBasis: true);
                var scale = bakeToShape.DecomposeScale();

                height *= scale.z;
                radius *= math.cmax(scale.xy);

                Cylinder[0] = new CylinderGeometry
                {
                    Center = center,
                    Orientation = orientation,
                    Height = height,
                    Radius = radius,
                    BevelRadius = math.min(bevelRadius, math.min(height * 0.5f, radius)),
                    SideCount = Cylinder[0].SideCount
                };
            }
        }

        internal static CylinderGeometry BakeToBodySpace(
            this CylinderGeometry cylinder, float4x4 localToWorld, float4x4 shapeToWorld, EulerAngles orientation, bool bakeUniformScale = true
        )
        {
            using (var geometry = new NativeArray<CylinderGeometry>(1, Allocator.TempJob) { [0] = cylinder })
            {
                var job = new BakeCylinderJob
                {
                    Cylinder = geometry,
                    LocalToWorld = localToWorld,
                    ShapeToWorld = shapeToWorld,
                    Orientation = orientation,
                    BakeUniformScale = bakeUniformScale
                };
                job.Run();
                return geometry[0];
            }
        }

        #endregion


        #region Sphere
        [BurstCompile]
        struct BakeSphereJob : IJob
        {
            public NativeArray<SphereGeometry> Sphere;
            public NativeArray<EulerAngles> Orientation;
            public float4x4 LocalToWorld;
            public float4x4 ShapeToWorld;
            public bool BakeUniformScale;

            public void Execute()
            {
                var center = Sphere[0].Center;
                var radius = Sphere[0].Radius;
                quaternion orientation = Orientation[0];

                var basisToWorld = Math.GetBasisToWorldMatrix(LocalToWorld, center, orientation, 1f);
                var basisPriority = basisToWorld.HasShear() ? Math.GetBasisAxisPriority(basisToWorld) : Math.Constants.DefaultAxisPriority;
                var bakeToShape = Math.GetPrimitiveBakeToShapeMatrix(LocalToWorld, ShapeToWorld, ref center, ref orientation, basisPriority, BakeUniformScale);

                radius *= math.cmax(bakeToShape.DecomposeScale());

                Sphere[0] = new SphereGeometry
                {
                    Center = center,
                    Radius = radius
                };
                Orientation[0] = orientation;
            }
        }

        internal static SphereGeometry BakeToBodySpace(
            this SphereGeometry sphere, float4x4 localToWorld, float4x4 shapeToWorld, ref EulerAngles orientation, bool bakeUniformScale = true
        )
        {
            using (var geometry = new NativeArray<SphereGeometry>(1, Allocator.TempJob) { [0] = sphere })
            using (var outOrientation = new NativeArray<EulerAngles>(1, Allocator.TempJob) { [0] = orientation })
            {
                var job = new BakeSphereJob
                {
                    Sphere = geometry,
                    Orientation = outOrientation,
                    LocalToWorld = localToWorld,
                    ShapeToWorld = shapeToWorld,
                    BakeUniformScale = bakeUniformScale
                };
                job.Run();
                orientation = outOrientation[0];
                return geometry[0];
            }
        }

        #endregion

        #region Plane

        [BurstCompile]
        struct BakePlaneJob : IJob
        {
            public NativeArray<float3x4> Vertices;
            public float3 Center;
            public float2 Size;
            public EulerAngles Orientation;
            public float4x4 BakeFromShape;

            public void Execute()
            {
                var v = Vertices[0];
                GetPlanePoints(Center, Size, Orientation, out v.c0, out v.c1, out v.c2, out v.c3);
                v.c0 = math.mul(BakeFromShape, new float4(v.c0, 1f)).xyz;
                v.c1 = math.mul(BakeFromShape, new float4(v.c1, 1f)).xyz;
                v.c2 = math.mul(BakeFromShape, new float4(v.c2, 1f)).xyz;
                v.c3 = math.mul(BakeFromShape, new float4(v.c3, 1f)).xyz;
                Vertices[0] = v;
            }
        }

        internal static void BakeToBodySpace(
            float3 center, float2 size, EulerAngles orientation, float4x4 bakeFromShape,
            out float3 vertex0, out float3 vertex1, out float3 vertex2, out float3 vertex3
        )
        {
            using (var geometry = new NativeArray<float3x4>(1, Allocator.TempJob))
            {
                var job = new BakePlaneJob
                {
                    Vertices = geometry,
                    Center = center,
                    Size = size,
                    Orientation = orientation,
                    BakeFromShape = bakeFromShape
                };
                job.Run();
                vertex0 = geometry[0].c0;
                vertex1 = geometry[0].c1;
                vertex2 = geometry[0].c2;
                vertex3 = geometry[0].c3;
            }
        }

        internal static void GetPlanePoints(
            float3 center, float2 size, EulerAngles orientation,
            out float3 vertex0, out float3 vertex1, out float3 vertex2, out float3 vertex3
        )
        {
            var sizeYUp = math.float3(size.x, 0, size.y);

            vertex0 = center + math.mul(orientation, sizeYUp * math.float3(-0.5f, 0,  0.5f));
            vertex1 = center + math.mul(orientation, sizeYUp * math.float3(0.5f, 0,  0.5f));
            vertex2 = center + math.mul(orientation, sizeYUp * math.float3(0.5f, 0, -0.5f));
            vertex3 = center + math.mul(orientation, sizeYUp * math.float3(-0.5f, 0, -0.5f));
        }

        #endregion

        #region ShapeInputHash
#if !(UNITY_ANDROID && !UNITY_64) // !Android32
        // Getting memory alignment errors from HashUtility.Hash128 on Android32
        [BurstCompile]
#endif
        internal struct GetShapeInputsHashJob : IJob
        {
            public NativeArray<Hash128> Result;

            public uint ForceUniqueIdentifier;
            public ConvexHullGenerationParameters GenerationParameters;
            public Material Material;
            public CollisionFilter CollisionFilter;
            public float4x4 BakeFromShape;

            [ReadOnly] public NativeArray<HashableShapeInputs> Inputs;
            [ReadOnly] public NativeArray<int> AllSkinIndices;
            [ReadOnly] public NativeArray<float> AllBlendShapeWeights;

            public void Execute()
            {
                Result[0] = HashableShapeInputs.GetHash128(
                    ForceUniqueIdentifier, GenerationParameters, Material, CollisionFilter, BakeFromShape,
                    Inputs, AllSkinIndices, AllBlendShapeWeights
                );
            }
        }
        #endregion
    }
}
