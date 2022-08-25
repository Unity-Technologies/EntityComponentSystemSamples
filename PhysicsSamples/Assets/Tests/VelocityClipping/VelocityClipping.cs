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
    public class VelocityClipping : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new ClipVelocitiesData());
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(StepPhysicsWorld))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial class VelocityClippingSystem : SystemBase
    {
        EntityQuery m_VerificationGroup;
        BuildPhysicsWorld m_BuildPhysicsWorld;

        protected override void OnCreate()
        {
            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ClipVelocitiesData) }
            });

            RequireForUpdate(m_VerificationGroup);
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

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.RegisterPhysicsRuntimeSystemReadWrite();
        }

        protected override void OnUpdate()
        {
            var physicsStep = PhysicsStep.Default;
            if (HasSingleton<PhysicsStep>())
            {
                physicsStep = GetSingleton<PhysicsStep>();
            }

            // No need for clipping if Havok is used
            if (physicsStep.SimulationType == SimulationType.UnityPhysics)
            {
                Dependency = new ClipVelocitiesJob
                {
                    MotionVelocities = m_BuildPhysicsWorld.PhysicsWorld.MotionVelocities,
                    MotionDatas = m_BuildPhysicsWorld.PhysicsWorld.MotionDatas,
                    TimeStep = Time.DeltaTime,
                    Gravity = physicsStep.Gravity
                }.Schedule(Dependency);
            }
        }
    }
}
