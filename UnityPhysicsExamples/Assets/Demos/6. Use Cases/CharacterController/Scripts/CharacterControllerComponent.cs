using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public struct CharacterControllerComponentData : IComponentData
{
    public float3 Gravity;
    public float MovementSpeed;
    public float RotationSpeed;
    public float JumpSpeed;
    public float MaxSlope;
    public int MaxIterations;
    public float CharacterMass;
    public float ContactTolerance;
    public int AffectsPhysicsBodies;
}

public struct CharacterControllerInternalData : IComponentData
{
    public float3 RequestedMovementDirection;
    public float CurrentRotationAngle;
    public CharacterControllerUtilities.CharacterSupportState SupportedState;
    public float3 LinearVelocity;
    public Entity Entity;
}

[Serializable]
public class CharacterControllerComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    // Input
    public float3 Gravity = new float3(0.0f, -10.0f, 0.0f);
    public float MovementSpeed = 2.5f;
    public float RotationSpeed = 2.5f;
    public float JumpSpeed = 5.0f;
    public float MaxSlope = 1.57f;
    public int MaxIterations = 10;
    public float CharacterMass = 1.0f;
    public float ContactTolerance = 0.1f;
    public int AffectsPhysicsBodies = 1;

    void OnEnable() { }

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            ref PhysicsWorld world = ref World.Active.GetExistingManager<BuildPhysicsWorld>().PhysicsWorld;
            var componentData = new CharacterControllerComponentData
            {
                Gravity = Gravity,
                MovementSpeed = MovementSpeed,
                RotationSpeed = RotationSpeed,
                JumpSpeed = JumpSpeed,
                MaxSlope = MaxSlope,
                MaxIterations = MaxIterations,
                CharacterMass = CharacterMass,
                ContactTolerance = ContactTolerance,
                AffectsPhysicsBodies = AffectsPhysicsBodies,
            };
            var internalData = new CharacterControllerInternalData
            {
                Entity = entity
            };

            dstManager.AddComponentData(entity, componentData);
            dstManager.AddComponentData(entity, internalData);
        }
    }
}

[UpdateAfter(typeof(ExportPhysicsWorld))]
public class CharacterControllerSystem : JobComponentSystem
{
    [BurstCompile]
    struct CharacterControllerJob : IJobChunk
    {
        // ChunkIndex makes sure we write to different parts of these arrays, so [NativeDisableContainerSafetyRestriction] is fine.
        // Also for DeferredImpulseWriter below.
        [NativeDisableContainerSafetyRestriction] public ArchetypeChunkComponentType<CharacterControllerComponentData> CharacterControllerComponentType;
        [NativeDisableContainerSafetyRestriction] public ArchetypeChunkComponentType<CharacterControllerInternalData> CharacterControllerInternalType;
        [NativeDisableContainerSafetyRestriction] public ArchetypeChunkComponentType<Translation> TranslationType;
        [NativeDisableContainerSafetyRestriction] public ArchetypeChunkComponentType<Rotation> RotationType;
        [ReadOnly] public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;

        [DeallocateOnJobCompletion] public NativeArray<DistanceHit> DistanceHits;
        [DeallocateOnJobCompletion] public NativeArray<ColliderCastHit> CastHits;
        [DeallocateOnJobCompletion] public NativeArray<SurfaceConstraintInfo> Constraints;

        public float HorizontalInput;
        public float VerticalInput;
        public bool JumpInput;
        public float ShootXInput;
        public float DeltaTime;

        [ReadOnly]
        public PhysicsWorld PhysicsWorld;

        // Stores impulses we wish to apply to dynamic bodies the character is interacting with.
        // This is needed to avoid race conditions when 2 characters are interacting with the
        // same body at the same time.
        [NativeDisableContainerSafetyRestriction]
        public BlockStream.Writer DeferredImpulseWriter;

        public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            float3 up = math.up();

            var chunkCCData = chunk.GetNativeArray(CharacterControllerComponentType);
            var chunkCCInternalData = chunk.GetNativeArray(CharacterControllerInternalType);
            var chunkPhysicsColliderData = chunk.GetNativeArray(PhysicsColliderType);
            var chunkTranslationData = chunk.GetNativeArray(TranslationType);
            var chunkRotationData = chunk.GetNativeArray(RotationType);

            DeferredImpulseWriter.BeginForEachIndex(chunkIndex);

