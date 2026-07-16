using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.ClosestTarget
{
    public partial struct InitializationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Settings>();
            state.RequireForUpdate<ExecuteClosestTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var settings = SystemAPI.GetSingleton<Settings>();
            var random = Random.CreateFromIndex(1234);

            Spawn(ref state, settings.UnitPrefab, settings.UnitCount, ref random);
            Spawn(ref state, settings.TargetPrefab, settings.TargetCount, ref random);
        }

        void Spawn(ref SystemState state, Entity prefab, int count, ref Random random)
        {
            var units = state.EntityManager.Instantiate(prefab, count, Allocator.Temp);

            for (int i = 0; i < units.Length; i += 1)
            {
                var position = new float3();
                position.xz = random.NextFloat2() * 200 - 100;
                state.EntityManager.SetComponentData(units[i],
                    new LocalTransform { Position = position, Scale = 1 });
                state.EntityManager.SetComponentData(units[i],
                    new Movement { Value = random.NextFloat2Direction() });
            }
        }
    }
}
