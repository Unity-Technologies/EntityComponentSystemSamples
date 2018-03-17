using System;
using Unity.Entities;

namespace Samples.Boids
{
    [Serializable]
    public struct Boid : ISharedComponentData
    {
        public float cellRadius;
        public float separationWeight;
        public float alignmentWeight;
        public float targetWeight;
        public float obstacleAversionDistance;
    }

    public class BoidComponent : SharedComponentDataWrapper<Boid> { }
}
