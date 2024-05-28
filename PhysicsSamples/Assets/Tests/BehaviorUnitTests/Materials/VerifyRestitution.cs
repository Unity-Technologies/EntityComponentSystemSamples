using Unity.Entities;
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

    public class VerifyRestitution : MonoBehaviour
    {
        class VerifyRestitutionBaker : Baker<VerifyRestitution>
        {
            public override void Bake(VerifyRestitution authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new VerifyRestitutionData() { MaxY = 0 });
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct VerifyRestitutionSystem : ISystem
    {
        private bool m_FirstFrame;
        double m_StartSeconds;
        const double kCheckSeconds = 0.9;

        void ResetStartTime(ref SystemState state)
        {
            m_StartSeconds = SystemAPI.Time.ElapsedTime;
        }

        public void OnCreate(ref SystemState state)
        {
            m_FirstFrame = true;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (m_FirstFrame)
            {
                ResetStartTime(ref state);
                m_FirstFrame = false;
            }

            double elapsedSeconds = SystemAPI.Time.ElapsedTime - m_StartSeconds;

            foreach (var(verifyRestitution, localTransform, velocity, entity) in SystemAPI.Query<RefRW<VerifyRestitutionData>, RefRO<LocalTransform>, RefRO<PhysicsVelocity>>().WithEntityAccess())
            {
                if (velocity.ValueRO.Linear.y > 0)
                {
                    verifyRestitution.ValueRW.MaxY = math.max(verifyRestitution.ValueRW.MaxY, localTransform.ValueRO.Position.y);
                }

                // the ball shall have reached its apex after a certain amount of time has passed
                if (elapsedSeconds > kCheckSeconds)
                {
                    // Biggest bounce should be near the original height, which is 1
                    Assert.IsTrue(verifyRestitution.ValueRW.MaxY > 0.9f);
                }
            }
        }
    }
}
