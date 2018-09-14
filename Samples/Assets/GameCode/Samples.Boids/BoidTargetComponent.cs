using System;
using Unity.Entities;

namespace Samples.Boids
{
    [Serializable]
	public struct BoidTarget : IComponentData { }

	public class BoidTargetComponent : ComponentDataWrapper<BoidTarget> { }
}
