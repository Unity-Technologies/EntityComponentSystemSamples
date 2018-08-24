using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TwoStickPureExample
{
    [UpdateBefore(typeof(MoveForwardSystem))]
    class EnemyShootSystem : JobComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            [ReadOnly] public ComponentDataArray<Position> Position;
            public ComponentDataArray<EnemyShootState> ShootState;
        }

        [Inject] private Data m_Data;

        public struct PlayerData
        {
            public readonly int Length;
            [ReadOnly] public ComponentDataArray<Position> Position;
            [ReadOnly] public ComponentDataArray<PlayerInput> PlayerInput;
        }

        [Inject] private PlayerData m_Player;
        [Inject] private ShotSpawnBarrier m_ShotSpawnBarrier;

        // [BurstCompile]
        // This cannot currently be burst compiled because CommandBuffer.SetComponent() accesses a static field.
        struct SpawnEnemyShots : IJob
        {
            public float3 PlayerPos;
            public float DeltaTime;
            public float ShootRate;
            public float ShotTtl;
            public float ShotEnergy;
            public EntityArchetype ShotArchetype;

            [ReadOnly] public ComponentDataArray<Position> Position;
            public ComponentDataArray<EnemyShootState> ShootState;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                for (int i = 0; i < ShootState.Length; ++i)
                {
                    var state = ShootState[i];

                    state.Cooldown -= DeltaTime;
                    if (state.Cooldown <= 0.0)
                    {
                        state.Cooldown = ShootRate;

                        ShotSpawnData spawn;
                        spawn.Shot.TimeToLive = ShotTtl;
                        spawn.Shot.Energy = ShotEnergy;
                        spawn.Position = Position[i];
                        spawn.Rotation = new Rotation
                        {
                            Value = quaternion.lookRotation(math.normalize(PlayerPos - Position[i].Value), math.up())
                        };
                        spawn.Faction = Factions.kEnemy;

                        CommandBuffer.CreateEntity(ShotArchetype);
                        CommandBuffer.SetComponent(spawn);
                    }

                    ShootState[i] = state;
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_Data.Length == 0 || m_Player.Length == 0)
                return inputDeps;

            return new SpawnEnemyShots
            {
                PlayerPos = m_Player.Position[0].Value,
                DeltaTime = Time.deltaTime,
                ShootRate = TwoStickBootstrap.Settings.enemyShootRate,
                ShotTtl = TwoStickBootstrap.Settings.enemyShotTimeToLive,
                ShotEnergy = TwoStickBootstrap.Settings.enemyShotEnergy,
                Position = m_Data.Position,
                ShootState = m_Data.ShootState,
                CommandBuffer = m_ShotSpawnBarrier.CreateCommandBuffer(),
                ShotArchetype = TwoStickBootstrap.ShotSpawnArchetype,
            }.Schedule(inputDeps);
        }
    }
}
