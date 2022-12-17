using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;

/*
 * Issues:
 *  - setting up constraints if not using GameObjects
 *  - providing utility functions for Component and Direct data manipulation
 *  - assigning multiple Components of the same type to a single Entity
 */

public struct LinearDashpot : IComponentData
{
    public Entity localEntity;
    public float3 localOffset;
    public Entity parentEntity;
    public float3 parentOffset;

    public int dontApplyImpulseToParent;
    public float strength;
    public float damping;
}

public class LinearDashpotBehaviour : MonoBehaviour
{
    public PhysicsBodyAuthoring parentBody;
    public float3 parentOffset;
    public float3 localOffset;

    public bool dontApplyImpulseToParent = false;
    public float strength;
    public float damping;

    void OnEnable() {}
}

class LinearDashpotBaker : Baker<LinearDashpotBehaviour>
{
    public override void Bake(LinearDashpotBehaviour authoring)
    {
        if (authoring.enabled)
        {
            // Note: GetPrimaryEntity currently creates a new Entity
            //       if the parentBody is not a child in the scene hierarchy
            var componentData = new LinearDashpot
            {
                localEntity = GetEntity(),
                localOffset = authoring.localOffset,
                parentEntity = authoring.parentBody == null ? Entity.Null : GetEntity(authoring.parentBody),
                parentOffset = authoring.parentOffset,
                dontApplyImpulseToParent = authoring.dontApplyImpulseToParent ? 1 : 0,
                strength = authoring.strength,
                damping = authoring.damping
            };

            Entity dashpotEntity = CreateAdditionalEntity();
            AddComponent(dashpotEntity, componentData);
        }
    }
}

