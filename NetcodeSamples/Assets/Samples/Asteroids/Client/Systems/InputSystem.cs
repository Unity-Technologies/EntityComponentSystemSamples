using System;
using System.Diagnostics;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
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
            RequireForUpdate<NetworkId>();
            // Just to make sure this system does not run in other scenes
            RequireForUpdate<LevelComponent>();
        }

        struct InputJob : IJob
        {
            public byte left, right, thrust, shoot;
            public EntityCommandBuffer commandBuffer;
            public BufferLookup<ShipCommandData> inputFromEntity;
            public NetworkTick inputTargetTick;
            public Entity targetEntity;

            public void Execute()
            {
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
            }
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
            var inputTargetTick = networkTime.InputTargetTick;

            // singleton and enableable don't mix well.
            // https://jira.unity3d.com/browse/DOTS-9695
            // https://unity.slack.com/archives/CE7DZN2H1/p1699385984519549
            // SystemAPI.TryGetSingletonEntity<GhostOwnerIsLocal>(out var targetEntity); // <-- doesn't work, enableable not supported for singletons.
            Entity targetEntity = Entity.Null;
            foreach (var (_, entity) in SystemAPI.Query<RefRO<GhostOwnerIsLocal>>().WithAll<ShipCommandData>().WithEntityAccess())
            {
                if (targetEntity != Entity.Null) throw new Exception("Sanity check failed! More than once instance!");
                targetEntity = entity;
            }
            // var targetEntity = SystemAPI.QueryBuilder().WithAll<ShipCommandData, GhostOwnerIsLocal>().Build().GetSingletonEntity(); // <-- can't work
            // SystemAPI.TryGetSingletonEntity<ShipCommandData>(out var targetEntity); // could do this in binary world mode, but can't on single world since now client systems execute in a server world which contains all ship commands components.
            Dependency = new InputJob()
            {
                left = left,
                right = right,
                thrust = thrust,
                shoot = shoot,
                commandBuffer = commandBuffer,
                inputFromEntity = inputFromEntity,
                inputTargetTick = inputTargetTick,
                targetEntity = targetEntity,
            }.Schedule(Dependency);
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

        struct ThinInputJob : IJob
        {
            public byte left, right, thrust, shoot;
            public EntityCommandBuffer commandBuffer;
            public BufferLookup<ShipCommandData> inputFromEntity;
            public NetworkTick inputTargetTick;
            public Entity targetEntity;
            public void Execute()
            {
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
            }
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
            Dependency = new ThinInputJob()
            {
                left = left,
                right = right,
                thrust = thrust,
                shoot = shoot,
                commandBuffer = commandBuffer,
                inputFromEntity = inputFromEntity,
                inputTargetTick = inputTargetTick,
                targetEntity = targetEntity,
            }.Schedule(Dependency);
            m_Barrier.AddJobHandleForProducer(Dependency);
            m_Barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
