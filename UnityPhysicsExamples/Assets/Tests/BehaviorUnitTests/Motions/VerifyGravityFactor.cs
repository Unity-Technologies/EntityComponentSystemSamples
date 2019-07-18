using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyGravityFactorData : IComponentData
    {

    }

    public class VerifyGravityFactor : MonoBehaviour, IConvertGameObjectToEntity
    {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyGravityFactorData());
        }
    }

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyGravityFactorSystem : JobComponentSystem
    {
        EntityQuery m_VerificationGroup;

        protected override void OnCreate()
        {
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyGravityFactorData) }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                var translation = EntityManager.GetComponentData<Translation>(entity);

                // Sphere should never move due to gravity factor being 0
                Assert.AreEqual(translation.Value.x, 0.0f);
                Assert.AreEqual(translation.Value.y, 1.0f);
                Assert.AreEqual(translation.Value.z, 0.0f);
            }
            entities.Dispose();

            return inputDeps;
        }
    }
}
