using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyCollisionData : IComponentData
    {

    }

    public class VerifyCollision : MonoBehaviour, IConvertGameObjectToEntity
    {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyCollisionData());
        }
    }

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyCollisionSystem : JobComponentSystem
    {
        EntityQuery m_VerificationGroup;

        protected override void OnCreate()
        {
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyCollisionData) }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                // "Y" component should never go way below 0 if there was a collision
                var translation = EntityManager.GetComponentData<Translation>(entity);
                Assert.IsTrue(translation.Value.y > -0.001f);
            }
            entities.Dispose();

            return inputDeps;
        }
    }
}