            for (int i = 0; i < chunk.Count; i++)
            {
                var ccComponentData = chunkCCData[i];
                var ccInternalData = chunkCCInternalData[i];
                var collider = chunkPhysicsColliderData[i];
                var position = chunkTranslationData[i];
                var rotation = chunkRotationData[i];

                // Create a copy of capsule collider
                Unity.Physics.Collider* capsuleColliderForQueries;
                {
                    Unity.Physics.Collider* colliderPtr = collider.ColliderPtr;

                    // Only capsule controller is supported
                    Assert.IsTrue(colliderPtr->Type == ColliderType.Capsule);

                    byte* copiedColliderMemory = stackalloc byte[colliderPtr->MemorySize];
                    capsuleColliderForQueries = (Unity.Physics.Collider*)(copiedColliderMemory);
                    UnsafeUtility.MemCpy(capsuleColliderForQueries, colliderPtr, colliderPtr->MemorySize);
                    capsuleColliderForQueries->Filter = CollisionFilter.Default;
                }

                // Tau and damping for character solver
                const float tau = 0.4f;
                const float damping = 0.9f;

                // Check support
                RigidTransform transform = new RigidTransform
                {
                    pos = position.Value,
                    rot = rotation.Value
                };

                CharacterControllerUtilities.CheckSupport(PhysicsWorld, DeltaTime, transform, -up, ccComponentData.MaxSlope,
                    ccComponentData.ContactTolerance, capsuleColliderForQueries, ref Constraints, ref DistanceHits, out ccInternalData.SupportedState);

                // petarm.todo: incorporate support plane's velocity and project input onto it

                // User input
                HandleUserInput(ref ccComponentData, ref ccInternalData, ref ccInternalData.LinearVelocity);

                // World collision + integrate
                CharacterControllerUtilities.CollideAndIntegrate(PhysicsWorld, DeltaTime, ccComponentData.MaxIterations, up, ccComponentData.Gravity,
                    ccComponentData.CharacterMass, tau, damping, ccComponentData.AffectsPhysicsBodies > 0, capsuleColliderForQueries,
                    ref DistanceHits, ref CastHits, ref Constraints,
                    ref transform, ref ccInternalData.LinearVelocity, ref DeferredImpulseWriter);

                // Write back and orientation integration
                position.Value = transform.pos;
                rotation.Value = quaternion.AxisAngle(up, ccInternalData.CurrentRotationAngle);

                // Write back to chunk data
                {
                    chunkCCData[i] = ccComponentData;
                    chunkCCInternalData[i] = ccInternalData;
                    chunkTranslationData[i] = position;
                    chunkRotationData[i] = rotation;
                }
            }

            DeferredImpulseWriter.EndForEachIndex();
        }

