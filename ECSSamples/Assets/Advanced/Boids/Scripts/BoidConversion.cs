#if UNITY_EDITOR

using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace Samples.Boids
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [ConverterVersion("macton", 4)]
    public class BoidConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((BoidAuthoring boidAuthoring) =>
            {
                var entity = GetPrimaryEntity(boidAuthoring);
                
                DstEntityManager.AddSharedComponentData(entity, new Boid
                {
                    CellRadius = boidAuthoring.CellRadius,
                    SeparationWeight = boidAuthoring.SeparationWeight,
                    AlignmentWeight = boidAuthoring.AlignmentWeight,
                    TargetWeight = boidAuthoring.TargetWeight,
                    ObstacleAversionDistance = boidAuthoring.ObstacleAversionDistance,
                    MoveSpeed = boidAuthoring.MoveSpeed
                });
                
                // Remove default transform system components
                DstEntityManager.RemoveComponent<Translation>(entity);
                DstEntityManager.RemoveComponent<Rotation>(entity);

                // Add world render bounds components so they do not need to be added at initialization time.
                DstEntityManager.AddComponent(entity, ComponentType.ChunkComponent<ChunkWorldRenderBounds>());
                DstEntityManager.AddComponent<WorldRenderBounds>(entity);
            });
        }
    }
}

#endif
