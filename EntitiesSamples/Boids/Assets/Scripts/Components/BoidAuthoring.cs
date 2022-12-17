#if UNITY_EDITOR

using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Samples.Boids
{
    public class BoidAuthoring : MonoBehaviour
    {
        public float CellRadius = 8.0f;
        public float SeparationWeight = 1.0f;
        public float AlignmentWeight = 1.0f;
        public float TargetWeight = 2.0f;
        public float ObstacleAversionDistance = 30.0f;
        public float MoveSpeed = 25.0f;

        public class BoidAuthoringBaker : Baker<BoidAuthoring>
        {
            public override void Bake(BoidAuthoring authoring)
            {
                AddSharedComponent(new Boid
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
}
#endif
