using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Transforms;

// Demos 3D scale replication: writes a non-uniform scale into PostTransformMatrix while T is held.
// Replication is opt-in via the "PostTransformMatrix - 3D Scale" variant override on the Cube prefab.
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct CubeScaleSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Simulate, CubeInput>()
            .WithAllRW<PostTransformMatrix>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (input, ptm) in SystemAPI.Query<RefRO<CubeInput>, RefRW<PostTransformMatrix>>().WithAll<Simulate>())
        {
            ptm.ValueRW.Value = input.ValueRO.Scale
                ? float4x4.Scale(3, 5, 1)
                : float4x4.identity;
        }
    }
}
