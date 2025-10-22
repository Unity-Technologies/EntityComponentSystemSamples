#if UNITY_DOTS_SERVER_CI
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Asteroids.CI
{
    public struct ConnectionHitTag : IComponentData
    {
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    public partial class ConnectionLogSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var type =
              #if UNITY_SERVER
                "server"
            #elif UNITY_CLIENT
                "client"
            #elif UNITY_EDITOR
                "editor"
            #else
                "null"
            #endif
            ;

            Entities.WithNone<ConnectionHitTag>().ForEach(
                (Entity entity, in NetworkStreamConnection conn, in NetworkId id) =>
                {
                    EntityManager.AddComponent<ConnectionHitTag>(entity);
                    System.Console.WriteLine(
                        FixedString.Format(
                            "Asteroids.CI.ConnectionLogSystem: Connection live={0} for {1}\n({2}, network id={3})",
                            conn.Value.IsCreated ? 1 : 0,
                            type,
                            conn.Value.ToFixedString(),
                            id.Value));
                }).WithStructuralChanges().WithoutBurst().Run();
        }
    }
}
#endif
