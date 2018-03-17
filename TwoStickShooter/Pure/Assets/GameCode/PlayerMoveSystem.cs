using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms2D;

namespace TwoStickPureExample
{
    public class PlayerMoveSystem : ComponentSystem
    {
        public struct Data
        {
            public int Length;
            public ComponentDataArray<Position2D> Position;
            public ComponentDataArray<Heading2D> Heading;
            public ComponentDataArray<PlayerInput> Input;
        }

        [Inject] private Data m_Data;

        protected override void OnUpdate()
        {
            var settings = TwoStickBootstrap.Settings;

            float dt = Time.deltaTime;
            for (int index = 0; index < m_Data.Length; ++index)
            {
                var position = m_Data.Position[index].Value;
                var heading = m_Data.Heading[index].Value;

                var playerInput = m_Data.Input[index];

                position += dt * playerInput.Move * settings.playerMoveSpeed;

                if (playerInput.Fire)
                {
                    heading = math.normalize(playerInput.Shoot);

                    playerInput.FireCooldown = settings.playerFireCoolDown;

                    PostUpdateCommands.CreateEntity(TwoStickBootstrap.ShotSpawnArchetype);
                    PostUpdateCommands.SetComponent(new ShotSpawnData
                    {
                        Shot = new Shot
                        {
                            TimeToLive = settings.bulletTimeToLive,
                            Energy = settings.playerShotEnergy,
                        },
                        Position = new Position2D{ Value = position },
                        Heading = new Heading2D{ Value = heading },
                        Faction = Factions.kPlayer,
                    });
                }

                m_Data.Position[index] = new Position2D {Value = position};
                m_Data.Heading[index] = new Heading2D {Value = heading};
                m_Data.Input[index] = playerInput;
            }
        }
    }
}
