using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Graphical.Splines
{
    public partial struct SnakeSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SnakeSettings>();
            state.RequireForUpdate<ExecuteSplines>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var typeSet = new NativeArray<ComponentType>(2, Allocator.Temp);
            typeSet[0] = ComponentType.ReadWrite<Snake>();
            typeSet[1] = ComponentType.ReadWrite<SnakePart>();
            var snakeArchetype = state.EntityManager.CreateArchetype(typeSet);

            var snakeLookup = SystemAPI.GetComponentLookup<Snake>();
            var snakePartLookup = SystemAPI.GetBufferLookup<SnakePart>();

            foreach (var (snakeSettings, entity) in SystemAPI.Query<RefRO<SnakeSettings>>().WithEntityAccess())
            {
                using (var snakes =
                       state.EntityManager.CreateEntity(snakeArchetype, snakeSettings.ValueRO.NumSnakes,
                           state.WorldUpdateAllocator))
                {
                    var partsCount = snakeSettings.ValueRO.NumSnakes * snakeSettings.ValueRO.NumPartsPerSnake;
                    using (var snakeParts =
                           state.EntityManager.Instantiate(snakeSettings.ValueRO.Prefab, partsCount,
                               state.WorldUpdateAllocator))
                    {
                        for (int i = 0; i < snakes.Length; i += 1)
                        {
                            snakeLookup[snakes[i]] = new Snake
                            {
                                Anchor = new float3(0, i + 1, 0) * 2,
                                Offset = -(i + 1) * 2,
                                SplineEntity = entity,
                                Speed = snakeSettings.ValueRO.Speed,
                                Spacing = snakeSettings.ValueRO.Spacing,
                            };
                            var slice = snakeParts.Slice(i * snakeSettings.ValueRO.NumPartsPerSnake,
                                snakeSettings.ValueRO.NumPartsPerSnake);
                            snakePartLookup[snakes[i]].Reinterpret<Entity>().CopyFrom(slice);
                        }
                    }
                }
            }
        }
    }
}
