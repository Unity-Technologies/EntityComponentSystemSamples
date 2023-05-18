using Unity.Entities;
using UnityEngine;

public class ShipStateAuthoringComponent : MonoBehaviour
{
    public class Baker : Baker<ShipStateAuthoringComponent>
    {
        public override void Bake(ShipStateAuthoringComponent authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ShipStateComponentData());
        }
    }

}
