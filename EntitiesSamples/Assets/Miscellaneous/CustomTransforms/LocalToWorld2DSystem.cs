using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Assertions;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;

namespace Miscellaneous.CustomTransforms
{
    // This system computes a transform matrix for each entity with a LocalTransform2D.

    // For root-level / world-space entities with no Parent, the LocalToWorld can be
    // computed directly from the entity's LocalTransform2D.

    // For child entities, each unique hierarchy is traversed recursively, computing each child's LocalToWorld
    // by composing its LocalTransform with its parent's transform.

    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(ParentSystem))]
    [BurstCompile]
    public partial struct LocalToWorld2DSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LocalTransform2D>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rootsQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform2D>().WithAllRW<LocalToWorld>()
                .WithNone<Parent>().Build();
            var parentsQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform2D, Child>()
                .WithAllRW<LocalToWorld>()
                .WithNone<Parent>().Build();
            var localToWorldWriteGroupMask = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform2D, Parent>()
                .WithAllRW<LocalToWorld>().Build().GetEntityQueryMask();

            // compute LocalToWorld for all root-level entities
            var rootJob = new ComputeRootLocalToWorldJob
            {
                LocalTransform2DTypeHandleRO = SystemAPI.GetComponentTypeHandle<LocalTransform2D>(true),
                PostTransformMatrixTypeHandleRO = SystemAPI.GetComponentTypeHandle<PostTransformMatrix>(true),
                LocalToWorldTypeHandleRW = SystemAPI.GetComponentTypeHandle<LocalToWorld>(),
                LastSystemVersion = state.LastSystemVersion,
            };
            state.Dependency = rootJob.ScheduleParallelByRef(rootsQuery, state.Dependency);

            // compute LocalToWorld for all child entities
            var childJob = new ComputeChildLocalToWorldJob
            {
                LocalToWorldWriteGroupMask = localToWorldWriteGroupMask,
                ChildTypeHandle = SystemAPI.GetBufferTypeHandle<Child>(true),
                ChildLookup = SystemAPI.GetBufferLookup<Child>(true),
                LocalToWorldTypeHandleRW = SystemAPI.GetComponentTypeHandle<LocalToWorld>(),
                LocalTransform2DLookup = SystemAPI.GetComponentLookup<LocalTransform2D>(true),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true),
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(),
                LastSystemVersion = state.LastSystemVersion,
            };
            state.Dependency = childJob.ScheduleParallelByRef(parentsQuery, state.Dependency);
        }

        [BurstCompile]
        unsafe struct ComputeRootLocalToWorldJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<LocalTransform2D> LocalTransform2DTypeHandleRO;
            [ReadOnly] public ComponentTypeHandle<PostTransformMatrix> PostTransformMatrixTypeHandleRO;
            public ComponentTypeHandle<LocalToWorld> LocalToWorldTypeHandleRW;
            public uint LastSystemVersion;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                Assert.IsFalse(useEnabledMask);

                LocalTransform2D* chunk2DLocalTransforms =
                    (LocalTransform2D*)chunk.GetRequiredComponentDataPtrRO(ref LocalTransform2DTypeHandleRO);
                if (chunk.DidChange(ref LocalTransform2DTypeHandleRO, LastSystemVersion) ||
                    chunk.DidChange(ref PostTransformMatrixTypeHandleRO, LastSystemVersion))
                {
                    LocalToWorld* chunkLocalToWorlds =
                        (LocalToWorld*)chunk.GetRequiredComponentDataPtrRW(ref LocalToWorldTypeHandleRW);
                    PostTransformMatrix* chunkPostTransformMatrices =
                        (PostTransformMatrix*)chunk.GetComponentDataPtrRO(ref PostTransformMatrixTypeHandleRO);
                    if (chunkPostTransformMatrices != null)
                    {
                        for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; ++i)
                        {
                            chunkLocalToWorlds[i].Value = math.mul(chunk2DLocalTransforms[i].ToMatrix(),
                                chunkPostTransformMatrices[i].Value);
                        }
                    }
                    else
                    {
                        for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; ++i)
                        {
                            chunkLocalToWorlds[i].Value = chunk2DLocalTransforms[i].ToMatrix();
                        }
                    }
                }
            }
        }

        [BurstCompile]
        unsafe struct ComputeChildLocalToWorldJob : IJobChunk
        {
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;

            [ReadOnly] public EntityQueryMask LocalToWorldWriteGroupMask;
            [ReadOnly] public BufferTypeHandle<Child> ChildTypeHandle;
            [ReadOnly] public BufferLookup<Child> ChildLookup;
            public ComponentTypeHandle<LocalToWorld> LocalToWorldTypeHandleRW;
            [ReadOnly] public ComponentLookup<LocalTransform2D> LocalTransform2DLookup;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;
            public uint LastSystemVersion;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                Assert.IsFalse(useEnabledMask);

                bool updateChildrenTransform = chunk.DidChange(ref ChildTypeHandle, LastSystemVersion);
                BufferAccessor<Child> chunkChildBuffers = chunk.GetBufferAccessor(ref ChildTypeHandle);
                updateChildrenTransform = updateChildrenTransform ||
                                          chunk.DidChange(ref LocalToWorldTypeHandleRW, LastSystemVersion);
                LocalToWorld* chunkLocalToWorlds =
                    (LocalToWorld*)chunk.GetRequiredComponentDataPtrRO(ref LocalToWorldTypeHandleRW);
                for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++)
                {
                    var localToWorld = chunkLocalToWorlds[i].Value;
                    var children = chunkChildBuffers[i];
                    for (int j = 0, childCount = children.Length; j < childCount; j++)
                    {
                        ChildLocalToWorldFromTransformMatrix(localToWorld, children[j].Value, updateChildrenTransform);
                    }
                }
            }

            void ChildLocalToWorldFromTransformMatrix(in float4x4 parentLocalToWorld, Entity childEntity,
                bool updateChildrenTransform)
            {
                updateChildrenTransform = updateChildrenTransform
                                          || PostTransformMatrixLookup.DidChange(childEntity, LastSystemVersion)
                                          || LocalTransform2DLookup.DidChange(childEntity, LastSystemVersion);

                float4x4 localToWorld;
                if (updateChildrenTransform && LocalToWorldWriteGroupMask.MatchesIgnoreFilter(childEntity))
                {
                    var localTransform2D = LocalTransform2DLookup[childEntity];
                    localToWorld = math.mul(parentLocalToWorld, localTransform2D.ToMatrix());
                    if (PostTransformMatrixLookup.HasComponent(childEntity))
                    {
                        localToWorld = math.mul(localToWorld, PostTransformMatrixLookup[childEntity].Value);
                    }

                    LocalToWorldLookup[childEntity] = new LocalToWorld { Value = localToWorld };
                }
                else
                {
                    localToWorld = LocalToWorldLookup[childEntity].Value;
                    updateChildrenTransform = LocalToWorldLookup.DidChange(childEntity, LastSystemVersion);
                }

                if (ChildLookup.TryGetBuffer(childEntity, out DynamicBuffer<Child> children))
                {
                    for (int i = 0, childCount = children.Length; i < childCount; i++)
                    {
                        ChildLocalToWorldFromTransformMatrix(localToWorld, children[i].Value, updateChildrenTransform);
                    }
                }
            }
        }
    }
}
