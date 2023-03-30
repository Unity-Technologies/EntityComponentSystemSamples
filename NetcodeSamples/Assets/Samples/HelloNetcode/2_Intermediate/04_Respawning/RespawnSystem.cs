using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    readonly partial struct CharacterWithHealth : IAspect
    {
        readonly RefRW<AutoCommandTarget> m_AutoCommandTarget;
        readonly RefRO<Health> m_Health;
        readonly RefRO<GhostOwner> m_GhostOwner;
        readonly RefRO<ConnectionOwner> m_ConnectionOwner;

        public ref AutoCommandTarget AutoCommandTarget => ref m_AutoCommandTarget.ValueRW;
        public GhostOwner GhostOwner => m_GhostOwner.ValueRO;
        public ConnectionOwner ConnectionOwner => m_ConnectionOwner.ValueRO;

        public bool IsAlive()
        {
            return m_Health.ValueRO.CurrentHitPoints > 0;
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(HelloNetcodePredictedSystemGroup))]
    [UpdateAfter(typeof(DamageSystem))]
    public partial struct RespawnSystem : ISystem
    {
        Random m_Random;
        public void OnCreate(ref SystemState state)
        {
            m_Random = new Random((uint)SystemAPI.Time.ElapsedTime + 1);
            state.RequireForUpdate<Spawner>();
            state.RequireForUpdate<Health>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var playerPrefab = SystemAPI.GetSingleton<Spawner>().Player;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var linkedEntityGroupFromEntity = SystemAPI.GetBufferLookup<LinkedEntityGroup>();

            foreach (var (character, localTransform, entity) in SystemAPI.Query<CharacterWithHealth, RefRW<LocalTransform>>().WithEntityAccess())
            {
                if (character.IsAlive())
                {
                    continue;
                }

                if (FallingDown(ref character.AutoCommandTarget, ref localTransform.ValueRW.Rotation, SystemAPI.Time.DeltaTime))
                {
                    continue;
                }

                DestroyAndRespawnPlayer(ecb, entity, playerPrefab, character.GhostOwner, character.ConnectionOwner, linkedEntityGroupFromEntity);
            }

            ecb.Playback(state.EntityManager);
        }

        void DestroyAndRespawnPlayer(EntityCommandBuffer ecb, Entity entity, Entity playerPrefab, GhostOwner networkId,
            ConnectionOwner connectionOwner, BufferLookup<LinkedEntityGroup> linkedEntityGroupFromEntity)
        {
            ecb.DestroyEntity(entity);

            InitializeNewPlayer(ecb, entity, playerPrefab, networkId, connectionOwner, linkedEntityGroupFromEntity);
        }

        /// <summary>
        /// Initialize new player at a random point within the plane. (Hardcoded to [-50;50]).
        /// To patch up the network components we set CommandTarget as well as removing the destroyed entity
        /// and adding the new player entity to the linked entity group of the connection entity.
        /// </summary>
        void InitializeNewPlayer(EntityCommandBuffer ecb, Entity destroyedPlayer, Entity newPlayer, GhostOwner networkId,
            ConnectionOwner connectionOwner, BufferLookup<LinkedEntityGroup> linkedEntityGroupFromEntity)
        {
            var spawnedPlayer = ecb.Instantiate(newPlayer);
            ecb.SetComponent(spawnedPlayer, networkId);
            var newX = m_Random.NextInt(-40, 40);
            var newZ = m_Random.NextInt(-40, 40);

            ecb.SetComponent(spawnedPlayer, LocalTransform.FromPosition(new float3(newX, 1, newZ)));


            ecb.SetComponent(connectionOwner.Entity, new CommandTarget() { targetEntity = spawnedPlayer });
            ecb.AddComponent(spawnedPlayer, new ConnectionOwner { Entity = connectionOwner.Entity });

            var linkedEntityGroups = linkedEntityGroupFromEntity[connectionOwner.Entity];
            for (var index = 0; index < linkedEntityGroups.Length; index++)
            {
                var linkedEntityGroup = linkedEntityGroups[index];
                if (linkedEntityGroup.Value == destroyedPlayer)
                {
                    linkedEntityGroup.Value = newPlayer;
                    // linkedEntityGroups[index] = new LinkedEntityGroup { Value = spawnedPlayer };
                    // linkedEntityGroups.RemoveAtSwapBack(index);
                    // --index;
                }
            }

            // ecb.AppendToBuffer(connectionOwner.Entity, new LinkedEntityGroup { Value = spawnedPlayer });
        }

        static bool FallingDown(ref AutoCommandTarget autoCommandTarget, ref quaternion rotation, float deltaTime)
        {
            autoCommandTarget.Enabled = false;
            rotation = math.mul(rotation, quaternion.RotateZ(deltaTime));
            var rotatedLessThan90Degrees = math.mul(rotation, math.up()).y > 0;
            return rotatedLessThan90Degrees;
        }
    }
}
