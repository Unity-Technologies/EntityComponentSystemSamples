using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Collections;
using Unity.NetCode;
using Unity.NetCode.HostMigration;
using Unity.Physics.Systems;

namespace Samples.HelloNetcode
{
    public struct CharacterControllerConfig : IComponentData
    {
        public float MoveSpeed;
        public float JumpSpeed;
        public float Gravity;
    }

    public struct Character : IComponentData
    {
        public Entity ControllerConfig;

        [GhostField(Quantization = 1000)]
        public float3 Velocity;
        [GhostField]
        public byte OnGround;
        [GhostField]
        public NetworkTick JumpStart;
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsInitializeGroup))]
    [BurstCompile]
    partial struct CharacterControllerSystem : ISystem
    {
        const float k_DefaultTau = 0.4f;
        const float k_DefaultDamping = 0.9f;
        const float k_DefaultSkinWidth = 0f;
        const float k_DefaultContactTolerance = 0.1f;
        const float k_DefaultMaxSlope = 60f;
        const float k_DefaultMaxMovementSpeed = 10f;
        const int k_DefaultMaxIterations = 10;
        const float k_DefaultMass = 1f;

        private ProfilerMarker m_MarkerGroundCheck;
        private ProfilerMarker m_MarkerStep;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<Character>();

            m_MarkerGroundCheck = new Unity.Profiling.ProfilerMarker("GroundCheck");
            m_MarkerStep = new Unity.Profiling.ProfilerMarker("Step");
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            if (!HasPhysicsWorldBeenInitialized(physicsWorldSingleton))
            {
                return;
            }
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();

            var isMigratedLookup = SystemAPI.GetComponentLookup<IsMigrated>();

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (characterTransform, characterAutoCommandTarget, character, characterVelocity, characterInput, characterOwner, characterEntity) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<AutoCommandTarget>, RefRW<Character>, RefRW<PhysicsVelocity>, RefRO<CharacterControllerPlayerInput>, RefRO<GhostOwner>>().WithAll<Simulate>().WithEntityAccess())
            {
                if (!characterAutoCommandTarget.ValueRO.Enabled)
                {
                    characterVelocity.ValueRW.Linear = float3.zero;
                    continue;
                }

                if (isMigratedLookup.HasComponent(characterEntity))
                {
                    var characterPrefabQuery = SystemAPI.QueryBuilder().WithAll<CharacterControllerConfig, Prefab>().Build();
                    if (characterPrefabQuery.CalculateEntityCount() == 1)
                    {
                        UnityEngine.Debug.Log($"Reconnecting character controller config entity was:{character.ValueRO.ControllerConfig} is now:{characterPrefabQuery.GetSingletonEntity()}");
                        character.ValueRW.ControllerConfig = characterPrefabQuery.GetSingletonEntity();
                        commandBuffer.RemoveComponent<IsMigrated>(characterEntity);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"Failed to reconnect character controller config entity was:{character.ValueRO.ControllerConfig}");
                    }
                }

                var controllerConfig = SystemAPI.GetComponent<CharacterControllerConfig>(character.ValueRO.ControllerConfig);
                var controllerCollider = SystemAPI.GetComponent<PhysicsCollider>(character.ValueRO.ControllerConfig);

                // Character step input
                CharacterControllerUtilities.CharacterControllerStepInput stepInput = new CharacterControllerUtilities.CharacterControllerStepInput
                {
                    PhysicsWorldSingleton = physicsWorldSingleton,
                    DeltaTime = SystemAPI.Time.DeltaTime,
                    Up = new float3(0, 1, 0),
                    Gravity = new float3(0, -controllerConfig.Gravity, 0),
                    MaxIterations = k_DefaultMaxIterations,
                    Tau = k_DefaultTau,
                    Damping = k_DefaultDamping,
                    SkinWidth = k_DefaultSkinWidth,
                    ContactTolerance = k_DefaultContactTolerance,
                    MaxSlope = math.radians(k_DefaultMaxSlope),
                    RigidBodyIndex = physicsWorldSingleton.PhysicsWorld.GetRigidBodyIndex(characterEntity),
                    CurrentVelocity = character.ValueRO.Velocity,
                    MaxMovementSpeed = k_DefaultMaxMovementSpeed
                };

                //Using local position here is fine, because the character controller does not have any parent.
                //Using the Position is wrong because it is not up to date. (the LocalTransform is synchronized but
                //the world transform isn't).
                RigidTransform ccTransform = new RigidTransform()
                {
                    pos = characterTransform.ValueRO.Position,
                    rot = quaternion.identity
                };

                m_MarkerGroundCheck.Begin();
                CharacterControllerUtilities.CheckSupport(
                    in physicsWorldSingleton,
                    ref controllerCollider,
                    stepInput,
                    ccTransform,
                    out CharacterControllerUtilities.CharacterSupportState supportState,
                    out _,
                    out _);
                m_MarkerGroundCheck.End();

                float2 input = characterInput.ValueRO.Movement;
                float3 wantedMove = new float3(input.x, 0, input.y) * controllerConfig.MoveSpeed * SystemAPI.Time.DeltaTime;

                var characterRotation = quaternion.RotateY(characterInput.ValueRO.Yaw);
                // The character controllers yaw rotation can always be set, even when in the air:
                characterTransform.ValueRW.Rotation = characterRotation;

                // Wanted movement is relative to camera
                wantedMove = math.rotate(characterRotation, wantedMove);

                float3 wantedVelocity = wantedMove / SystemAPI.Time.DeltaTime;
                wantedVelocity.y = character.ValueRO.Velocity.y;

                if (supportState == CharacterControllerUtilities.CharacterSupportState.Supported)
                {
                    character.ValueRW.JumpStart = NetworkTick.Invalid;
                    character.ValueRW.OnGround = 1;
                    character.ValueRW.Velocity = wantedVelocity;
                    // Allow jump and stop falling when grounded
                    if (characterInput.ValueRO.Jump.IsSet)
                    {
                        character.ValueRW.Velocity.y = controllerConfig.JumpSpeed;
                        character.ValueRW.JumpStart = networkTime.ServerTick;
                    }
                    else
                        character.ValueRW.Velocity.y = 0;
                }
                else
                {
                    character.ValueRW.OnGround = 0;
                    // Free fall
                    character.ValueRW.Velocity.y -= controllerConfig.Gravity * SystemAPI.Time.DeltaTime;
                }

                m_MarkerStep.Begin();
                // Ok because affect bodies is false so no impulses are written
                NativeStream.Writer deferredImpulseWriter = default;
                CharacterControllerUtilities.CollideAndIntegrate(stepInput, k_DefaultMass, false, ref controllerCollider, ref ccTransform, ref character.ValueRW.Velocity, ref deferredImpulseWriter);
                m_MarkerStep.End();

                // Set the physics velocity and let physics move the kinematic object based on that
                characterVelocity.ValueRW.Linear = (ccTransform.pos - characterTransform.ValueRO.Position) / SystemAPI.Time.DeltaTime;
            }
            commandBuffer.Playback(state.EntityManager);
        }

        /// <summary>
        /// As we run before <see cref="PhysicsInitializeGroup"/> it is possible to execute before any physics bodies
        /// has been initialized.
        ///
        /// There may be a better way to do this.
        /// </summary>
        static bool HasPhysicsWorldBeenInitialized(PhysicsWorldSingleton physicsWorldSingleton)
        {
            return physicsWorldSingleton.PhysicsWorld.Bodies is { IsCreated: true, Length: > 0 };
        }
    }
}
