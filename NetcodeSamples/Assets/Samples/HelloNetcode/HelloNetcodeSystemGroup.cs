using Unity.Entities;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    public class HelloNetcodeSystemGroup : ComponentSystemGroup
    {}

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation, WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public class HelloNetcodeInputSystemGroup : ComponentSystemGroup
    {}

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public class HelloNetcodePredictedSystemGroup : ComponentSystemGroup
    {}
}
