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
    public class VerifyRestitutionSystem : ComponentSystem
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
            StepCounter++;

            // Make sure we only collect max Y value when the body is bouncing back up
            using (var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob))
            {
                foreach (var entity in entities)
                {
                    var translation = EntityManager.GetComponentData<Translation>(entity);
                    var velocity = EntityManager.GetComponentData<PhysicsVelocity>(entity).Linear;
                    var verifyRestitution = EntityManager.GetComponentData<VerifyRestitutionData>(entity);
                    if (velocity.y > 0)
                    {
                        verifyRestitution.MaxY = math.max(verifyRestitution.MaxY, translation.Value.y);

                        if (StepCounter > 55)
                        {
                            // Biggest bounce should be near the original height, which is 1
                            Assert.IsTrue(verifyRestitution.MaxY > 0.9f);
                        }

                        PostUpdateCommands.SetComponent(entity, verifyRestitution);
                    }
                }
            }
        }
    }
}
