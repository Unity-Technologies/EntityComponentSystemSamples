using System;
using Unity.Entities;

namespace Samples.Common
{
    [Serializable]
    public struct RandomInitialHeading : IComponentData { }

    [UnityEngine.DisallowMultipleComponent]
    public class RandomInitialHeadingComponent : ComponentDataWrapper<RandomInitialHeading> { } 
}
