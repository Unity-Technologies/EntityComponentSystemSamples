using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyCollisionData : IComponentData
    {
    }

    public class VerifyCollision : MonoBehaviour
    {
        class VerifyCollisionBaker : Baker<VerifyCollision>
        {
            public override void Bake(VerifyCollision authoring)
            {
                AddComponent<VerifyCollisionData>();
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial class VerifyCollisionSystem : SystemBase
    {
        EntityQuery m_VerificationGroup;

        protected override void OnCreate()
        {
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyCollisionData) }
            });
        }

        protected override void OnUpdate()
        {
            var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                // "Y" component should never go way below 0 if there was a collision
#if !ENABLE_TRANSFORM_V1
                var localTransform = EntityManager.GetComponentData<LocalTransform>(entity);
                Assert.IsTrue(localTransform.Position.y > -0.001f);
#else
                var translation = EntityManager.GetComponentData<Translation>(entity);
                Assert.IsTrue(translation.Value.y > -0.001f);
#endif
            }
            entities.Dispose();
        }
    }
}
