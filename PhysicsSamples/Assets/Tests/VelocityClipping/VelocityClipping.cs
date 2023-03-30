using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using UnityEngine;

namespace Unity.Physics.Tests
{
    public struct ClipVelocitiesData : IComponentData {}

    [Serializable]
    public class VelocityClipping : MonoBehaviour
    {
        class VelocityClippingBaker : Baker<VelocityClipping>
        {
            public override void Bake(VelocityClipping authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ClipVelocitiesData>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial struct VelocityClippingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ClipVelocitiesData) }
            }));
        }

        struct ClipVelocitiesJob : IJob
        {
            public NativeArray<MotionVelocity> MotionVelocities;
            public NativeArray<MotionData> MotionDatas;
            public float TimeStep;
            public float3 Gravity;

            public void Execute()
            {
                float gravityLengthInOneStep = math.length(Gravity * TimeStep);
                for (int i = 0; i < MotionVelocities.Length; i++)
                {
                    var motionData = MotionDatas[i];
                    var motionVelocity = MotionVelocities[i];

                    // Clip velocities using a simple heuristic:
                    // zero out velocities that are smaller than gravity in one step
                    if (math.length(motionVelocity.LinearVelocity) < motionVelocity.GravityFactor * gravityLengthInOneStep)
                    {
                        // Revert integration
                        Integrator.Integrate(ref motionData.WorldFromMotion, motionVelocity, -TimeStep);

                        // Clip velocity
                        motionVelocity.LinearVelocity = float3.zero;
                        motionVelocity.AngularVelocity = float3.zero;

                        // Write back
                        MotionDatas[i] = motionData;
                        MotionVelocities[i] = motionVelocity;
                    }
                }
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            var physicsStep = PhysicsStep.Default;
            if (SystemAPI.HasSingleton<PhysicsStep>())
            {
                physicsStep = SystemAPI.GetSingleton<PhysicsStep>();
            }

            //var world = GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            var world = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;
            // No need for clipping if Havok is used
            if (physicsStep.SimulationType == SimulationType.UnityPhysics)
            {
                state.Dependency = new ClipVelocitiesJob
                {
                    MotionVelocities = world.MotionVelocities,
                    MotionDatas = world.MotionDatas,
                    TimeStep = SystemAPI.Time.DeltaTime,
                    Gravity = physicsStep.Gravity
                }.Schedule(state.Dependency);
            }
        }
    }
}
