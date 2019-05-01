using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Samples.Boids
{
    public struct BoidObstacle : IComponentData
    { }
    
    [DisallowMultipleComponent] 
    public class BoidObstacleProxy : ComponentDataProxy<BoidObstacle> { }
}