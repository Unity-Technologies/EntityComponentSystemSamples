using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;

namespace TwoStickPureExample
{
    public class PlayerMoveSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public ComponentDataArray<Position> Position;
            public ComponentDataArray<Rotation> Heading;
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
                var rotation = m_Data.Heading[index].Value;

                var playerInput = m_Data.Input[index];

                position += dt * playerInput.Move * settings.playerMoveSpeed;

                if (playerInput.Fire)
                {
                    rotation = quaternion.LookRotation(math.normalize(playerInput.Shoot),math.up());

                    playerInput.FireCooldown = settings.playerFireCoolDown;

                    PostUpdateCommands.CreateEntity(TwoStickBootstrap.ShotSpawnArchetype);
                    PostUpdateCommands.SetComponent(new ShotSpawnData
                    {
                        Shot = new Shot
                        {
                            TimeToLive = settings.bulletTimeToLive,
                            Energy = settings.playerShotEnergy,
                        },
                        Position = new Position { Value = position },
                        Rotation = new Rotation { Value = rotation },
                        Faction = Factions.kPlayer,
                    });
                }

                m_Data.Position[index] = new Position {Value = position};
                m_Data.Heading[index] = new Rotation {Value = rotation};
                m_Data.Input[index] = playerInput;
            }
        }
    }
}
