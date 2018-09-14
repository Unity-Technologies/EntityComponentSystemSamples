using System;
using Unity.Entities;

namespace Samples.Common
{
    [Serializable]
    public struct PositionConstraint : IComponentData
    {
        public Entity parentEntity;
        public float maxDistance;
    }

    public class PositionConstraintComponent : ComponentDataWrapper<PositionConstraint> { } 
}
