using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    static partial class PhysicsShapeExtensions
    {
        static void MakeZAxisPrimaryBasis(ref int3 basisPriority)
        {
            if (basisPriority[1] == 2)
                basisPriority = basisPriority.yxz;
            else if (basisPriority[2] == 2)
                basisPriority = basisPriority.zxy;
        }

        #region Box
        [BurstCompile]
        internal struct BakeBoxJob : IJob
        {
            public NativeArray<BoxGeometry> Box;

            // TODO: make members PascalCase after merging static query fixes
            public float4x4 localToWorld;
            public float4x4 shapeToWorld;
            public EulerAngles orientation;

            public static float4x4 GetBakeToShape(float4x4 localToWorld, float4x4 shapeToWorld, ref float3 center,
                ref EulerAngles orientation)
            {
                float4x4 bakeToShape;
                float4x4 rotationMatrix = float4x4.identity;
                var basisPriority = k_DefaultAxisPriority;
                var sheared = localToWorld.HasShear();
                if (localToWorld.HasNonUniformScale() || sheared)
                {
                    if (sheared)
                    {
                        var transformScale = localToWorld.DecomposeScale();
                        var basisToWorld =
                            GetBasisToWorldMatrix(localToWorld, center, orientation, transformScale);
                        basisPriority = GetBasisAxisPriority(basisToWorld);
                    }

                    rotationMatrix = new float4x4(
                        new float4 { [basisPriority[2]] = 1 },
                        new float4 { [basisPriority[1]] = 1 },
                        new float4 { [basisPriority[0]] = 1 },
                        new float4 { [3] = 1 }
                    );
                }

                bakeToShape = GetPrimitiveBakeToShapeMatrix(localToWorld, shapeToWorld, ref center,
                    ref orientation, 1f, basisPriority);

                bakeToShape = math.mul(bakeToShape, rotationMatrix);
                return bakeToShape;
            }

            public void Execute()
            {
                var center = Box[0].Center;
                var size = Box[0].Size;
                var bevelRadius = Box[0].BevelRadius;

                var bakeToShape = GetBakeToShape(localToWorld, shapeToWorld, ref center, ref orientation);
                bakeToShape = math.mul(bakeToShape, float4x4.Scale(size));

                var scale = bakeToShape.DecomposeScale();

                size = scale;

                Box[0] = new BoxGeometry
                {
                    Center = center,
                    Orientation = orientation,
                    Size = size,
                    BevelRadius = math.clamp(bevelRadius, 0f, 0.5f * math.cmin(size))
                };
            }
        }
        #endregion

        #region Capsule
        [BurstCompile]
        internal struct BakeCapsuleJob : IJob
        {
            public NativeArray<CapsuleGeometryAuthoring> Capsule;

            // TODO: make members PascalCase after merging static query fixes
            public float4x4 localToWorld;
            public float4x4 shapeToWorld;

            public static float4x4 GetBakeToShape(float4x4 localToWorld, float4x4 shapeToWorld, ref float3 center,
                ref EulerAngles orientation)
            {
                var basisPriority = k_DefaultAxisPriority;
                var sheared = localToWorld.HasShear();
                if (localToWorld.HasNonUniformScale() || sheared)
                {
                    if (sheared)
                    {
                        var transformScale = localToWorld.DecomposeScale();
                        var basisToWorld = GetBasisToWorldMatrix(localToWorld, center, orientation, transformScale);
                        basisPriority = GetBasisAxisPriority(basisToWorld);
                    }

                    MakeZAxisPrimaryBasis(ref basisPriority);
                }

                return GetPrimitiveBakeToShapeMatrix(localToWorld, shapeToWorld, ref center, ref orientation, 1f,
                    basisPriority);
            }

            public void Execute()
            {
                var radius = Capsule[0].Radius;
                var center = Capsule[0].Center;
                var height = Capsule[0].Height;
                var orientationEuler = Capsule[0].OrientationEuler;

                var bakeToShape = GetBakeToShape(localToWorld, shapeToWorld, ref center, ref orientationEuler);
                var scale = bakeToShape.DecomposeScale();

                radius *= math.cmax(scale.xy);
                height = math.max(0, height * scale.z);

                Capsule[0] = new CapsuleGeometryAuthoring
                {
                    OrientationEuler = orientationEuler,
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

            // TODO: make members PascalCase after merging static query fixes
            public float4x4 localToWorld;
            public float4x4 shapeToWorld;
            public EulerAngles orientation;

            public static float4x4 GetBakeToShape(float4x4 localToWorld, float4x4 shapeToWorld, ref float3 center,
                ref EulerAngles orientation)
            {
                var basisPriority = k_DefaultAxisPriority;
                var sheared = localToWorld.HasShear();
                if (localToWorld.HasNonUniformScale() || sheared)
                {
                    if (sheared)
                    {
                        var transformScale = localToWorld.DecomposeScale();
                        var basisToWorld = GetBasisToWorldMatrix(localToWorld, center, orientation, transformScale);
                        basisPriority = GetBasisAxisPriority(basisToWorld);
                    }

                    MakeZAxisPrimaryBasis(ref basisPriority);
                }

                return GetPrimitiveBakeToShapeMatrix(localToWorld, shapeToWorld, ref center, ref orientation, 1f,
                    basisPriority);
            }

            public void Execute()
            {
                var center = Cylinder[0].Center;
                var height = Cylinder[0].Height;
                var radius = Cylinder[0].Radius;
                var bevelRadius = Cylinder[0].BevelRadius;

                var bakeToShape = GetBakeToShape(localToWorld, shapeToWorld, ref center, ref orientation);
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
            this CylinderGeometry cylinder, float4x4 localToWorld, float4x4 shapeToWorld, EulerAngles orientation
        )
        {
            using (var geometry = new NativeArray<CylinderGeometry>(1, Allocator.TempJob) { [0] = cylinder })
            {
                var job = new BakeCylinderJob
                {
                    Cylinder = geometry,
                    localToWorld = localToWorld,
                    shapeToWorld = shapeToWorld,
                    orientation = orientation
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
            // TODO: make members PascalCase after merging static query fixes
            public float4x4 localToWorld;
            public float4x4 shapeToWorld;

            public void Execute()
            {
                var center = Sphere[0].Center;
                var radius = Sphere[0].Radius;
                var orientation = Orientation[0];

                var basisToWorld = GetBasisToWorldMatrix(localToWorld, center, orientation, 1f);
                var basisPriority = basisToWorld.HasShear() ? GetBasisAxisPriority(basisToWorld) : k_DefaultAxisPriority;
                var bakeToShape = GetPrimitiveBakeToShapeMatrix(localToWorld, shapeToWorld, ref center, ref orientation, 1f, basisPriority);

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
            this SphereGeometry sphere, float4x4 localToWorld, float4x4 shapeToWorld, ref EulerAngles orientation
        )
        {
            using (var geometry = new NativeArray<SphereGeometry>(1, Allocator.TempJob) { [0] = sphere })
            using (var outOrientation = new NativeArray<EulerAngles>(1, Allocator.TempJob) { [0] = orientation })
            {
                var job = new BakeSphereJob
                {
                    Sphere = geometry,
                    Orientation = outOrientation,
                    localToWorld = localToWorld,
                    shapeToWorld = shapeToWorld
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
            // TODO: make members PascalCase after merging static query fixes
            public float3 center;
            public float2 size;
            public EulerAngles orientation;
            public float4x4 localToWorld;
            public float4x4 shapeToWorld;

            public void Execute()
            {
                var v = Vertices[0];
                GetPlanePoints(center, size, orientation, out v.c0, out v.c1, out v.c2, out v.c3);
                var localToShape = math.mul(math.inverse(shapeToWorld), localToWorld);
                v.c0 = math.mul(localToShape, new float4(v.c0, 1f)).xyz;
                v.c1 = math.mul(localToShape, new float4(v.c1, 1f)).xyz;
                v.c2 = math.mul(localToShape, new float4(v.c2, 1f)).xyz;
                v.c3 = math.mul(localToShape, new float4(v.c3, 1f)).xyz;
                Vertices[0] = v;
            }
        }

        internal static void BakeToBodySpace(
            float3 center, float2 size, EulerAngles orientation, float4x4 localToWorld, float4x4 shapeToWorld,
            out float3 vertex0, out float3 vertex1, out float3 vertex2, out float3 vertex3
        )
        {
            using (var geometry = new NativeArray<float3x4>(1, Allocator.TempJob))
            {
                var job = new BakePlaneJob
                {
                    Vertices = geometry,
                    center = center,
                    size = size,
                    orientation = orientation,
                    localToWorld = localToWorld,
                    shapeToWorld = shapeToWorld
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


        #region AABB
        [BurstCompile]
        internal struct GetAabbJob : IJob
        {
            [ReadOnly] public NativeArray<float3> Points;
            public NativeArray<Aabb> Aabb;

            public void Execute()
            {
                var aabb = new Aabb { Min = float.MaxValue, Max = float.MinValue };
                for (var i = 0; i < Points.Length; ++i)
                    aabb.Include(Points[i]);
                Aabb[0] = aabb;
            }
        }
        #endregion
    }
}
