using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Transforms2D;
using UnityEngine;

namespace TwoStickPureExample
{
    class EnemySpawnSystem : ComponentSystem
    {
        struct State
        {
            public readonly int Length;
            public ComponentDataArray<EnemySpawnCooldown> Cooldown;
            public ComponentDataArray<EnemySpawnSystemState> S;
        }

        [Inject] State m_State;

        public static void SetupComponentData(EntityManager entityManager)
        {
            var arch = entityManager.CreateArchetype(typeof(EnemySpawnCooldown), typeof(EnemySpawnSystemState));
            var stateEntity = entityManager.CreateEntity(arch);
            var oldState = Random.state;
            Random.InitState(0xaf77);
            entityManager.SetComponentData(stateEntity, new EnemySpawnCooldown { Value = 0.0f });
            entityManager.SetComponentData(stateEntity, new EnemySpawnSystemState
            {
                SpawnedEnemyCount = 0,
                RandomState = Random.state
            });
            Random.state = oldState;
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
            var oldState = Random.state;
            Random.state = state.RandomState;

            float2 spawnPosition = ComputeSpawnLocation();
            state.SpawnedEnemyCount++;

            PostUpdateCommands.CreateEntity(TwoStickBootstrap.BasicEnemyArchetype);
            PostUpdateCommands.SetComponent(new Position2D { Value = spawnPosition });
            PostUpdateCommands.SetComponent(new Heading2D { Value = new float2(0.0f, -1.0f) });
            PostUpdateCommands.SetComponent(default(Enemy));
            PostUpdateCommands.SetComponent(new Health { Value = TwoStickBootstrap.Settings.enemyInitialHealth });
            PostUpdateCommands.SetComponent(new EnemyShootState { Cooldown = 0.5f });
            PostUpdateCommands.SetComponent(new MoveSpeed { speed = TwoStickBootstrap.Settings.enemySpeed });
            PostUpdateCommands.AddSharedComponent(TwoStickBootstrap.EnemyLook);

            state.RandomState = Random.state;

            m_State.S[0] = state;
            Random.state = oldState;
        }

        float ComputeCooldown()
        {
            return 0.15f;
        }

        float2 ComputeSpawnLocation()
        {
            var settings = TwoStickBootstrap.Settings;

            float r = Random.value;
            float x0 = settings.playfield.xMin;
            float x1 = settings.playfield.xMax;
            float x = x0 + (x1 - x0) * r;

            return new float2(x, settings.playfield.yMax); // Y axis is positive up
        }
    }
}
