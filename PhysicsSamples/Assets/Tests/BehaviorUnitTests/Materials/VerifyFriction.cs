using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyFrictionData : IComponentData
    {
    }

    public class VerifyFriction : MonoBehaviour
    {
        class VerifyFrictionBaker : Baker<VerifyFriction>
        {
            public override void Bake(VerifyFriction authoring)
            {
                AddComponent<VerifyFrictionData>();
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class VerifyFrictionSystem : SystemBase
    {
        EntityQuery m_VerificationGroup;

        protected override void OnCreate()
        {
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyFrictionData) }
            });
        }

        protected override void OnUpdate()
        {
            var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
#if !ENABLE_TRANSFORM_V1
                var localTransform = EntityManager.GetComponentData<LocalTransform>(entity);

                // Cube should never get past the X == 0
                Assert.IsTrue(localTransform.Position.x < 0.0f);
#else
                var translation = EntityManager.GetComponentData<Translation>(entity);

                // Cube should never get past the X == 0
                Assert.IsTrue(translation.Value.x < 0.0f);
#endif
            }
            entities.Dispose();
        }
    }
}
