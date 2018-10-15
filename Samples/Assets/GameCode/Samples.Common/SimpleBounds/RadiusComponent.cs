using System;
using Unity.Entities;

namespace Samples.Common
{
    [Serializable]
    public struct Radius : IComponentData
    {
        public float radius;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class RadiusComponent : ComponentDataWrapper<Radius> { } 
}
