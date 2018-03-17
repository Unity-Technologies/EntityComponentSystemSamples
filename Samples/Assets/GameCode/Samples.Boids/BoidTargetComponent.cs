using Unity.Entities;

namespace Samples.Boids
{
	public struct BoidTarget : IComponentData { }

	public class BoidTargetComponent : ComponentDataWrapper<BoidTarget> { }
}