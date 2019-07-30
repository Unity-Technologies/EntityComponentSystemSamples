using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyFrictionData : IComponentData
    {

    }

    public class VerifyFriction : MonoBehaviour, IConvertGameObjectToEntity
    {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyFrictionData());
        }
    }

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyFrictionSystem : JobComponentSystem
    {
        EntityQuery m_VerificationGroup;

        protected override void OnCreate()
        {
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyFrictionData) }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                var translation = EntityManager.GetComponentData<Translation>(entity);

                // Cube should never get past the X == 0
                Assert.IsTrue(translation.Value.x < 0.0f);
            }
            entities.Dispose();

            return inputDeps;
        }
    }
}
