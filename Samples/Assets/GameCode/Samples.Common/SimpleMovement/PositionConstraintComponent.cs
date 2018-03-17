using Unity.Entities;

namespace Samples.Common
{
    public struct PositionConstraint : IComponentData
    {
        public Entity parentEntity;
        public float maxDistance;
    }

    public class PositionConstraintComponent : ComponentDataWrapper<PositionConstraint> { } 
}
