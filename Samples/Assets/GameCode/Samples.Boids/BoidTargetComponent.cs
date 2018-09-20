using System;
using Unity.Entities;

namespace Samples.Boids
{
    [Serializable]
    public struct BoidTarget : IComponentData { }

    [UnityEngine.DisallowMultipleComponent]
    public class BoidTargetComponent : ComponentDataWrapper<BoidTarget> { }
}
