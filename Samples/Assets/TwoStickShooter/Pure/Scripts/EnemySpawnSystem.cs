using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TwoStickPureExample
{
    class EnemySpawnSystem : ComponentSystem
    {
        struct State
        {
#pragma warning disable 649
            public readonly int Length;
#pragma warning restore 649
            public ComponentDataArray<EnemySpawnCooldown> Cooldown;
            public ComponentDataArray<EnemySpawnSystemState> S;
        }

        [Inject] State m_State;

        public static void SetupComponentData(EntityManager entityManager)
        {
            var arch = entityManager.CreateArchetype(typeof(EnemySpawnCooldown), typeof(EnemySpawnSystemState));
            var stateEntity = entityManager.CreateEntity(arch);
            var oldState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(0xaf77);
            entityManager.SetComponentData(stateEntity, new EnemySpawnCooldown { Value = 0.0f });
            entityManager.SetComponentData(stateEntity, new EnemySpawnSystemState
            {
                SpawnedEnemyCount = 0,
                RandomState = UnityEngine.Random.state
            });
            UnityEngine.Random.state = oldState;
        }


        protected override void OnUpdate()
        {
            float cooldown = m_State.Cooldown[0].Value;

            cooldown = Mathf.Max(0.0f, m_State.Cooldown[0].Value - Time.deltaTime);
            bool spawn = cooldown <= 0.0f;

            if (spawn)
            {
                cooldown = ComputeCooldown();
            }

            m_State.Cooldown[0] = new EnemySpawnCooldown { Value = cooldown };

            if (spawn)
            {
                SpawnEnemy();
            }
        }

        void SpawnEnemy()
        {
            var state = m_State.S[0];
            var oldState = UnityEngine.Random.state;
            UnityEngine.Random.state = state.RandomState;

            float3 spawnPosition = ComputeSpawnLocation();
            state.SpawnedEnemyCount++;

            PostUpdateCommands.CreateEntity(TwoStickBootstrap.BasicEnemyArchetype);
            PostUpdateCommands.SetComponent(new Position { Value = spawnPosition });
            PostUpdateCommands.SetComponent(new Rotation
            {
                Value = quaternion.LookRotation(new float3(0.0f, 0.0f, -1.0f), math.up())
            });
            PostUpdateCommands.SetComponent(new Health { Value = TwoStickBootstrap.Settings.enemyInitialHealth });
            PostUpdateCommands.SetComponent(new EnemyShootState { Cooldown = 0.5f });
            PostUpdateCommands.SetComponent(new MoveSpeed { speed = TwoStickBootstrap.Settings.enemySpeed });
            PostUpdateCommands.AddSharedComponent(TwoStickBootstrap.EnemyLook);

            state.RandomState = UnityEngine.Random.state;

            m_State.S[0] = state;
            UnityEngine.Random.state = oldState;
        }

        float ComputeCooldown()
        {
            return 0.15f;
        }

        float3 ComputeSpawnLocation()
        {
            var settings = TwoStickBootstrap.Settings;

            float r = UnityEngine.Random.value;
            float x0 = settings.playfield.xMin;
            float x1 = settings.playfield.xMax;
            float x = x0 + (x1 - x0) * r;

            return new float3(x, 0.0f, settings.playfield.yMax); // Y axis is positive up
        }
    }
}