        private void HandleUserInput(ref CharacterControllerComponentData ccComponentData, ref CharacterControllerInternalData ccInternalData, ref float3 linearVelocity)
        {
            float3 up = math.up();

            // Movement and jumping
            bool shouldJump = false;
            {
                float3 forward = math.forward(quaternion.identity);
                float3 right = math.cross(up, forward);

                // petarm.todo: hook input through component data, not here
                float horizontal = HorizontalInput;
                float vertical = VerticalInput;
                bool jumpRequested = JumpInput;
                bool haveInput = (math.abs(horizontal) > float.Epsilon) || (math.abs(vertical) > float.Epsilon);
                if (haveInput)
                {
                    float3 originalMovement = forward * vertical + right * horizontal;
                    float3 actualMovement = math.rotate(quaternion.AxisAngle(up, ccInternalData.CurrentRotationAngle), originalMovement);
                    ccInternalData.RequestedMovementDirection = math.normalize(actualMovement);
                }
                else
                {
                    ccInternalData.RequestedMovementDirection = float3.zero;
                }
                shouldJump = jumpRequested && ccInternalData.SupportedState != CharacterControllerUtilities.CharacterSupportState.Unsupported;
            }

            // Turning
            {
                float horizontal = ShootXInput;
                bool haveInput = (math.abs(horizontal) > float.Epsilon);
                if (haveInput)
                {
                    ccInternalData.CurrentRotationAngle += horizontal * ccComponentData.RotationSpeed * DeltaTime;
                }
            }

            // Change velocity and apply gravity
            {
                // Assert that up has 1 in exactly one of the axis and other 2 are 0
                Assert.IsTrue(up.Equals(new float3(1, 0, 0)) || up.Equals(new float3(0, 1, 0)) || up.Equals(new float3(0, 0, 1)));

                // Change velocity but keep the Up component
                linearVelocity = linearVelocity * up + ccInternalData.RequestedMovementDirection * ccComponentData.MovementSpeed;
                if (shouldJump)
                {
                    linearVelocity += ccComponentData.JumpSpeed * up;
                }
                else if (ccInternalData.SupportedState != CharacterControllerUtilities.CharacterSupportState.Supported)
                {
                    linearVelocity += ccComponentData.Gravity * DeltaTime;
                }
            }
        }
    }

    [BurstCompile]
    struct ApplyDefferedPhysicsUpdatesJob : IJob
    {
        // Chunks can be deallocated at this point
        [DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;

        public BlockStream.Reader DeferredImpulseReader;

        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityData;
        public ComponentDataFromEntity<PhysicsMass> PhysicsMassData;
        public ComponentDataFromEntity<Translation> TranslationData;
        public ComponentDataFromEntity<Rotation> RotationData;

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
                var impulse = DeferredImpulseReader.Read<DeferredCharacterControllerImpulse>();
                while (DeferredImpulseReader.RemainingItemCount == 0 && index < maxIndex)
                {
                    DeferredImpulseReader.BeginForEachIndex(index++);
                }

                PhysicsVelocity pv = PhysicsVelocityData[impulse.Entity];
                PhysicsMass pm = PhysicsMassData[impulse.Entity];
                Translation t = TranslationData[impulse.Entity];
                Rotation r = RotationData[impulse.Entity];

                // Don't apply on kinematic bodies
                if (pm.InverseMass > 0.0f)
                {
                    // Apply impulse
                    pv.ApplyImpulse(pm, t, r, impulse.Impulse, impulse.Point);

                    // Write back
                    PhysicsVelocityData[impulse.Entity] = pv;
                }
            }
        }
    }

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    ExportPhysicsWorld m_ExportPhysicsWorldSystem;
    EndFramePhysicsSystem m_EndFramePhysicsSystem;

    ComponentGroup m_CharacterControllersGroup;

    protected override void OnCreateManager()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateManager<BuildPhysicsWorld>();
        m_ExportPhysicsWorldSystem = World.GetOrCreateManager<ExportPhysicsWorld>();
        m_EndFramePhysicsSystem = World.GetOrCreateManager<EndFramePhysicsSystem>();

        var query = new EntityArchetypeQuery
        {
            All = new ComponentType[] { typeof(CharacterControllerComponentData), typeof(CharacterControllerInternalData),
                typeof(PhysicsCollider), typeof(Translation), typeof(Rotation) }
        };
        m_CharacterControllersGroup = GetComponentGroup(query);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        m_ExportPhysicsWorldSystem.FinalJobHandle.Complete();

        var chunks = m_CharacterControllersGroup.CreateArchetypeChunkArray(Allocator.TempJob);

        var ccComponentType = GetArchetypeChunkComponentType<CharacterControllerComponentData>();
        var ccInternalType = GetArchetypeChunkComponentType<CharacterControllerInternalData>();
        var physicsColliderType = GetArchetypeChunkComponentType<PhysicsCollider>();
        var translationType = GetArchetypeChunkComponentType<Translation>();
        var rotationType = GetArchetypeChunkComponentType<Rotation>();

        BlockStream deferredImpulses = new BlockStream(chunks.Length, 0xCA37B9F2);

        // Maximum number of hits character controller can store in world queries
        const int maxQueryHits = 128;

        var ccJob = new CharacterControllerJob()
        {
            // Archetypes
            CharacterControllerComponentType = ccComponentType,
            CharacterControllerInternalType = ccInternalType,
            PhysicsColliderType = physicsColliderType,
            TranslationType = translationType,
            RotationType = rotationType,
            // Input
            HorizontalInput = Input.GetAxis("Horizontal"),
            VerticalInput = Input.GetAxis("Vertical"),
            JumpInput = Input.GetButtonDown("Jump"),
            ShootXInput = Input.GetAxis("ShootX"),
            DeltaTime = Time.fixedDeltaTime,
            PhysicsWorld = m_BuildPhysicsWorldSystem.PhysicsWorld,
            DeferredImpulseWriter = deferredImpulses,
            // Memory
            DistanceHits = new NativeArray<DistanceHit>(maxQueryHits, Allocator.TempJob),
            CastHits = new NativeArray<ColliderCastHit>(maxQueryHits, Allocator.TempJob),
            Constraints = new NativeArray<SurfaceConstraintInfo>(2 * maxQueryHits, Allocator.TempJob)
        };

        inputDeps = ccJob.Schedule(m_CharacterControllersGroup, inputDeps);

        var applyJob = new ApplyDefferedPhysicsUpdatesJob()
        {
            Chunks = chunks,
            DeferredImpulseReader = deferredImpulses,
            PhysicsVelocityData = GetComponentDataFromEntity<PhysicsVelocity>(),
            PhysicsMassData = GetComponentDataFromEntity<PhysicsMass>(),
            TranslationData = GetComponentDataFromEntity<Translation>(),
            RotationData = GetComponentDataFromEntity<Rotation>()
        };

        inputDeps = applyJob.Schedule(inputDeps);
        var disposeHandle = deferredImpulses.ScheduleDispose(inputDeps);

        // Must finish all jobs before physics step end
        m_EndFramePhysicsSystem.HandlesToWaitFor.Add(disposeHandle);

        return inputDeps;
    }
}
