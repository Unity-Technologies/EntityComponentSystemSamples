using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TwoStickHybridExample
{
    public class PlayerMoveSystem : ComponentSystem
    {
        public struct Data
        {
            public int Length;
            public GameObjectArray GameObject;
            public ComponentArray<Position2D> Position;
            public ComponentArray<Heading2D> Heading;
            public ComponentArray<PlayerInput> Input;
        }

        [Inject] private Data m_Data;

        protected override void OnUpdate()
        {
            if (m_Data.Length == 0)
                return;

            var settings = TwoStickBootstrap.Settings;

            float dt = Time.deltaTime;
            var firingPlayers = new List<GameObject>();
            for (int index = 0; index < m_Data.Length; ++index)
            {
                var position = m_Data.Position[index];
                var heading = m_Data.Heading[index];

                var playerInput = m_Data.Input[index];

                position.Value += dt * playerInput.Move * settings.playerMoveSpeed;

                if (playerInput.Fire)
                {
                    heading.Value = math.normalize(playerInput.Shoot);
                    playerInput.FireCooldown = settings.playerFireCoolDown;

                    firingPlayers.Add(m_Data.GameObject[index]);
                }
            }

            foreach (var player in firingPlayers)
            {
                var newShotData = new ShotSpawnData()
                {
                    Position = player.GetComponent<Position2D>().Value,
                    Heading = player.GetComponent<Heading2D>().Value,
                    Faction = player.GetComponent<Faction>()
                };

                ShotSpawnSystem.SpawnShot(newShotData);
            }
        }
    }
}
