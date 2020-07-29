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
            system.StepCounter = 0;
        }
    }

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyRestitutionSystem : SystemBase
    {
        EntityQuery m_VerificationGroup;
        public int StepCounter;
        
        protected override void OnCreate()
        {
            StepCounter = 0;
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyRestitutionData) }
            });
        }

        protected override void OnUpdate()
        {
            var stepCount = StepCounter++;

            Entities
            .ForEach((Entity entity, ref VerifyRestitutionData verifyRestitution, in Translation translation, in PhysicsVelocity velocity) =>
            {
                if (velocity.Linear.y > 0)
                {
                    verifyRestitution.MaxY = math.max(verifyRestitution.MaxY, translation.Value.y);

                    if (stepCount > 55)
                    {
                        // Biggest bounce should be near the original height, which is 1
                        Assert.IsTrue(verifyRestitution.MaxY > 0.9f);
                    }
                }
            }).Run();
        }
    }
}
