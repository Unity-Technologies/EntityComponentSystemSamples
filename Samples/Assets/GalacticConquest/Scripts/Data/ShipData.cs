using Unity.Entities;

namespace Data
{
    public struct ShipData : IComponentData
    {
        public Entity TargetEntity;
        public int TeamOwnership;
    }
}
