#if UNITY_EDITOR

using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace Samples.Boids
{
    public class BoidAuthoringBaker : Baker<BoidAuthoring>
    {
        public override void Bake(BoidAuthoring authoring)
        {
            AddSharedComponent( new Boid
            {
                CellRadius = authoring.CellRadius,
                SeparationWeight = authoring.SeparationWeight,
                AlignmentWeight = authoring.AlignmentWeight,
                TargetWeight = authoring.TargetWeight,
                ObstacleAversionDistance = authoring.ObstacleAversionDistance,
                MoveSpeed = authoring.MoveSpeed
            });
        }
    }
}

#endif
