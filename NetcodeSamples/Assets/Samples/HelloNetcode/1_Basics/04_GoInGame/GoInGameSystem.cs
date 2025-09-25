using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    // Place any established network connection in-game so ghost snapshot sync can start
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class GoInGameSystem : SystemBase
    {
        private EntityQuery m_NewConnections;

        protected override void OnCreate()
        {
            RequireForUpdate<EnableGoInGame>();
            m_NewConnections = SystemAPI.QueryBuilder().WithAll<NetworkId>().WithNone<NetworkStreamInGame>().Build();
            RequireForUpdate(m_NewConnections);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            FixedString32Bytes worldName = World.Name;

            // Go in game as soon as we have a connection set up (connection network ID has been set)
            foreach (var (id, ent) in SystemAPI.Query<NetworkId>().WithNone<NetworkStreamInGame>().WithEntityAccess())
            {
                UnityEngine.Debug.Log($"[{worldName}] Go in game connection {id.Value}");
                commandBuffer.AddComponent<NetworkStreamInGame>(ent);
            }

            commandBuffer.Playback(EntityManager);
        }
    }
}
