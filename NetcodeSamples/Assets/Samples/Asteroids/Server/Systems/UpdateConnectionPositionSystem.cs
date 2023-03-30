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

        ComponentLookup<LocalTransform> m_Transforms;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {

            m_Transforms = state.GetComponentLookup<LocalTransform>(true);

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            m_Transforms.Update(ref state);
            var updateJob = new UpdateConnectionPositionSystemJob
            {
                transformFromEntity = m_Transforms
            };

            updateJob.Schedule();
        }

        [BurstCompile]
        partial struct UpdateConnectionPositionSystemJob : IJobEntity
        {

            [ReadOnly] public ComponentLookup<LocalTransform> transformFromEntity;


            public void Execute(ref GhostConnectionPosition conPos, in CommandTarget target)
            {

                if (!transformFromEntity.HasComponent(target.targetEntity))
                    return;
                conPos = new GhostConnectionPosition
                {
                    Position = transformFromEntity[target.targetEntity].Position
                };

            }
        }
    }
}
