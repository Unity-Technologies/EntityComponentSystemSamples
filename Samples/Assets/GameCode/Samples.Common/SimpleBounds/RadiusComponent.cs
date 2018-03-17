using System;
using Unity.Entities;

namespace Samples.Common
{
    [Serializable]
    public struct Radius : IComponentData
    {
        public float radius;
    }

    public class RadiusComponent : ComponentDataWrapper<Radius> { } 
}
