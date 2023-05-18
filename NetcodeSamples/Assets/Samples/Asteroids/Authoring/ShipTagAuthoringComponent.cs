using Unity.Entities;
using UnityEngine;

public class ShipTagAuthoringComponent : MonoBehaviour
{
    public class Baker : Baker<ShipTagAuthoringComponent>
    {
        public override void Bake(ShipTagAuthoringComponent authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ShipTagComponentData());
        }
    }

}
