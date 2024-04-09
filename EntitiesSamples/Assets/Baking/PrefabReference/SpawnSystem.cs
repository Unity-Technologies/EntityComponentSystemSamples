using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;

namespace Baking.PrefabReference
{
    public partial struct SpawnSystem : ISystem
    {
        float timer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<PrefabLoadResult>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            timer -= SystemAPI.Time.DeltaTime;
            if (timer > 0)
            {
                return;
            }

            var config = SystemAPI.GetSingleton<Config>();
            timer = config.SpawnInterval;

            var configEntity = SystemAPI.GetSingletonEntity<Config>();
            if (!SystemAPI.HasComponent<PrefabLoadResult>(configEntity))
            {
                return;
            }

            var prefabLoadResult = SystemAPI.GetComponent<PrefabLoadResult>(configEntity);
            var entity = state.EntityManager.Instantiate(prefabLoadResult.PrefabRoot);
            var random = Random.CreateFromIndex((uint) state.GlobalSystemVersion);
            state.EntityManager.SetComponentData(entity,
                LocalTransform.FromPosition(random.NextFloat(-5, 5), random.NextFloat(-5, 5), 0));
        }
    }
}
