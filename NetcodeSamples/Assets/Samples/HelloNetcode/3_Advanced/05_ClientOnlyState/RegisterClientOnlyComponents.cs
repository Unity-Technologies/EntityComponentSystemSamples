using Unity.Entities;
using Unity.NetCode.Samples;

namespace Samples.HelloNetcode
{
    // System that register the PlayerMovement component to the ClientOnlyComponent collection.
    // The registration is performed at runtime instead of at creation time (inside the OnCreate) only
    // because the EnableClientOnlyState condition should be checked.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct RegisterClientOnlyComponents : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableClientOnlyState>();
            state.RequireForUpdate<ClientOnlyCollection>();
        }
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<ClientOnlyCollection>().ValueRW.RegisterClientOnlyComponentType(
                ComponentType.ReadWrite<PlayerMovement>());
            state.Enabled = false;
        }
    }
}
