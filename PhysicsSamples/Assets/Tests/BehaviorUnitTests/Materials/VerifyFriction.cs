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
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<VerifyFrictionData>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct VerifyFrictionSystem : ISystem
    {
        EntityQuery m_VerificationGroup;

        public void OnCreate(ref SystemState state)
        {
            m_VerificationGroup = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyFrictionData) }
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                var localTransform = state.EntityManager.GetComponentData<LocalTransform>(entity);

                // Cube should never get past the X == 0
                Assert.IsTrue(localTransform.Position.x < 0.0f);
            }
            entities.Dispose();
        }
    }
}
