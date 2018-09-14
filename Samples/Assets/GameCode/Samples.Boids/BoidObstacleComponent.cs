using System;
using Unity.Entities;

namespace Samples.Boids
{
    [Serializable]
	public struct BoidObstacle : IComponentData { }

	public class BoidObstacleComponent : ComponentDataWrapper<BoidObstacle> { }
}
