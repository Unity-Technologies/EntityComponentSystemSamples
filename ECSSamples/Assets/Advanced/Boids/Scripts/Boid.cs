using System;
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;

namespace Samples.Boids
{
    [Serializable]
    [WriteGroup(typeof(LocalToWorld))]
    public struct Boid : ISharedComponentData
    {
        public float CellRadius;
        public float SeparationWeight;
        public float AlignmentWeight;
        public float TargetWeight;
        public float ObstacleAversionDistance;
        public float MoveSpeed;
    }

    namespace Authoring
    {
        [RequiresEntityConversion]
        public class Boid : MonoBehaviour, IConvertGameObjectToEntity
        {
            public float CellRadius;
            public float SeparationWeight;
            public float AlignmentWeight;
            public float TargetWeight;
            public float ObstacleAversionDistance;
            public float MoveSpeed;
    
            // Lets you convert the editor data representation to the entity optimal runtime representation
            public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
            {
                dstManager.AddSharedComponentData(entity, new Samples.Boids.Boid
                {
                    CellRadius = CellRadius,
                    SeparationWeight = SeparationWeight,
                    AlignmentWeight = AlignmentWeight,
                    TargetWeight = TargetWeight,
                    ObstacleAversionDistance = ObstacleAversionDistance,
                    MoveSpeed = MoveSpeed
                });
            }
        }
    }
}

