using Streaming.SceneManagement.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;

namespace Streaming.SceneManagement.StreamingVolume
{
    partial struct VolumeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Relevant>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Check which volumes contain any relevant entities.
            NativeHashSet<Entity> activeVolumes = new NativeHashSet<Entity>(100, Allocator.Temp);

            var relevantQuery = SystemAPI.QueryBuilder().WithAll<Relevant, LocalToWorld>().Build();
            var relevantLocalToWorlds = relevantQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);

            foreach (var (transform, volume, volumeEntity) in
                     SystemAPI.Query<RefRO<LocalToWorld>, RefRO<Volume>>()
                         .WithEntityAccess())
            {
                var range = volume.ValueRO.Scale / 2f;
                var pos = transform.ValueRO.Position;

                foreach (var relevantLocalToWorld in relevantLocalToWorlds)
                {
                    var relevantPosition = relevantLocalToWorld.Position;
                    var distance = math.abs(relevantPosition - pos);
                    var insideAxis = (distance < range);
                    if (insideAxis.x && insideAxis.y && insideAxis.z)
                    {
                        // Relevant entity inside volume
                        activeVolumes.Add(volumeEntity);
                        break;
                    }
                }
            }

            // Load and unload sections in accord with activated volumes.
            NativeList<(Entity, LevelInfo)> toLoadList = new NativeList<(Entity, LevelInfo)>(10, Allocator.Temp);
            NativeList<(Entity, LevelInfo)> toUnloadList = new NativeList<(Entity, LevelInfo)>(10, Allocator.Temp);

            foreach (var (volumes, levelInfo, entity) in
                     SystemAPI.Query<DynamicBuffer<VolumeBuffer>, RefRW<LevelInfo>>()
                         .WithEntityAccess())
            {
                bool shouldLoad = false;
                foreach (var volume in volumes)
                {
                    if (activeVolumes.Contains(volume.volumeEntity))
                    {
                        shouldLoad = true;
                        break;
                    }
                }

                // We can't add or remove components inside a foreach query, so we
                // defer the changes to the loops that follow.
                if (shouldLoad && levelInfo.ValueRW.runtimeEntity == Entity.Null)
                {
                    toLoadList.Add((entity, levelInfo.ValueRW));
                }
                else if (!shouldLoad && levelInfo.ValueRW.runtimeEntity != Entity.Null)
                {
                    toUnloadList.Add((entity, levelInfo.ValueRW));
                }
            }

            foreach (var toLoad in toLoadList)
            {
                var (entity, streamingData) = toLoad;
                streamingData.runtimeEntity =
                    SceneSystem.LoadSceneAsync(state.WorldUnmanaged, streamingData.sceneReference);
                state.EntityManager.SetComponentData(entity, streamingData);
            }

            foreach (var toUnload in toUnloadList)
            {
                var (entity, streamingData) = toUnload;
                SceneSystem.UnloadScene(state.WorldUnmanaged, streamingData.runtimeEntity,
                    SceneSystem.UnloadParameters.DestroyMetaEntities);
                streamingData.runtimeEntity = Entity.Null;
                state.EntityManager.SetComponentData(entity, streamingData);
            }
        }
    }
}
