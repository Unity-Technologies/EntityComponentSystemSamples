using System;
using Unity.Entities;

namespace Samples.Common
{
    [Serializable]
    public struct RandomInitialHeading : IComponentData { }

    public class RandomInitialHeadingComponent : ComponentDataWrapper<RandomInitialHeading> { } 
}