#region System
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct LinearDashpotSystem : ISystem
{
    public EntityQuery LinearDashpotQuery;
    public ComponentHandles Handles;

    public struct ComponentHandles
    {
        public ComponentLookup<PhysicsVelocity> Velocities;
#if !ENABLE_TRANSFORM_V1
        public ComponentLookup<LocalTransform> LocalTransforms;
#else
        public ComponentLookup<Translation> Translations;
        public ComponentLookup<Rotation> Rotations;
#endif
        public ComponentLookup<PhysicsMass> Masses;
        public ComponentLookup<LocalToWorld> LocalToWorlds;
        public ComponentTypeHandle<LinearDashpot> LinearDashpotHandle;

        public ComponentHandles(ref SystemState state)
        {
            Velocities = state.GetComponentLookup<PhysicsVelocity>(false);
#if !ENABLE_TRANSFORM_V1
            LocalTransforms = state.GetComponentLookup<LocalTransform>(true);
#else
            Translations = state.GetComponentLookup<Translation>(true);
            Rotations = state.GetComponentLookup<Rotation>(true);
#endif
            Masses = state.GetComponentLookup<PhysicsMass>(true);
            LocalToWorlds = state.GetComponentLookup<LocalToWorld>(true);
            LinearDashpotHandle = state.GetComponentTypeHandle<LinearDashpot>(true);
        }

        public void Update(ref SystemState state)
        {
            Velocities.Update(ref state);
#if !ENABLE_TRANSFORM_V1
            LocalTransforms.Update(ref state);
#else
            Translations.Update(ref state);
            Rotations.Update(ref state);
#endif
            Masses.Update(ref state);
            LocalToWorlds.Update(ref state);
            LinearDashpotHandle.Update(ref state);
        }
    }


    [BurstCompile]
    struct LinearDashpotJob : IJobChunk
    {
        public ComponentLookup<PhysicsVelocity> Velocities;
#if !ENABLE_TRANSFORM_V1
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransforms;
#else
        [ReadOnly] public ComponentLookup<Translation> Translations;
        [ReadOnly] public ComponentLookup<Rotation> Rotations;
#endif
        [ReadOnly] public ComponentLookup<PhysicsMass> Masses;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorlds;
        [ReadOnly] public ComponentTypeHandle<LinearDashpot> LinearDashpotHandle;

        [BurstCompile]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<LinearDashpot> chunkLinearDashpots = chunk.GetNativeArray(ref LinearDashpotHandle);

            bool hasChunkLinearDashpotType = chunk.Has(ref LinearDashpotHandle);

            if (!hasChunkLinearDashpotType)
            {
                // should never happen
                return;
            }

            int instanceCount = chunk.Count;

            ChunkEntityEnumerator enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, instanceCount);

            while (enumerator.NextEntityIndex(out var i))
            {
                var linearDashpot = chunkLinearDashpots[i];

                if (linearDashpot.strength == 0)
                {
                    continue;
                }

                var eA = linearDashpot.localEntity;
                var eB = linearDashpot.parentEntity;

                var eAIsNull = eA == Entity.Null;
                if (eAIsNull)
                {
                    continue;
                }

                var eBIsNull = eB == Entity.Null;

                var hasVelocityA = !eAIsNull && Velocities.HasComponent(eA);
                var hasVelocityB = !eBIsNull && Velocities.HasComponent(eB);

                if (!hasVelocityA)
                {
                    return;
                }

#if !ENABLE_TRANSFORM_V1
                LocalTransform localTransformA = LocalTransform.Identity;
#else
                Translation positionA = default;
                Rotation rotationA = new Rotation { Value = quaternion.identity };
#endif
                PhysicsVelocity velocityA = default;
                PhysicsMass massA = default;

#if !ENABLE_TRANSFORM_V1
                LocalTransform localTransformB = localTransformA;
#else
                Translation positionB = positionA;
                Rotation rotationB = rotationA;
#endif
                PhysicsVelocity velocityB = velocityA;
                PhysicsMass massB = massA;

#if !ENABLE_TRANSFORM_V1
                if (LocalTransforms.HasComponent(eA)) localTransformA = LocalTransforms[eA];
#else
                if (Translations.HasComponent(eA)) positionA = Translations[eA];
                if (Rotations.HasComponent(eA)) rotationA = Rotations[eA];
#endif
                if (Velocities.HasComponent(eA)) velocityA = Velocities[eA];
                if (Masses.HasComponent(eA)) massA = Masses[eA];

                if (LocalToWorlds.HasComponent(eB))
                {
                    // parent could be static and not have a Translation or Rotation
                    var worldFromBody = Math.DecomposeRigidBodyTransform(LocalToWorlds[eB].Value);
#if !ENABLE_TRANSFORM_V1
                    localTransformB.Position = worldFromBody.pos;
                    localTransformB.Rotation = worldFromBody.rot;
#else
                    positionB = new Translation { Value = worldFromBody.pos };
                    rotationB = new Rotation { Value = worldFromBody.rot };
#endif
                }
#if !ENABLE_TRANSFORM_V1
                if (LocalTransforms.HasComponent(eB)) localTransformB = LocalTransforms[eB];
#else
                if (Translations.HasComponent(eB)) positionB = Translations[eB];
                if (Rotations.HasComponent(eB)) rotationB = Rotations[eB];
#endif
                if (Velocities.HasComponent(eB)) velocityB = Velocities[eB];
                if (Masses.HasComponent(eB)) massB = Masses[eB];


#if !ENABLE_TRANSFORM_V1
                var posA = math.transform(new RigidTransform(localTransformA.Rotation, localTransformA.Position), linearDashpot.localOffset);
                var posB = math.transform(new RigidTransform(localTransformB.Rotation, localTransformB.Position), linearDashpot.parentOffset);
                var lvA = velocityA.GetLinearVelocity(massA, localTransformA.Position, localTransformA.Rotation, posA);
                var lvB = velocityB.GetLinearVelocity(massB, localTransformB.Position, localTransformB.Rotation, posB);
#else
                var posA = math.transform(new RigidTransform(rotationA.Value, positionA.Value), linearDashpot.localOffset);
                var posB = math.transform(new RigidTransform(rotationB.Value, positionB.Value), linearDashpot.parentOffset);
                var lvA = velocityA.GetLinearVelocity(massA, positionA.Value, rotationA.Value, posA);
                var lvB = velocityB.GetLinearVelocity(massB, positionB.Value, rotationB.Value, posB);
#endif

                var impulse = linearDashpot.strength * (posB - posA) + linearDashpot.damping * (lvB - lvA);
                impulse = math.clamp(impulse, new float3(-100.0f), new float3(100.0f));

#if !ENABLE_TRANSFORM_V1
                velocityA.ApplyImpulse(massA, localTransformA.Position, localTransformA.Rotation, impulse, posA);
#else
                velocityA.ApplyImpulse(massA, positionA.Value, rotationA.Value, impulse, posA);
#endif
                Velocities[eA] = velocityA;

                if (0 == linearDashpot.dontApplyImpulseToParent && hasVelocityB)
                {
#if !ENABLE_TRANSFORM_V1
                    velocityB.ApplyImpulse(massB, localTransformB.Position, localTransformB.Rotation, -impulse, posB);
#else
                    velocityB.ApplyImpulse(massB, positionB.Value, rotationB.Value, -impulse, posB);
#endif
                    Velocities[eB] = velocityB;
                }
            }
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        Handles = new ComponentHandles(ref state);

        LinearDashpotQuery = state.GetEntityQuery(ComponentType.ReadOnly<LinearDashpot>());
        state.RequireForUpdate(LinearDashpotQuery);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Handles.Update(ref state);

        state.Dependency = new LinearDashpotJob
        {
            Velocities = Handles.Velocities,
#if !ENABLE_TRANSFORM_V1
            LocalTransforms = Handles.LocalTransforms,
#else
            Translations = Handles.Translations,
            Rotations = Handles.Rotations,
#endif
            Masses = Handles.Masses,
            LocalToWorlds = Handles.LocalToWorlds,
            LinearDashpotHandle = Handles.LinearDashpotHandle
        }.Schedule(LinearDashpotQuery, state.Dependency);
    }
}
#endregion
