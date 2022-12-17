using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyGravityFactorData : IComponentData
    {
    }

    public class VerifyGravityFactor : MonoBehaviour
    {
        class VerifyGravityFactorBaker : Baker<VerifyGravityFactor>
        {
            public override void Bake(VerifyGravityFactor authoring)
            {
                AddComponent<VerifyGravityFactorData>();
            }
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial class VerifyGravityFactorSystem : SystemBase
    {
        EntityQuery m_VerificationGroup;

        protected override void OnCreate()
        {
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyGravityFactorData) }
            });
        }

        protected override void OnUpdate()
        {
            using (var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob))
            {
                foreach (var entity in entities)
                {
#if !ENABLE_TRANSFORM_V1
                    var transform = EntityManager.GetComponentData<LocalTransform>(entity);

                    // Sphere should never move due to gravity factor being 0
                    Assert.AreEqual(transform.Position.x, 0.0f);
                    Assert.AreEqual(transform.Position.y, 1.0f);
                    Assert.AreEqual(transform.Position.z, 0.0f);
#else
                    var translation = EntityManager.GetComponentData<Translation>(entity);

                    // Sphere should never move due to gravity factor being 0
                    Assert.AreEqual(translation.Value.x, 0.0f);
                    Assert.AreEqual(translation.Value.y, 1.0f);
                    Assert.AreEqual(translation.Value.z, 0.0f);
#endif
                }
            }
        }
    }
}
