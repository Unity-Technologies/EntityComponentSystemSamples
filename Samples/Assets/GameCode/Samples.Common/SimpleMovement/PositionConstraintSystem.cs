using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

namespace Samples.Common
{
    public class PositionConstraintSystem : JobComponentSystem
    {
        struct PositionConstraintsGroup
        {
#pragma warning disable 649
            [ReadOnly] public ComponentDataArray<PositionConstraint> positionConstraints;
            [ReadOnly] public EntityArray                            entities;
            public readonly int                                      Length;

        }

        [Inject] private PositionConstraintsGroup  m_PositionContraintsGroup;
        [Inject] ComponentDataFromEntity<Position> m_TransformPositions;
#pragma warning restore 649
        [BurstCompile]
        struct ContrainPositions : IJob
        {
            public ComponentDataFromEntity<Position>                 positions;
            [ReadOnly] public ComponentDataArray<PositionConstraint> positionConstraints;
            [ReadOnly] public EntityArray                            positionConstraintEntities;

            public void Execute()
            {
                for (int i = 0; i < positionConstraints.Length; i++)
                {
                    var childEntity = positionConstraintEntities[i];
                    var parentEntity = positionConstraints[i].parentEntity;
                    var childPosition = positions[childEntity].Value;
                    var parentPosition = positions[parentEntity].Value;
                    var d = childPosition - parentPosition;
                    var len = math.length(d);
                    if (len >= 0.0001f) // TODO- find out why parent/child position are identical
                    {
                        var nl = math.min(len, positionConstraints[i].maxDistance);

                        positions[childEntity] = new Position
                        {
                            Value = parentPosition + ((d * nl) / len)
                        };
                    }
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var constrainPositionsJob = new ContrainPositions
            {
                positions = m_TransformPositions,
                positionConstraints = m_PositionContraintsGroup.positionConstraints,
                positionConstraintEntities = m_PositionContraintsGroup.entities
            };
            
            return constrainPositionsJob.Schedule(inputDeps);
        }
    }
}
