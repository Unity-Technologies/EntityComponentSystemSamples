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

    }

    public class VerifyRestitution : MonoBehaviour, IConvertGameObjectToEntity
    {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyRestitutionData());
        }
    }

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyRestitutionSystem : JobComponentSystem
    {
        int m_StepCounter;
        float m_MaxYValue;
        EntityQuery m_VerificationGroup;

        protected override void OnCreate()
        {
            m_StepCounter = 0;
            m_MaxYValue = 0.0f;
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyRestitutionData) }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Make sure we don't collect max Y value in first couple of steps
            // (until sphere starts falling)
            if (m_StepCounter > 10)
            {
                var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob);
                foreach (var entity in entities)
                {
                    var translation = EntityManager.GetComponentData<Translation>(entity);
                    m_MaxYValue = math.max(m_MaxYValue, translation.Value.y);

                }
                entities.Dispose();
            }

            m_StepCounter++;
            if (m_StepCounter > 50)
            {
                // Biggest bounce should be near the original height, which is 1
                Assert.IsTrue(m_MaxYValue > 0.9f);
            }

            return inputDeps;
        }
    }
}
