using Unity.Netcode;
using Unity.Entities;


namespace Samples.HelloNetcode
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    public partial class HelloNetcodeSystemGroup : ComponentSystemGroup
    {}

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation, WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class HelloNetcodeInputSystemGroup : ComponentSystemGroup
    {}

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial class HelloNetcodePredictedSystemGroup : ComponentSystemGroup
    {}
}
