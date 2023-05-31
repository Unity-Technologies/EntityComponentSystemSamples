using Unity.Collections;
using Unity.Deformations;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(DeformationsInPresentation))]
partial class CalculateSkinMatrixSystemBase : SystemBase
{
    EntityQuery m_BoneEntityQuery;
    EntityQuery m_RootEntityQuery;

    protected override void OnCreate()
    {
        m_BoneEntityQuery = GetEntityQuery(
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<BoneTag>()
            );

        m_RootEntityQuery = GetEntityQuery(
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<RootTag>()
            );
    }

    protected override void OnUpdate()
    {
        var boneCount = m_BoneEntityQuery.CalculateEntityCount();
        var bonesLocalToWorld = new NativeParallelHashMap<Entity, float4x4>(boneCount, Allocator.TempJob);
        var bonesLocalToWorldParallel = bonesLocalToWorld.AsParallelWriter();

        var dependency = Dependency;

        var bone = Entities
            .WithName("GatherBoneTransforms")
            .WithAll<BoneTag>()
            .ForEach((Entity entity, in LocalToWorld localToWorld) =>
        {
            bonesLocalToWorldParallel.TryAdd(entity, localToWorld.Value);
        }).ScheduleParallel(dependency);

        var rootCount = m_RootEntityQuery.CalculateEntityCount();
        var rootWorldToLocal = new NativeParallelHashMap<Entity, float4x4>(rootCount, Allocator.TempJob);
        var rootWorldToLocalParallel = rootWorldToLocal.AsParallelWriter();

        var root = Entities
            .WithName("GatherRootTransforms")
            .WithAll<RootTag>()
            .ForEach((Entity entity, in LocalToWorld localToWorld) =>
        {
            rootWorldToLocalParallel.TryAdd(entity, math.inverse(localToWorld.Value));
        }).ScheduleParallel(dependency);

        dependency = JobHandle.CombineDependencies(bone, root);

        dependency = Entities
            .WithName("CalculateSkinMatrices")
            .WithReadOnly(bonesLocalToWorld)
            .WithReadOnly(rootWorldToLocal)
            .ForEach((ref DynamicBuffer<SkinMatrix> skinMatrices, in DynamicBuffer<BindPose> bindPoses, in DynamicBuffer<BoneEntity> bones, in RootEntity root) =>
        {
            // Loop over each bone
            for (int i = 0; i < skinMatrices.Length; ++i)
            {
                // Grab localToWorld matrix of bone
                var boneEntity = bones[i].Value;
                var rootEntity = root.Value;

                // #TODO: this is necessary for LiveLink?
                if (!bonesLocalToWorld.ContainsKey(boneEntity) || !rootWorldToLocal.ContainsKey(rootEntity))
                    return;

                var matrix = bonesLocalToWorld[boneEntity];

                // Convert matrix relative to root
                var rootMatrixInv = rootWorldToLocal[rootEntity];
                matrix = math.mul(rootMatrixInv, matrix);

                // Compute to skin matrix
                var bindPose = bindPoses[i].Value;
                matrix = math.mul(matrix, bindPose);

                // Assign SkinMatrix
                skinMatrices[i] = new SkinMatrix
                {
                    Value = new float3x4(matrix.c0.xyz, matrix.c1.xyz, matrix.c2.xyz, matrix.c3.xyz)
                };
            }
        }).ScheduleParallel(dependency);

        Dependency = JobHandle.CombineDependencies(bonesLocalToWorld.Dispose(dependency), rootWorldToLocal.Dispose(dependency));
    }
}

