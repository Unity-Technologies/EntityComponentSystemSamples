using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;

public class EntityTracker : MonoBehaviour {}

[UpdateInGroup(typeof(TransformSystemGroup))]
[UpdateAfter(typeof(LocalToParentSystem))]
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
        var localToWorlds = m_Query.ToComponentDataArrayAsync<LocalToWorld>(Allocator.TempJob, out var jobHandle);
        Dependency = new SyncTransforms
        {
            LocalToWorlds = localToWorlds
        }.Schedule(m_Query.GetTransformAccessArray(), JobHandle.CombineDependencies(Dependency, jobHandle));
    }

    [BurstCompile]
    struct SyncTransforms : IJobParallelForTransform
    {
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<LocalToWorld> LocalToWorlds;

        public void Execute(int index, TransformAccess transform)
        {
            transform.position = LocalToWorlds[index].Position;
            transform.rotation = LocalToWorlds[index].Rotation;
        }
    }
}
