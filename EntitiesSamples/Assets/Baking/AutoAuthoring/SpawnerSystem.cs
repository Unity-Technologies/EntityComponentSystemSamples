using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Baking.AutoAuthoring
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public partial struct SpawnerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Spawner>();
            state.RequireForUpdate<ManagedSpawner>();
            state.RequireForUpdate<BufferSpawner>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            // unmanaged spawner
            {
                var spawner = SystemAPI.GetSingleton<Spawner>();

                var instances = new NativeArray<Entity>(spawner.InstanceCount, Allocator.Temp);
                state.EntityManager.Instantiate(spawner.Prefab, instances);

                var offset = math.float3(0);
                foreach (var entity in instances)
                {
                    state.EntityManager.SetComponentData(entity, LocalTransform.FromPosition(offset));
                    offset += spawner.Offset;
                }
            }

            // managed spawner
            {
                var query = SystemAPI.QueryBuilder().WithAll<ManagedSpawner>().Build();
                var spawner = query.GetSingleton<ManagedSpawner>();

                var instances = new NativeArray<Entity>(spawner.InstanceCount, Allocator.Temp);
                var em = state.EntityManager;
                em.Instantiate(spawner.Prefab, instances);

                var offset = math.float3(0);
                foreach (var entity in instances)
                {
                    em.SetComponentData(entity, LocalTransform.FromPosition(offset));
                    offset += spawner.Offset;

                    var materialMeshInfo = em.GetComponentData<MaterialMeshInfo>(entity);
                    var renderMeshArray = em.GetSharedComponentManaged<RenderMeshArray>(entity);
                    renderMeshArray.MaterialReferences[MaterialMeshInfo.StaticIndexToArrayIndex(materialMeshInfo.Material)] =
                        spawner.Material;
                    renderMeshArray.ResetHash128();

                    em.SetSharedComponentManaged(entity, renderMeshArray);
                }
            }

            // buffer spawner
            {
                var spawnElements = SystemAPI.GetSingletonBuffer<BufferSpawner>();
                var offset = math.float3(0);
                for (int i = 0; i < spawnElements.Length; ++i)
                {
                    var element = spawnElements[i];

                    var instances = new NativeArray<Entity>(element.InstanceCount, Allocator.Temp);
                    state.EntityManager.Instantiate(element.Prefab, instances);
                    foreach (var entity in instances)
                    {
                        state.EntityManager.SetComponentData(entity, LocalTransform.FromPosition(offset));
                        offset += element.Offset;
                    }
                }
            }
        }
    }
#endif
}
