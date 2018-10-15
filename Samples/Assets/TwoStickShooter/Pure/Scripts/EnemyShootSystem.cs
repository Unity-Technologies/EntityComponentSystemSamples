using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TwoStickPureExample
{
    [UpdateBefore(typeof(MoveForwardSystem))]
    class EnemyShootSystem : JobComponentSystem
    {
#pragma warning disable 649
        public struct PlayerData
        {
            public readonly int Length;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<PlayerInput> PlayerInput;
        }

        [Inject] private PlayerData m_Player;
        [Inject] private ShotSpawnBarrier m_ShotSpawnBarrier;
#pragma warning restore 649

        // [BurstCompile]
        // This cannot currently be burst compiled because CommandBuffer.SetComponent() accesses a static field.
        struct SpawnEnemyShots : IJobProcessComponentData<Position, EnemyShootState>
        {
            public float3 PlayerPos;
            public float DeltaTime;
            public float ShootRate;
            public float ShotTtl;
            public float ShotEnergy;
            public EntityArchetype ShotArchetype;

            public EntityCommandBuffer CommandBuffer;

            public void Execute([ReadOnly] ref Position position, ref EnemyShootState state)
            {
                state.Cooldown -= DeltaTime;
                if (state.Cooldown <= 0.0)
                {
                    state.Cooldown = ShootRate;

                    ShotSpawnData spawn;
                    spawn.Shot.TimeToLive = ShotTtl;
                    spawn.Shot.Energy = ShotEnergy;
                    spawn.Position = position;
                    spawn.Rotation = new Rotation
                    {
                        Value = quaternion.LookRotation(math.normalize(PlayerPos - position.Value), math.up())
                    };
                    spawn.Faction = Factions.kEnemy;

                    CommandBuffer.CreateEntity(ShotArchetype);
                    CommandBuffer.SetComponent(spawn);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_Player.Length == 0)
                return inputDeps;

            return new SpawnEnemyShots
            {
                PlayerPos = m_Player.Position[0].Value,
                DeltaTime = Time.deltaTime,
                ShootRate = TwoStickBootstrap.Settings.enemyShootRate,
                ShotTtl = TwoStickBootstrap.Settings.enemyShotTimeToLive,
                ShotEnergy = TwoStickBootstrap.Settings.enemyShotEnergy,
                CommandBuffer = m_ShotSpawnBarrier.CreateCommandBuffer(),
                ShotArchetype = TwoStickBootstrap.ShotSpawnArchetype,
            }.ScheduleSingle(this, inputDeps);
        }
    }
}
