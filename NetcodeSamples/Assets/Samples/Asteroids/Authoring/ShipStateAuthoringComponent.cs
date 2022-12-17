using Unity.Entities;
using UnityEngine;

public class ShipStateAuthoringComponent : MonoBehaviour
{
}

public class ShipStateAuthoringComponentBaker : Baker<ShipStateAuthoringComponent>
{
    public override void Bake(ShipStateAuthoringComponent authoring)
    {
        AddComponent(new ShipStateComponentData());
    }
}
