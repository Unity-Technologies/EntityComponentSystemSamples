using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace HelloCube.GameObjectSync
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GameObjectInitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<GameObjectPrefab>().Build();
            state.RequireForUpdate(query);
            state.RequireForUpdate<Execute>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Instantiate the associated GameObject from the prefab.
            foreach (var (goPrefab, entity) in SystemAPI.Query<GameObjectPrefab>().WithEntityAccess())
            {
                var go = GameObject.Instantiate(goPrefab.Prefab);
                ecb.AddComponent(entity, new RotatingGameObject(go));
                ecb.RemoveComponent<GameObjectPrefab>(entity);
            }
        }
    }
}