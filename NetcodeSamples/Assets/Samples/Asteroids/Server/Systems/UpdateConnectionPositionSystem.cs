using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Transforms;

namespace Asteroids.Server
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct UpdateConnectionPositionSystem : ISystem
    {
#if !ENABLE_TRANSFORM_V1
        ComponentLookup<LocalTransform> m_Transforms;
#else
        ComponentLookup<Translation> m_Translations;
#endif

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
#if !ENABLE_TRANSFORM_V1
            m_Transforms = state.GetComponentLookup<LocalTransform>(true);
#else
            m_Translations = state.GetComponentLookup<Translation>(true);
#endif
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
#if !ENABLE_TRANSFORM_V1
            m_Transforms.Update(ref state);
            var updateJob = new UpdateConnectionPositionSystemJob
            {
                transformFromEntity = m_Transforms
            };
#else
            m_Translations.Update(ref state);
            var updateJob = new UpdateConnectionPositionSystemJob
            {
                translationFromEntity = m_Translations
            };
#endif
            updateJob.Schedule();
        }

        [BurstCompile]
        partial struct UpdateConnectionPositionSystemJob : IJobEntity
        {
#if !ENABLE_TRANSFORM_V1
            [ReadOnly] public ComponentLookup<LocalTransform> transformFromEntity;
#else
            [ReadOnly] public ComponentLookup<Translation> translationFromEntity;
#endif

            public void Execute(ref GhostConnectionPosition conPos, in CommandTargetComponent target)
            {
#if !ENABLE_TRANSFORM_V1
                if (!transformFromEntity.HasComponent(target.targetEntity))
                    return;
                conPos = new GhostConnectionPosition
                {
                    Position = transformFromEntity[target.targetEntity].Position
                };
#else
                if (!translationFromEntity.HasComponent(target.targetEntity))
                    return;
                conPos = new GhostConnectionPosition
                {
                    Position = translationFromEntity[target.targetEntity].Value
                };
#endif
            }
        }
    }
}
