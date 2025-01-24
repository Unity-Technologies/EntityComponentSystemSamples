using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace HelloCube.CrossQuery
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    public partial struct SpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabCollection>();
            state.RequireForUpdate<ExecuteCrossQuery>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var prefabCollection = SystemAPI.GetSingleton<PrefabCollection>();

            // spawn boxes
            state.EntityManager.Instantiate(prefabCollection.Box, 20, Allocator.Temp);

            // init the newly spawned boxes
            int i = 0;
            foreach (var (velocity, trans, defaultColor, colorProperty) in
                     SystemAPI.Query<RefRW<Velocity>, RefRW<LocalTransform>,
                         RefRW<DefaultColor>, RefRW<URPMaterialPropertyBaseColor>>())
            {
                if (i < 10)
                {
                    // black box on left
                    velocity.ValueRW.Value = new float3(2, 0, 0);
                    var verticalOffset = i * 2;
                    trans.ValueRW.Position = new float3(-3, -8 + verticalOffset, 0);
                    defaultColor.ValueRW.Value = new float4(0, 0, 0, 1);
                    colorProperty.ValueRW.Value = new float4(0, 0, 0, 1);
                }
                else
                {
                    // white box on right
                    velocity.ValueRW.Value = new float3(-2, 0, 0);
                    var verticalOffset = (i - 10) * 2;
                    trans.ValueRW.Position = new float3(3, -8 + verticalOffset, 0);
                    defaultColor.ValueRW.Value = new float4(1, 1, 1, 1);
                    colorProperty.ValueRW.Value = new float4(1, 1, 1, 1);
                }

                i++;
            }
        }
    }
}
