using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Modify
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct LinearDashpotSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LinearDashpot>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var linearDashpotQuery = SystemAPI.QueryBuilder().WithAll<LinearDashpot>().Build();

            state.Dependency = new LinearDashpotJob
            {
                VelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                MassLookup = SystemAPI.GetComponentLookup<PhysicsMass>(true),
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                LinearDashpotHandle = SystemAPI.GetComponentTypeHandle<LinearDashpot>(true)
            }.Schedule(linearDashpotQuery, state.Dependency);
        }

        [BurstCompile]
        struct LinearDashpotJob : IJobChunk
        {
            public ComponentLookup<PhysicsVelocity> VelocityLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<PhysicsMass> MassLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            [ReadOnly] public ComponentTypeHandle<LinearDashpot> LinearDashpotHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                NativeArray<LinearDashpot> chunkLinearDashpots = chunk.GetNativeArray(ref LinearDashpotHandle);

                bool hasChunkLinearDashpotType = chunk.Has(ref LinearDashpotHandle);
                if (!hasChunkLinearDashpotType)
                {
                    // should never happen
                    return;
                }

                int instanceCount = chunk.Count;

                ChunkEntityEnumerator enumerator =
                    new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, instanceCount);

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

                    var hasVelocityA = !eAIsNull && VelocityLookup.HasComponent(eA);
                    var hasVelocityB = !eBIsNull && VelocityLookup.HasComponent(eB);

                    if (!hasVelocityA)
                    {
                        return;
                    }

                    LocalTransform localTransformA = LocalTransform.Identity;
                    PhysicsVelocity velocityA = default;
                    PhysicsMass massA = default;
                    LocalTransform localTransformB = localTransformA;
                    PhysicsVelocity velocityB = velocityA;
                    PhysicsMass massB = massA;

                    if (LocalTransformLookup.HasComponent(eA))
                    {
                        localTransformA = LocalTransformLookup[eA];
                    }

                    if (VelocityLookup.HasComponent(eA))
                    {
                        velocityA = VelocityLookup[eA];
                    }

                    if (MassLookup.HasComponent(eA))
                    {
                        massA = MassLookup[eA];
                    }

                    if (LocalToWorldLookup.HasComponent(eB))
                    {
                        // parent could be static and not have a Translation or Rotation
                        var worldFromBody = Math.DecomposeRigidBodyTransform(LocalToWorldLookup[eB].Value);

                        localTransformB.Position = worldFromBody.pos;
                        localTransformB.Rotation = worldFromBody.rot;
                    }

                    if (LocalTransformLookup.HasComponent(eB))
                    {
                        localTransformB = LocalTransformLookup[eB];
                    }

                    if (VelocityLookup.HasComponent(eB))
                    {
                        velocityB = VelocityLookup[eB];
                    }

                    if (MassLookup.HasComponent(eB))
                    {
                        massB = MassLookup[eB];
                    }

                    var posA = math.transform(new RigidTransform(localTransformA.Rotation, localTransformA.Position),
                        linearDashpot.localOffset);
                    var posB = math.transform(new RigidTransform(localTransformB.Rotation, localTransformB.Position),
                        linearDashpot.parentOffset);
                    var lvA = velocityA.GetLinearVelocity(massA, localTransformA.Position, localTransformA.Rotation,
                        posA);
                    var lvB = velocityB.GetLinearVelocity(massB, localTransformB.Position, localTransformB.Rotation,
                        posB);

                    var impulse = linearDashpot.strength * (posB - posA) + linearDashpot.damping * (lvB - lvA);
                    impulse = math.clamp(impulse, new float3(-100.0f), new float3(100.0f));
                    velocityA.ApplyImpulse(massA, localTransformA.Position, localTransformA.Rotation, impulse, posA);

                    VelocityLookup[eA] = velocityA;

                    if (0 == linearDashpot.dontApplyImpulseToParent && hasVelocityB)
                    {
                        velocityB.ApplyImpulse(massB, localTransformB.Position, localTransformB.Rotation, -impulse,
                            posB);

                        VelocityLookup[eB] = velocityB;
                    }
                }
            }
        }
    }
}
