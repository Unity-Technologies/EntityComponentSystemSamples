using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.GraphicsIntegration;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine.Assertions;

namespace CharacterController
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct CharacterControllerSystem : ISystem
    {
        const float k_DefaultTau = 0.4f;
        const float k_DefaultDamping = 0.9f;

        EntityQuery characterControllerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            characterControllerQuery = SystemAPI.QueryBuilder()
                .WithAllRW<CharacterController, CharacterControllerInternal>()
                .WithAllRW<LocalTransform>()
                .WithAll<PhysicsCollider>().Build();
            state.RequireForUpdate(characterControllerQuery);
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var chunks = characterControllerQuery.ToArchetypeChunkArray(Allocator.TempJob);
            var deferredImpulses = new NativeStream(chunks.Length, Allocator.TempJob);
            var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            float dt = SystemAPI.Time.DeltaTime;
            var ccJob = new CharacterControllerJob
            {
                CharacterControllerHandle = SystemAPI.GetComponentTypeHandle<CharacterController>(true),
                CharacterControllerInternalHandle = SystemAPI.GetComponentTypeHandle<CharacterControllerInternal>(),
                PhysicsColliderHandle = SystemAPI.GetComponentTypeHandle<PhysicsCollider>(true),
                LocalTransformHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(),
                StatefulCollisionEventHandle = SystemAPI.GetBufferTypeHandle<StatefulCollisionEvent>(),
                StatefulTriggerEventHandle = SystemAPI.GetBufferTypeHandle<StatefulTriggerEvent>(),

                DeltaTime = dt,
                PhysicsWorldSingleton = physicsWorldSingleton,
                DeferredImpulseWriter = deferredImpulses.AsWriter()
            };

            state.Dependency = ccJob.ScheduleParallel(characterControllerQuery, state.Dependency);

            var smoothedCharacterControllerQuery = SystemAPI.QueryBuilder()
                .WithAllRW<CharacterControllerInternal, PhysicsGraphicalSmoothing>().Build();
            var copyVelocitiesHandle =
                new CopyVelocityToGraphicalSmoothingJob().ScheduleParallel(smoothedCharacterControllerQuery,
                    state.Dependency);

            var applyJob = new ApplyDefferedPhysicsUpdatesJob()
            {
                Chunks = chunks,
                DeferredImpulseReader = deferredImpulses.AsReader(),
                PhysicsVelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(),
                PhysicsMassLookup = SystemAPI.GetComponentLookup<PhysicsMass>(true),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            };

            state.Dependency = applyJob.Schedule(state.Dependency);
            state.Dependency = deferredImpulses.Dispose(state.Dependency);
            state.Dependency = JobHandle.CombineDependencies(state.Dependency, copyVelocitiesHandle);
        }

        // override the behavior of CopyPhysicsVelocityToSmoothing
        [BurstCompile]
        partial struct CopyVelocityToGraphicalSmoothingJob : IJobEntity
        {
            public void Execute(in CharacterControllerInternal ccInternal,
                ref PhysicsGraphicalSmoothing smoothing)
            {
                smoothing.CurrentVelocity = ccInternal.Velocity;
            }
        }


        [BurstCompile]
        struct CharacterControllerJob : IJobChunk
        {
            public float DeltaTime;

            [ReadOnly] public PhysicsWorldSingleton PhysicsWorldSingleton;
            public ComponentTypeHandle<CharacterControllerInternal> CharacterControllerInternalHandle;
            public ComponentTypeHandle<LocalTransform> LocalTransformHandle;
            public BufferTypeHandle<StatefulCollisionEvent> StatefulCollisionEventHandle;
            public BufferTypeHandle<StatefulTriggerEvent> StatefulTriggerEventHandle;
            [ReadOnly] public ComponentTypeHandle<CharacterController> CharacterControllerHandle;
            [ReadOnly] public ComponentTypeHandle<PhysicsCollider> PhysicsColliderHandle;

            // Stores impulses we wish to apply to dynamic bodies the character is interacting with.
            // This is needed to avoid race conditions when 2 characters are interacting with the
            // same body at the same time.
            [NativeDisableParallelForRestriction] public NativeStream.Writer DeferredImpulseWriter;

            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                Assert.IsFalse(useEnabledMask);
                var chunkCCData = chunk.GetNativeArray(ref CharacterControllerHandle);
                var chunkCCInternalData = chunk.GetNativeArray(ref CharacterControllerInternalHandle);
                var chunkPhysicsColliderData = chunk.GetNativeArray(ref PhysicsColliderHandle);
                var chunkLocalTransformData = chunk.GetNativeArray(ref LocalTransformHandle);
                var hasChunkCollisionEventBufferType = chunk.Has(ref StatefulCollisionEventHandle);
                var hasChunkTriggerEventBufferType = chunk.Has(ref StatefulTriggerEventHandle);

                BufferAccessor<StatefulCollisionEvent> collisionEventBuffers = default;
                BufferAccessor<StatefulTriggerEvent> triggerEventBuffers = default;
                if (hasChunkCollisionEventBufferType)
                {
                    collisionEventBuffers = chunk.GetBufferAccessor(ref StatefulCollisionEventHandle);
                }

                if (hasChunkTriggerEventBufferType)
                {
                    triggerEventBuffers = chunk.GetBufferAccessor(ref StatefulTriggerEventHandle);
                }

                DeferredImpulseWriter.BeginForEachIndex(unfilteredChunkIndex);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var ccComponentData = chunkCCData[i];
                    var ccInternalData = chunkCCInternalData[i];
                    var collider = chunkPhysicsColliderData[i];
                    var localTransform = chunkLocalTransformData[i];

                    DynamicBuffer<StatefulCollisionEvent> collisionEventBuffer = default;
                    DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer = default;

                    if (hasChunkCollisionEventBufferType)
                    {
                        collisionEventBuffer = collisionEventBuffers[i];
                    }

                    if (hasChunkTriggerEventBufferType)
                    {
                        triggerEventBuffer = triggerEventBuffers[i];
                    }

                    // Collision filter must be valid
                    if (!collider.IsValid || collider.Value.Value.GetCollisionFilter().IsEmpty)
                        continue;

                    var up = math.select(math.up(), -math.normalize(ccComponentData.Gravity),
                        math.lengthsq(ccComponentData.Gravity) > 0f);

                    // Character step input
                    Util.StepInput stepInput = new Util.StepInput
                    {
                        PhysicsWorldSingleton = PhysicsWorldSingleton,
                        DeltaTime = DeltaTime,
                        Up = up,
                        Gravity = ccComponentData.Gravity,
                        MaxIterations = ccComponentData.MaxIterations,
                        Tau = k_DefaultTau,
                        Damping = k_DefaultDamping,
                        SkinWidth = ccComponentData.SkinWidth,
                        ContactTolerance = ccComponentData.ContactTolerance,
                        MaxSlope = ccComponentData.MaxSlope,
                        RigidBodyIndex = PhysicsWorldSingleton.PhysicsWorld.GetRigidBodyIndex(ccInternalData.Entity),
                        CurrentVelocity = ccInternalData.Velocity.Linear,
                        MaxMovementSpeed = ccComponentData.MaxMovementSpeed
                    };

                    // Character transform
                    RigidTransform transform = new RigidTransform
                    {
                        pos = localTransform.Position,
                        rot = localTransform.Rotation
                    };

                    NativeList<StatefulCollisionEvent> currentFrameCollisionEvents = default;
                    NativeList<StatefulTriggerEvent> currentFrameTriggerEvents = default;

                    if (ccComponentData.RaiseCollisionEvents != 0)
                    {
                        currentFrameCollisionEvents = new NativeList<StatefulCollisionEvent>(Allocator.Temp);
                    }

                    if (ccComponentData.RaiseTriggerEvents != 0)
                    {
                        currentFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(Allocator.Temp);
                    }

                    // Check support
                    Util.CheckSupport(in PhysicsWorldSingleton, ref collider, stepInput, transform,
                        out ccInternalData.SupportedState, out float3 surfaceNormal, out float3 surfaceVelocity,
                        currentFrameCollisionEvents);

                    // User input
                    float3 desiredVelocity = ccInternalData.Velocity.Linear;
                    HandleUserInput(ccComponentData, stepInput.Up, surfaceVelocity, ref ccInternalData,
                        ref desiredVelocity);

                    // Calculate actual velocity with respect to surface
                    if (ccInternalData.SupportedState == Util.CharacterSupportState.Supported)
                    {
                        CalculateMovement(ccInternalData.CurrentRotationAngle, stepInput.Up, ccInternalData.IsJumping,
                            ccInternalData.Velocity.Linear, desiredVelocity, surfaceNormal, surfaceVelocity,
                            out ccInternalData.Velocity.Linear);
                    }
                    else
                    {
                        ccInternalData.Velocity.Linear = desiredVelocity;
                    }

                    // World collision + integrate
                    Util.CollideAndIntegrate(stepInput, ccComponentData.CharacterMass,
                        ccComponentData.AffectsPhysicsBodies != 0,
                        ref collider, ref transform, ref ccInternalData.Velocity.Linear, ref DeferredImpulseWriter,
                        currentFrameCollisionEvents, currentFrameTriggerEvents);

                    // Update collision event status
                    if (currentFrameCollisionEvents.IsCreated)
                    {
                        UpdateCollisionEvents(currentFrameCollisionEvents, collisionEventBuffer);
                    }

                    if (currentFrameTriggerEvents.IsCreated)
                    {
                        UpdateTriggerEvents(currentFrameTriggerEvents, triggerEventBuffer);
                    }

                    // Write back and orientation integration

                    localTransform.Position = transform.pos;
                    localTransform.Rotation = quaternion.AxisAngle(up, ccInternalData.CurrentRotationAngle);


                    // Write back to chunk data
                    {
                        chunkCCInternalData[i] = ccInternalData;

                        chunkLocalTransformData[i] = localTransform;
                    }
                }

                DeferredImpulseWriter.EndForEachIndex();
            }

            private void HandleUserInput(CharacterController cc, float3 up,
                float3 surfaceVelocity,
                ref CharacterControllerInternal ccInternal, ref float3 linearVelocity)
            {
                // Reset jumping state and unsupported velocity
                if (ccInternal.SupportedState == Util.CharacterSupportState.Supported)
                {
                    ccInternal.IsJumping = false;
                    ccInternal.UnsupportedVelocity = float3.zero;
                }

                // Movement and jumping
                bool shouldJump = false;
                float3 requestedMovementDirection = float3.zero;
                {
                    float3 forward = math.forward(quaternion.identity);
                    float3 right = math.cross(up, forward);

                    float horizontal = ccInternal.Input.Movement.x;
                    float vertical = ccInternal.Input.Movement.y;
                    bool jumpRequested = ccInternal.Input.Jumped != 0;
                    ccInternal.Input.Jumped = 0; // "consume" the event
                    bool haveInput = (math.abs(horizontal) > float.Epsilon) || (math.abs(vertical) > float.Epsilon);
                    if (haveInput)
                    {
                        float3 localSpaceMovement = forward * vertical + right * horizontal;
                        float3 worldSpaceMovement =
                            math.rotate(quaternion.AxisAngle(up, ccInternal.CurrentRotationAngle),
                                localSpaceMovement);
                        requestedMovementDirection = math.normalize(worldSpaceMovement);
                    }

                    shouldJump = jumpRequested && ccInternal.SupportedState == Util.CharacterSupportState.Supported;
                }

                // Turning
                {
                    float horizontal = ccInternal.Input.Looking.x;
                    bool haveInput = (math.abs(horizontal) > float.Epsilon);
                    if (haveInput)
                    {
                        var userRotationSpeed = horizontal * cc.RotationSpeed;
                        ccInternal.Velocity.Angular = -userRotationSpeed * up;
                        ccInternal.CurrentRotationAngle += userRotationSpeed * DeltaTime;
                    }
                    else
                    {
                        ccInternal.Velocity.Angular = 0f;
                    }
                }

                // Apply input velocities
                {
                    if (shouldJump)
                    {
                        // Add jump speed to surface velocity and make character unsupported
                        ccInternal.IsJumping = true;
                        ccInternal.SupportedState = Util.CharacterSupportState.Unsupported;
                        ccInternal.UnsupportedVelocity = surfaceVelocity + cc.JumpUpwardsSpeed * up;
                    }
                    else if (ccInternal.SupportedState != Util.CharacterSupportState.Supported)
                    {
                        // Apply gravity
                        ccInternal.UnsupportedVelocity += cc.Gravity * DeltaTime;
                    }

                    // If unsupported then keep jump and surface momentum
                    linearVelocity = requestedMovementDirection * cc.MovementSpeed +
                        (ccInternal.SupportedState != Util.CharacterSupportState.Supported
                            ? ccInternal.UnsupportedVelocity
                            : float3.zero);
                }
            }

            private void CalculateMovement(float currentRotationAngle, float3 up, bool isJumping,
                float3 currentVelocity, float3 desiredVelocity, float3 surfaceNormal, float3 surfaceVelocity,
                out float3 linearVelocity)
            {
                float3 forward = math.forward(quaternion.AxisAngle(up, currentRotationAngle));

                quaternion surfaceFrame;

                float3 binorm;
                {
                    binorm = math.cross(forward, up);
                    binorm = math.normalize(binorm);

                    float3 tangent = math.cross(binorm, surfaceNormal);
                    tangent = math.normalize(tangent);

                    binorm = math.cross(tangent, surfaceNormal);
                    binorm = math.normalize(binorm);

                    surfaceFrame = new quaternion(new float3x3(binorm, tangent, surfaceNormal));
                }

                float3 relative = currentVelocity - surfaceVelocity;

                relative = math.rotate(math.inverse(surfaceFrame), relative);

                float3 diff;
                {
                    float3 sideVec = math.cross(forward, up);
                    float fwd = math.dot(desiredVelocity, forward);
                    float side = math.dot(desiredVelocity, sideVec);
                    float len = math.length(desiredVelocity);
                    float3 desiredVelocitySF = new float3(-side, -fwd, 0.0f);
                    desiredVelocitySF = math.normalizesafe(desiredVelocitySF, float3.zero);
                    desiredVelocitySF *= len;
                    diff = desiredVelocitySF - relative;
                }

                relative += diff;

                linearVelocity = math.rotate(surfaceFrame, relative) + surfaceVelocity +
                    (isJumping ? math.dot(desiredVelocity, up) * up : float3.zero);
            }

            private void UpdateTriggerEvents(NativeList<StatefulTriggerEvent> triggerEvents,
                DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer)
            {
                var previousFrameTriggerEvents =
                    new NativeList<StatefulTriggerEvent>(triggerEventBuffer.Length, Allocator.Temp);

                for (int i = 0; i < triggerEventBuffer.Length; i++)
                {
                    var triggerEvent = triggerEventBuffer[i];
                    if (triggerEvent.State != StatefulEventState.Exit)
                    {
                        previousFrameTriggerEvents.Add(triggerEvent);
                    }
                }

                var eventsWithState = new NativeList<StatefulTriggerEvent>(triggerEvents.Length, Allocator.Temp);

                StatefulSimulationEventBuffers<StatefulTriggerEvent>.GetStatefulEvents(previousFrameTriggerEvents,
                    triggerEvents, eventsWithState);

                triggerEventBuffer.Clear();

                for (int i = 0; i < eventsWithState.Length; i++)
                {
                    triggerEventBuffer.Add(eventsWithState[i]);
                }
            }

            private void UpdateCollisionEvents(NativeList<StatefulCollisionEvent> collisionEvents,
                DynamicBuffer<StatefulCollisionEvent> collisionEventBuffer)
            {
                var previousFrameCollisionEvents =
                    new NativeList<StatefulCollisionEvent>(collisionEventBuffer.Length, Allocator.Temp);

                for (int i = 0; i < collisionEventBuffer.Length; i++)
                {
                    var collisionEvent = collisionEventBuffer[i];
                    if (collisionEvent.State != StatefulEventState.Exit)
                    {
                        previousFrameCollisionEvents.Add(collisionEvent);
                    }
                }

                var eventsWithState = new NativeList<StatefulCollisionEvent>(collisionEvents.Length, Allocator.Temp);
                StatefulSimulationEventBuffers<StatefulCollisionEvent>.GetStatefulEvents(previousFrameCollisionEvents,
                    collisionEvents, eventsWithState);

                collisionEventBuffer.Clear();
                for (int i = 0; i < eventsWithState.Length; i++)
                {
                    collisionEventBuffer.Add(eventsWithState[i]);
                }
            }
        }

        [BurstCompile]
        struct ApplyDefferedPhysicsUpdatesJob : IJob
        {
            // Chunks can be deallocated at this point
            [DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;
            public NativeStream.Reader DeferredImpulseReader;
            public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;
            [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

            public void Execute()
            {
                int index = 0;
                int maxIndex = DeferredImpulseReader.ForEachCount;
                DeferredImpulseReader.BeginForEachIndex(index++);
                while (DeferredImpulseReader.RemainingItemCount == 0 && index < maxIndex)
                {
                    DeferredImpulseReader.BeginForEachIndex(index++);
                }

                while (DeferredImpulseReader.RemainingItemCount > 0)
                {
                    // Read the data
                    var impulse = DeferredImpulseReader.Read<DeferredCharacterImpulse>();
                    while (DeferredImpulseReader.RemainingItemCount == 0 && index < maxIndex)
                    {
                        DeferredImpulseReader.BeginForEachIndex(index++);
                    }

                    PhysicsVelocity pv = PhysicsVelocityLookup[impulse.Entity];
                    PhysicsMass pm = PhysicsMassLookup[impulse.Entity];
                    LocalTransform t = LocalTransformLookup[impulse.Entity];

                    // Don't apply on kinematic bodies
                    if (pm.InverseMass > 0.0f)
                    {
                        // Apply impulse

                        pv.ApplyImpulse(pm, t.Position, t.Rotation, impulse.Impulse, impulse.Point);

                        // Write back
                        PhysicsVelocityLookup[impulse.Entity] = pv;
                    }
                }
            }
        }
    }
}
