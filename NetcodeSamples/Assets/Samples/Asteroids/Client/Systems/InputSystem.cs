using System.Diagnostics;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Samples.Common;

namespace Asteroids.Client
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class InputSystem : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem m_Barrier;

        protected override void OnCreate()
        {
            m_Barrier = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            RequireForUpdate<NetworkStreamInGame>();
            // Just to make sure this system does not run in other scenes
            RequireForUpdate<LevelComponent>();
        }

        protected override void OnUpdate()
        {
            byte left, right, thrust, shoot;
            left = right = thrust = shoot = 0;

            if (Input.GetKey("left") || TouchInput.GetKey(TouchInput.KeyCode.Left))
                left = 1;
            if (Input.GetKey("right") || TouchInput.GetKey(TouchInput.KeyCode.Right))
                right = 1;
            if (Input.GetKey("up") || TouchInput.GetKey(TouchInput.KeyCode.Up))
                thrust = 1;
            if (Input.GetKey("space") || TouchInput.GetKey(TouchInput.KeyCode.Space))
                shoot = 1;

            var commandBuffer = m_Barrier.CreateCommandBuffer();
            var inputFromEntity = GetBufferLookup<ShipCommandData>();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var inputTargetTick = networkTime.ServerTick;
            SystemAPI.TryGetSingletonEntity<ShipCommandData>(out var targetEntity);
            Job.WithCode(() => {
                if (targetEntity == Entity.Null)
                {
                    if (shoot != 0)
                    {
                        var req = commandBuffer.CreateEntity();
                        commandBuffer.AddComponent<PlayerSpawnRequest>(req);
                        commandBuffer.AddComponent(req, new SendRpcCommandRequest());
                    }
                }
                else
                {
                    // If ship, store commands in network command buffer
                    if (inputFromEntity.HasBuffer(targetEntity))
                    {
                        var input = inputFromEntity[targetEntity];
                        input.AddCommandData(new ShipCommandData{Tick = inputTargetTick, left = left, right = right, thrust = thrust, shoot = shoot});
                    }
                }
            }).Schedule();
            m_Barrier.AddJobHandleForProducer(Dependency);
        }
    }
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class ThinInputSystem : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem m_Barrier;
        private int m_FrameCount;

        protected override void OnCreate()
        {
            m_Barrier = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            RequireForUpdate<NetworkStreamInGame>();
            // Just to make sure this system does not run in other scenes
            RequireForUpdate<LevelComponent>();

            // Give every thin client some randomness.
            var rand = Unity.Mathematics.Random.CreateFromIndex((uint) Stopwatch.GetTimestamp());
            m_FrameCount = rand.NextInt(100);
        }

        protected override void OnUpdate()
        {
            if (SystemAPI.TryGetSingleton<CommandTarget>(out var commandTarget))
            {
                if (commandTarget.targetEntity == Entity.Null)
                {
                    // No ghosts are spawned, so we need to create a placeholder input component to store commands in.
                    // If the thin client timed out, and reconnected, we need to ensure this is not already created.
                    if (!SystemAPI.TryGetSingletonEntity<ShipCommandData>(out var ent))
                    {
                        ent = EntityManager.CreateEntity();
                        EntityManager.AddBuffer<ShipCommandData>(ent);
                    }
                    SystemAPI.SetSingleton(new CommandTarget{targetEntity = ent});
                }
            }

            byte left, right, thrust, shoot;
            left = right = thrust = shoot = 0;

            // Spawn and generate some random inputs
            var state = (int) SystemAPI.Time.ElapsedTime % 3;
            if (state == 0)
                left = 1;
            else
                thrust = 1;
            ++m_FrameCount;
            if (m_FrameCount % 100 == 0)
            {
                shoot = 1;
                m_FrameCount = 0;
            }

            var commandBuffer = m_Barrier.CreateCommandBuffer();
            var inputFromEntity = GetBufferLookup<ShipCommandData>();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var inputTargetTick = networkTime.ServerTick;
            SystemAPI.TryGetSingletonEntity<ShipCommandData>(out var targetEntity);
            Job.WithCode(() => {
                if (shoot != 0)
                {
                    // Special handling for thin clients since we can't tell if the ship is spawned or not
                    var req = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent<PlayerSpawnRequest>(req);
                    commandBuffer.AddComponent(req, new SendRpcCommandRequest());
                }
                // If ship, store commands in network command buffer
                if (inputFromEntity.HasBuffer(targetEntity))
                {
                    var input = inputFromEntity[targetEntity];
                    input.AddCommandData(new ShipCommandData{Tick = inputTargetTick, left = left, right = right, thrust = thrust, shoot = shoot});
                }
            }).Schedule();
            m_Barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
