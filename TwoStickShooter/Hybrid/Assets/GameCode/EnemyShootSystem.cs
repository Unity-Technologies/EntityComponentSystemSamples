using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TwoStickHybridExample
{
    public class EnemyShootSystem : ComponentSystem
    {
        public struct Data
        {
            public int Length;
            public ComponentArray<Position2D> Position;
            public ComponentArray<EnemyShootState> ShootState;
        }

        [Inject] private Data m_Data;

        public struct PlayerData
        {
            public int Length;
            public ComponentArray<Position2D> Position;
            public ComponentArray<PlayerInput> PlayerInput;
        }

        [Inject] private PlayerData m_Player;

        protected override void OnUpdate()
        {
            if (m_Player.Length == 0)
                return;

            var playerPos = m_Player.Position[0].Value;

            var shotSpawnData = new List<ShotSpawnData>();

            float dt = Time.deltaTime;
            float shootRate = TwoStickBootstrap.Settings.enemyShootRate;

            for (int i = 0; i < m_Data.Length; ++i)
            {
                var state = m_Data.ShootState[i];

                state.Cooldown -= dt;
                if (state.Cooldown <= 0.0)
                {
                    state.Cooldown = shootRate;
                    var position = m_Data.Position[i].Value;

                    ShotSpawnData spawn = new ShotSpawnData()
                    {
                        Position = position,
                        Heading = math.normalize(playerPos - position),
                        Faction = TwoStickBootstrap.Settings.EnemyFaction
                    };
                    shotSpawnData.Add(spawn);
                }
            }

            foreach (var spawn in shotSpawnData)
            {
                ShotSpawnSystem.SpawnShot(spawn);
            }
        }
    }
}