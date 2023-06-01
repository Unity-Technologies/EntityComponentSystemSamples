using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine.Serialization;

namespace Modify
{
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(PhysicsSolveAndIntegrateGroup))]
    [UpdateAfter(typeof(PhysicsCreateJacobiansGroup))]
    public partial struct ConveyorBeltSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConveyorBelt>();
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();
            if (simulation.Type == SimulationType.NoPhysics)
            {
                return;
            }

            ref var world = ref SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;

            state.Dependency = new SetConveyorBeltSpeedJob
            {
                ConveyorBeltLookup = SystemAPI.GetComponentLookup<ConveyorBelt>(true),
                Bodies = world.Bodies
            }.Schedule(simulation, ref world, state.Dependency);
        }

        [BurstCompile]
        struct SetConveyorBeltSpeedJob : IJacobiansJob
        {
            [ReadOnly] public ComponentLookup<ConveyorBelt> ConveyorBeltLookup;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<RigidBody> Bodies;

            // Don't do anything for triggers
            public void Execute(ref ModifiableJacobianHeader h, ref ModifiableTriggerJacobian j)
            {
            }

            public void Execute(ref ModifiableJacobianHeader jacHeader, ref ModifiableContactJacobian contact)
            {
                if (!jacHeader.HasSurfaceVelocity) return;

                float3 linearVelocity = float3.zero;
                float3 angularVelocity = float3.zero;

                // Get the surface velocities if available
                for (int i = 0; i < 2; i++)
                {
                    var entity = (i == 0) ? jacHeader.EntityA : jacHeader.EntityB;
                    if (!ConveyorBeltLookup.HasComponent(entity)) continue;

                    var index = (i == 0) ? jacHeader.BodyIndexA : jacHeader.BodyIndexB;
                    var rotation = Bodies[index].WorldFromBody.rot;
                    var belt = ConveyorBeltLookup[entity];

                    if (belt.IsAngular)
                    {
                        // assuming rotation is around contact normal.
                        var av = contact.Normal * belt.Speed;

                        // calculate linear velocity at point, assuming rotating around body pivot
                        var otherIndex = (i == 0) ? jacHeader.BodyIndexB : jacHeader.BodyIndexA;
                        var offset = Bodies[otherIndex].WorldFromBody.pos - Bodies[index].WorldFromBody.pos;
                        var lv = math.cross(av, offset);

                        angularVelocity += av;
                        linearVelocity += lv;
                    }
                    else
                    {
                        linearVelocity += math.rotate(rotation, belt.LocalDirection) * belt.Speed;
                    }
                }

                // Add the extra velocities
                jacHeader.SurfaceVelocity = new SurfaceVelocity
                {
                    LinearVelocity = jacHeader.SurfaceVelocity.LinearVelocity + linearVelocity,
                    AngularVelocity = jacHeader.SurfaceVelocity.AngularVelocity + angularVelocity,
                };
            }
        }
    }
}
