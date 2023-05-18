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
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<VerifyGravityFactorData>(entity);
            }
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial struct VerifyGravityFactorSystem : ISystem
    {
        EntityQuery m_VerificationGroup;

        public void OnCreate(ref SystemState state)
        {
            m_VerificationGroup = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyGravityFactorData) }
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            using (var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob))
            {
                foreach (var entity in entities)
                {
                    var transform = state.EntityManager.GetComponentData<LocalTransform>(entity);

                    // Sphere should never move due to gravity factor being 0
                    Assert.AreEqual(transform.Position.x, 0.0f);
                    Assert.AreEqual(transform.Position.y, 1.0f);
                    Assert.AreEqual(transform.Position.z, 0.0f);
                }
            }
        }
    }
}
