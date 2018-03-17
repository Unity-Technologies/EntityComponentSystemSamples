using Unity.Entities;

namespace Samples.Common
{
    public struct RandomInitialHeading : IComponentData { }

    public class RandomInitialHeadingComponent : ComponentDataWrapper<RandomInitialHeading> { } 
}
