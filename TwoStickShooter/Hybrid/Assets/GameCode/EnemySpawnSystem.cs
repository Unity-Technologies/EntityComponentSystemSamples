using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TwoStickHybridExample
{
    // Spawns new enemies.
    public class EnemySpawnSystem : ComponentSystem
    {

        public struct State
        {
            public int Length;
            public ComponentArray<EnemySpawnSystemState> S;
        }

        [Inject] private State m_State;

        public static void SetupComponentData()
        {
            
            var oldState = Random.state;
            Random.InitState(0xaf77);
            
            var state = TwoStickBootstrap.Settings.EnemySpawnState;
            state.Cooldown = 0.0f;
            state.SpawnedEnemyCount = 0;
            state.RandomState = Random.state;
            
            Random.state = oldState;
        }
        
        protected override void OnUpdate()
        {
            var state = m_State.S[0];

            var oldState = Random.state;
            Random.state = state.RandomState;

            state.Cooldown -= Time.deltaTime;

            if (state.Cooldown <= 0.0f)
            {
                var settings = TwoStickBootstrap.Settings;
                var enemy = Object.Instantiate(settings.EnemyPrefab);
                ComputeSpawnLocation(enemy);
                state.SpawnedEnemyCount++;
                state.Cooldown = ComputeCooldown(state.SpawnedEnemyCount);
            }

            state.RandomState = Random.state;

            Random.state = oldState;
        }

        private float ComputeCooldown(int stateSpawnedEnemyCount)
        {
            return 0.15f;
        }

        private void ComputeSpawnLocation(GameObject enemy)
        {
            var settings = TwoStickBootstrap.Settings;

            float r = Random.value;
            float x0 = settings.playfield.xMin;
            float x1 = settings.playfield.xMax;
            float x = x0 + (x1 - x0) * r;

            enemy.GetComponent<Position2D>().Value = new float2(x, settings.playfield.yMax);
            enemy.GetComponent<Heading2D>().Value = new float2(0, -TwoStickBootstrap.Settings.enemySpeed);
        }
    }

}
