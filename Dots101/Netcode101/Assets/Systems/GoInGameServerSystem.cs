using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace KickBall
{
    // When server receives GoInGameRequest, ready the client to receive ghosts, spawn the player character, and delete the RPC request
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct GoInGameServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityPrefabs>();
            var query = SystemAPI.QueryBuilder().WithAll<GoInGameRequest, ReceiveRpcCommandRequest>().Build();
            state.RequireForUpdate(query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerPrefab = SystemAPI.GetSingleton<EntityPrefabs>().Player;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (requestSource, requestEntity) in
                     SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>()
                         .WithAll<GoInGameRequest>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<NetworkStreamInGame>(requestSource.ValueRO.SourceConnection);

                // spawn player
                {
                    var networkId = SystemAPI.GetComponent<NetworkId>(requestSource.ValueRO.SourceConnection);

                    var player = ecb.Instantiate(playerPrefab);
                    ecb.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });

                    // Set color for player based on their network ID.
                    var rand = Random.CreateFromIndex((uint)networkId.Value);
                    ecb.SetComponent(player, new Color { Value = new float4(rand.NextFloat3(), 0) });

                    // Add the player to the linked entity group so it is destroyed automatically on disconnect
                    ecb.AppendToBuffer(requestSource.ValueRO.SourceConnection, new LinkedEntityGroup { Value = player });    
                }

                ecb.DestroyEntity(requestEntity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
