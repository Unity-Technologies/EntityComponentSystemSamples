using System;
using System.Data.Common;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;
using Unity.Physics.Systems;
using Random = Unity.Mathematics.Random;

namespace Samples.MultyPhysicsWorld
{
    // RPC request from client to server for game to go "in game" and send snapshots / inputs
    public struct GoInGameRequest : IRpcCommand
    {
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class VisualizationPhysicsSystemGroup : CustomPhysicsSystemGroup
    {
        public VisualizationPhysicsSystemGroup() : base(1, true)
        {}

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<PhysicsSpawner>();
        }
    }

    // When client has a connection with network id, go in game and tell server to also go in game
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation|WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class GoInGameClientSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<PhysicsSpawner>();
            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<NetworkId>(), ComponentType.Exclude<NetworkStreamInGame>()));
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entities.WithNone<NetworkStreamInGame>().ForEach((Entity ent, in NetworkId id) =>
            {
                commandBuffer.AddComponent<NetworkStreamInGame>(ent);
                var req = commandBuffer.CreateEntity();
                commandBuffer.AddComponent<GoInGameRequest>(req);
                commandBuffer.AddComponent(req, new SendRpcCommandRequest { TargetConnection = ent });
            }).Run();
            commandBuffer.Playback(EntityManager);
        }
    }

    // When server receives go in game request, go in game and delete request
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class GoInGameServerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<PhysicsSpawner>();
            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<GoInGameRequest>(), ComponentType.ReadOnly<ReceiveRpcCommandRequest>()));
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var networkIdFromEntity = GetComponentLookup<NetworkId>(true);
            var spawner = SystemAPI.GetSingleton<PhysicsSpawner>();
            Entities
                .WithReadOnly(networkIdFromEntity).ForEach((
                Entity reqEnt,
                in GoInGameRequest req,
                in ReceiveRpcCommandRequest reqSrc) =>
            {
                commandBuffer.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);
                commandBuffer.DestroyEntity(reqEnt);

                var entity = commandBuffer.Instantiate(spawner.prefab);
                //Add the entity to the connection linked entity group so when connection is destroyed, ghost are too
                var linkedEntityGroups = commandBuffer.AddBuffer<LinkedEntityGroup>(reqSrc.SourceConnection);
                linkedEntityGroups.Add(entity);

                commandBuffer.AddBuffer<PlayerInput>(entity);

                commandBuffer.SetComponent(entity, LocalTransform.FromPosition(new float3(0.0f, 1.0f, 0.0f)));

                commandBuffer.SetComponent(entity, new GhostOwner { NetworkId = networkIdFromEntity[reqSrc.SourceConnection].Value });
                commandBuffer.SetComponent(reqSrc.SourceConnection, new CommandTarget { targetEntity = entity });
            }).Run();
            commandBuffer.Playback(EntityManager);
        }
    }

    public struct PlayerInput : ICommandData
    {
        public NetworkTick Tick {get; set;}
        public int horizontal;
        public int vertical;
        public int rotation;

        public FixedString512Bytes ToFixedString() => $"h:{horizontal},v:{vertical},rot:{rotation}";
    }

    // When server receives go in game request, go in game and delete request
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class ImputSampling : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<PhysicsSpawner>();
            RequireForUpdate<NetworkStreamInGame>();
        }

        protected override void OnUpdate()
        {
            var localInput = SystemAPI.GetSingleton<CommandTarget>().targetEntity;
            if (localInput == Entity.Null)
            {
                var localPlayerId = SystemAPI.GetSingleton<NetworkId>().Value;
                var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
                var connection = SystemAPI.GetSingletonEntity<CommandTarget>();
                Entities
                    .WithNone<PlayerInput>()
                    .ForEach((Entity ent, in GhostOwner ghostOwner) =>
                {
                    if (ghostOwner.NetworkId == localPlayerId)
                    {
                        commandBuffer.AddBuffer<PlayerInput>(ent);
                        commandBuffer.SetComponent(connection, new CommandTarget {targetEntity = ent});
                    }
                }).Run();
                commandBuffer.Playback(EntityManager);
                return;
            }
            var input = default(PlayerInput);
            input.Tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            if (Input.GetKey(KeyCode.LeftArrow))
                input.horizontal -= 1;
            if (Input.GetKey(KeyCode.RightArrow))
                input.horizontal += 1;
            if (Input.GetKey(KeyCode.DownArrow))
                input.vertical -= 1;
            if (Input.GetKey(KeyCode.UpArrow))
                input.vertical += 1;
            if (Input.GetKey(KeyCode.Space))
                input.rotation = 1;
            var inputBuffer = EntityManager.GetBuffer<PlayerInput>(localInput);
            inputBuffer.AddCommandData(input);
        }
    }

    // When server receives go in game request, go in game and delete request
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsInitializeGroup))]
    public partial class CubeFlySystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<PhysicsSpawner>();
            RequireForUpdate<GhostCollection>();
        }

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var predictTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            var isServer = World.IsServer();
            Entities
                .WithAll<Simulate>()
                .ForEach((Entity playerEntity,
                    ref PhysicsVelocity physicsVelocity,

                    ref LocalTransform transform,

                    in PhysicsMass physicsMass,
                    in DynamicBuffer<PlayerInput> playerInputs) =>
                {
                    if(!playerInputs.GetDataAtTick(predictTick, out var input))
                        return;

                    var force = new float3
                    {
                        x = input.horizontal,
                        y = 0f,
                        z = input.vertical,
                    };
                    force = math.normalizesafe(force);
                    force *= 10.0f;
                    var impulse = force * deltaTime;
                    var angularInpulse = new float3(0f, input.rotation, 0f) * deltaTime;
                    physicsVelocity.ApplyLinearImpulse(physicsMass, impulse);
                    physicsVelocity.ApplyAngularImpulse(physicsMass, angularInpulse);
                    //force the cube to be at 1 mt from the ground
                    //translation.Value.y = 0.7f + 0.1f*math.sin((float)(math.PI * predictTick * 1.0f/60f));
                }).Schedule();
        }
    }

    public struct ParticleAge : IComponentData
    {
        public double deadTime;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ParticleEmitterSystem : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem m_Barrier;
        private ComponentLookup<LocalTransform> m_TransformLookup;
        protected override void OnCreate()
        {
            m_Barrier = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            m_TransformLookup = GetComponentLookup<LocalTransform>(true);
            RequireForUpdate<PhysicsSpawner>();
            RequireForUpdate<GhostCollection>();
            RequireForUpdate<ParticleEmitter>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var commandBuffer = m_Barrier.CreateCommandBuffer().AsParallelWriter();
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            m_TransformLookup.Update(this);

            Entities.ForEach((Entity entity, int nativeThreadIndex, ref ParticleAge age) =>
            {
                if (age.deadTime < elapsedTime)
                    commandBuffer.DestroyEntity(nativeThreadIndex, entity);
            }).ScheduleParallel();
            m_Barrier.AddJobHandleForProducer(Dependency);

            var transformLookup = m_TransformLookup;
            Entities
                .WithReadOnly(transformLookup)
                .ForEach((Entity entity, int nativeThreadIndex, ref ParticleEmitter emitter, in LocalTransform transform) =>

            {
                emitter.accumulator += deltaTime * emitter.emissionRate + 0.5f;
                int particles = (int) emitter.accumulator;
                if (particles == 0)
                    return;
                emitter.accumulator -= particles;
                if(emitter.random.state == 0)
                    emitter.random.InitState(1023932191);
                while (--particles >= 0)
                {
                    // Create the first particle, then instantiate the rest based on its value
                    var particle = commandBuffer.Instantiate(nativeThreadIndex, emitter.prefab);
                    float3 randomSpread = emitter.random.NextFloat3();
                    var originalScale = transformLookup[emitter.prefab];

                    var pos = new float3(transform.Position.x + randomSpread.x*3f, transform.Position.y, transform.Position.z + randomSpread.z);
                    commandBuffer.AddComponent(nativeThreadIndex, particle, new ParticleAge{deadTime = elapsedTime + emitter.particleAge});
                    commandBuffer.SetComponent(nativeThreadIndex, particle, LocalTransform.FromPositionRotationScale(
                        pos, transform.Rotation, originalScale.Scale));

                }
            }).Schedule();
            m_Barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
