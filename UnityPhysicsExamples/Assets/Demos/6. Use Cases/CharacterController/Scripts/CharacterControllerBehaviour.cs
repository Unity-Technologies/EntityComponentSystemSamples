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

[Serializable]
public struct CharacterControllerUserInputData : IComponentData
{
    public float HorizontalInput;
    public float VerticalInput;
    public int JumpInput;
    public float ShootXInput;
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
public class CharacterControllerBehaviour : MonoBehaviour, IConvertGameObjectToEntity
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
            ref PhysicsWorld world = ref World.Active.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
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
            var userInputData = new CharacterControllerUserInputData();
            var internalData = new CharacterControllerInternalData
            {
                Entity = entity
            };

            dstManager.AddComponentData(entity, componentData);
            dstManager.AddComponentData(entity, userInputData);
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
        [NativeDisableContainerSafetyRestriction] public ArchetypeChunkComponentType<CharacterControllerInternalData> CharacterControllerInternalType;
        [NativeDisableContainerSafetyRestriction] public ArchetypeChunkComponentType<Translation> TranslationType;
        [NativeDisableContainerSafetyRestriction] public ArchetypeChunkComponentType<Rotation> RotationType;
        [ReadOnly] public ArchetypeChunkComponentType<CharacterControllerComponentData> CharacterControllerComponentType;
        [ReadOnly] public ArchetypeChunkComponentType<CharacterControllerUserInputData> CharacterControllerUserInputType;
        [ReadOnly] public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;

        [DeallocateOnJobCompletion] public NativeArray<DistanceHit> DistanceHits;
        [DeallocateOnJobCompletion] public NativeArray<ColliderCastHit> CastHits;
        [DeallocateOnJobCompletion] public NativeArray<SurfaceConstraintInfo> Constraints;

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
            var chunkCCInputData = chunk.GetNativeArray(CharacterControllerUserInputType);
            var chunkCCInternalData = chunk.GetNativeArray(CharacterControllerInternalType);
            var chunkPhysicsColliderData = chunk.GetNativeArray(PhysicsColliderType);
            var chunkTranslationData = chunk.GetNativeArray(TranslationType);
            var chunkRotationData = chunk.GetNativeArray(RotationType);

            DeferredImpulseWriter.BeginForEachIndex(chunkIndex);

            for (int i = 0; i < chunk.Count; i++)
            {
                var ccComponentData = chunkCCData[i];
                var ccInputData = chunkCCInputData[i];
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
                HandleUserInput(ccInputData, ccComponentData, ref ccInternalData, ref ccInternalData.LinearVelocity);

                // World collision + integrate
                CharacterControllerUtilities.CollideAndIntegrate(PhysicsWorld, DeltaTime, ccComponentData.MaxIterations, up, ccComponentData.Gravity,
                    ccComponentData.CharacterMass, tau, damping, ccComponentData.MaxSlope, ccComponentData.AffectsPhysicsBodies > 0, capsuleColliderForQueries,
                    ref DistanceHits, ref CastHits, ref Constraints,
                    ref transform, ref ccInternalData.LinearVelocity, ref DeferredImpulseWriter);

                // Write back and orientation integration
                position.Value = transform.pos;
                rotation.Value = quaternion.AxisAngle(up, ccInternalData.CurrentRotationAngle);

                // Write back to chunk data
                {
                    chunkCCInternalData[i] = ccInternalData;
                    chunkTranslationData[i] = position;
                    chunkRotationData[i] = rotation;
                }
            }

            DeferredImpulseWriter.EndForEachIndex();
        }

        private void HandleUserInput(CharacterControllerUserInputData ccInputData, CharacterControllerComponentData ccComponentData,
            ref CharacterControllerInternalData ccInternalData, ref float3 linearVelocity)
        {
            float3 up = math.up();

            // Movement and jumping
            bool shouldJump = false;
            {
                float3 forward = math.forward(quaternion.identity);
                float3 right = math.cross(up, forward);

                float horizontal = ccInputData.HorizontalInput;
                float vertical = ccInputData.VerticalInput;
                bool jumpRequested = ccInputData.JumpInput > 0;
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
                shouldJump = jumpRequested && ccInternalData.SupportedState == CharacterControllerUtilities.CharacterSupportState.Supported;
            }

            // Turning
            {
                float horizontal = ccInputData.ShootXInput;
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

    EntityQuery m_CharacterControllersGroup;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_ExportPhysicsWorldSystem = World.GetOrCreateSystem<ExportPhysicsWorld>();
        m_EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();

        EntityQueryDesc query = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(CharacterControllerComponentData), typeof(CharacterControllerInternalData),
                typeof(CharacterControllerUserInputData), typeof(PhysicsCollider), typeof(Translation), typeof(Rotation) }
        };
        m_CharacterControllersGroup = GetEntityQuery(query);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        m_ExportPhysicsWorldSystem.FinalJobHandle.Complete();

        var chunks = m_CharacterControllersGroup.CreateArchetypeChunkArray(Allocator.TempJob);

        var ccComponentType = GetArchetypeChunkComponentType<CharacterControllerComponentData>();
        var ccInputType = GetArchetypeChunkComponentType<CharacterControllerUserInputData>();
        var ccInternalType = GetArchetypeChunkComponentType<CharacterControllerInternalData>();
        var physicsColliderType = GetArchetypeChunkComponentType<PhysicsCollider>();
        var translationType = GetArchetypeChunkComponentType<Translation>();
        var rotationType = GetArchetypeChunkComponentType<Rotation>();

        BlockStream deferredImpulses = new BlockStream(chunks.Length, 0xCA37B9F2);

        // Maximum number of hits character controller can store in world queries
        const int maxQueryHits = 128;

        // Read user input
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        int jumpInput = Input.GetButtonDown("Jump") ? 1 : 0;
        float shootXInput = Input.GetAxis("ShootX");

        for (int i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];

            // Fill all input data with the same values read from user.
            // These can be hooked differently, this is just an example.
            {
                var chunkCCInputData = chunk.GetNativeArray(ccInputType);
                for (int j = 0; j < chunk.Count; j++)
                {
                    var inputData = chunkCCInputData[j];
                    inputData.HorizontalInput = horizontalInput;
                    inputData.VerticalInput = verticalInput;
                    inputData.JumpInput = jumpInput;
                    inputData.ShootXInput = shootXInput;
                    chunkCCInputData[j] = inputData;
                }
            }
        }

        var ccJob = new CharacterControllerJob()
        {
            // Archetypes
            CharacterControllerComponentType = ccComponentType,
            CharacterControllerUserInputType = ccInputType,
            CharacterControllerInternalType = ccInternalType,
            PhysicsColliderType = physicsColliderType,
            TranslationType = translationType,
            RotationType = rotationType,
            // Input
            DeltaTime = Time.fixedDeltaTime,
            PhysicsWorld = m_BuildPhysicsWorldSystem.PhysicsWorld,
            DeferredImpulseWriter = deferredImpulses,
            // Memory
            DistanceHits = new NativeArray<DistanceHit>(maxQueryHits, Allocator.TempJob),
            CastHits = new NativeArray<ColliderCastHit>(maxQueryHits, Allocator.TempJob),
            Constraints = new NativeArray<SurfaceConstraintInfo>(4 * maxQueryHits, Allocator.TempJob)
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
        var disposeHandle = deferredImpulses.Dispose(inputDeps);

        // Must finish all jobs before physics step end
        m_EndFramePhysicsSystem.HandlesToWaitFor.Add(disposeHandle);

        return inputDeps;
    }
}
