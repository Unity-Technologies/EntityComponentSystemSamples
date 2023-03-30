using Unity.Entities;
using UnityEngine;

public class ShipCommandDataAuthoringComponent : MonoBehaviour
{
    public class Baker : Baker<ShipCommandDataAuthoringComponent>
    {
        public override void Bake(ShipCommandDataAuthoringComponent authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddBuffer<ShipCommandData>(entity);
        }
    }
}
