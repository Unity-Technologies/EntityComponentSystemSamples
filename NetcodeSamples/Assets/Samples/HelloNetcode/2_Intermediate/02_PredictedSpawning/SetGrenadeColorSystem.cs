using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// Set the color on the grenade so it alternates between red and green.
    /// Note that we have to do this here (for all new grenades) as:
    /// - It may not be spawned by us.
    /// - If we do it in predicted code, it can fail to set before it's presented (causing the ball to be black for one render frame).
    /// - We may fail to predict spawn it (so it's essentially a new ghost from our POV).
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct SetGrenadeColorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GrenadeSpawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // The Change filter ensures we only set this color if the GrenadeData component changes, which will only happen once (when it spawns).
            foreach (var (urpColorRw, grenadeDataRo) in SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, RefRO<GrenadeData>>().WithChangeFilter<GrenadeData>())
            {
                urpColorRw.ValueRW.Value = grenadeDataRo.ValueRO.SpawnId % 2 == 1
                    ? new float4(1, 0, 0, 1)
                    : new float4(0, 1, 0, 1);
            }
        }
    }
}
