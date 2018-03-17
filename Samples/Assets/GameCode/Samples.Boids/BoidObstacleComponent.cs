using Unity.Entities;

namespace Samples.Boids
{
	public struct BoidObstacle : IComponentData { }

	public class BoidObstacleComponent : ComponentDataWrapper<BoidObstacle> { }
}