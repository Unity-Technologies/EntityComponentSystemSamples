using System;
using Unity.Entities;

namespace Samples.Boids
{
    [Serializable]
    public struct BoidObstacle : IComponentData { }

    [UnityEngine.DisallowMultipleComponent]
    public class BoidObstacleComponent : ComponentDataWrapper<BoidObstacle> { }
}
