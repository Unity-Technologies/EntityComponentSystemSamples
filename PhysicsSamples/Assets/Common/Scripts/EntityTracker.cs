using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;

public class EntityTracker : MonoBehaviour {}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(TransformSystemGroup))]
#if !ENABLE_TRANSFORM_V1
[UpdateAfter(typeof(LocalToWorldSystem))]
#else
[UpdateAfter(typeof(LocalToParentSystem))]
#endif
partial class SynchronizeGameObjectTransformsWithEntities : SystemBase
{
    EntityQuery m_Query;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_Query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(EntityTracker),
                typeof(Transform),
                typeof(LocalToWorld)
            }
        });
    }

    protected override void OnUpdate()
    {
        var localToWorlds = m_Query.ToComponentDataListAsync<LocalToWorld>(World.UpdateAllocator.ToAllocator,
            out var jobHandle);
        // TODO(DOTS-6141): this call can't currently be made inline from inside the Schedule call
        var inputDep = JobHandle.CombineDependencies(Dependency, jobHandle);
        Dependency = new SyncTransforms
        {
            LocalToWorlds = localToWorlds
        }.Schedule(m_Query.GetTransformAccessArray(), inputDep);
    }

    [BurstCompile]
    struct SyncTransforms : IJobParallelForTransform
    {
        [ReadOnly] public NativeList<LocalToWorld> LocalToWorlds;

        public void Execute(int index, TransformAccess transform)
        {
            transform.position = LocalToWorlds[index].Position;
            transform.rotation = LocalToWorlds[index].Rotation;
        }
    }
}
