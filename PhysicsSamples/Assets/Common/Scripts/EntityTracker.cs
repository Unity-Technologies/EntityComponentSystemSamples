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
[UpdateAfter(typeof(LocalToWorldSystem))]
partial struct SynchronizeGameObjectTransformsWithEntities : ISystem
{
    EntityQuery m_Query;

    public void OnCreate(ref SystemState state)
    {
        m_Query = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(EntityTracker),
                typeof(Transform),
                typeof(LocalToWorld)
            }
        });
    }

    public void OnUpdate(ref SystemState state)
    {
        var localToWorlds = m_Query.ToComponentDataListAsync<LocalToWorld>(state.World.UpdateAllocator.ToAllocator,
            out var jobHandle);
        var inputDep = JobHandle.CombineDependencies(state.Dependency, jobHandle);
        state.Dependency = new SyncTransforms
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
