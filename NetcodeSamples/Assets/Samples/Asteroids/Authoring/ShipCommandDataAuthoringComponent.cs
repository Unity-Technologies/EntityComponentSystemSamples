using Unity.Entities;
using UnityEngine;

public class ShipCommandDataAuthoringComponent : MonoBehaviour
{
}

public class ShipCommandDataAuthoringComponentBaker : Baker<ShipCommandDataAuthoringComponent>
{
    public override void Bake(ShipCommandDataAuthoringComponent authoring)
    {
        AddBuffer<ShipCommandData>();
    }
}
