using System;
using Unity.Entities;

namespace Unity.NetCode.Samples.Common
{
    /// <summary>
    ///     We use a dynamic assembly list so we can build a server with a subset of the assemblies
    ///     (only including one of the samples instead of all).
    ///     If you only have a single game in the project you generally do not need to enable DynamicAssemblyList.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [CreateAfter(typeof(RpcSystem))]
    public partial struct SetRpcSystemDynamicAssemblyListSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<RpcCollection>().ValueRW.DynamicAssemblyList = true;
            state.Enabled = false;
        }
    }
}
