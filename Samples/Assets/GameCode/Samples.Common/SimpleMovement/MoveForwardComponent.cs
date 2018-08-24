using Unity.Entities;

namespace Samples.Common
{
    public struct MoveForward : ISharedComponentData { }

    public class MoveForwardComponent : SharedComponentDataWrapper<MoveForward> { } 
}
