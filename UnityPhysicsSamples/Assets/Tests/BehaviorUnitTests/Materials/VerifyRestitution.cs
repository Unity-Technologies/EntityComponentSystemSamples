using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyRestitutionData : IComponentData
    {
        public float MaxY;
    }

    public class VerifyRestitution : MonoBehaviour, IConvertGameObjectToEntity
    {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyRestitutionData() { MaxY = 0 });
        }

        void OnEnable()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<VerifyRestitutionSystem>();
            system.ResetStartTime();
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyRestitutionSystem : SystemBase
    {
        double m_StartSeconds;
        const double kCheckSeconds = 0.9;

        public void ResetStartTime() => m_StartSeconds = Time.ElapsedTime;

        protected override void OnCreate()
        {
            ResetStartTime();
        }

        protected override void OnUpdate()
        {
            double elapsedSeconds = Time.ElapsedTime - m_StartSeconds;
            Entities
                .WithoutBurst()// asserts don't fire from Burst loops
                .ForEach((Entity entity, ref VerifyRestitutionData verifyRestitution, in Translation translation, in PhysicsVelocity velocity) =>
                {
                    if (velocity.Linear.y > 0)
                    {
                        verifyRestitution.MaxY = math.max(verifyRestitution.MaxY, translation.Value.y);
                    }

                    // the ball shall have reached its apex after a certain amount of time has passed
                    if (elapsedSeconds > kCheckSeconds)
                    {
                        // Biggest bounce should be near the original height, which is 1
                        Assert.IsTrue(verifyRestitution.MaxY > 0.9f);
                    }
                }).Run();
        }
    }
}
