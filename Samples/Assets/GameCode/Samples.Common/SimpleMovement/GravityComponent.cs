using Unity.Entities;

namespace Samples.Common
{
    public struct Gravity : ISharedComponentData { }

    public class GravityComponent : SharedComponentDataWrapper<Gravity> { } 
}
