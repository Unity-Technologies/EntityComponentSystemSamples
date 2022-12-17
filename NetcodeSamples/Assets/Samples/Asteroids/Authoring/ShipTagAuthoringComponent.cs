using Unity.Entities;
using UnityEngine;

public class ShipTagAuthoringComponent : MonoBehaviour
{
}

public class ShipTagAuthoringComponentBaker : Baker<ShipTagAuthoringComponent>
{
    public override void Bake(ShipTagAuthoringComponent authoring)
    {
        AddComponent(new ShipTagComponentData());
    }
}
