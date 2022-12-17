using Unity.Entities;
using UnityEngine;

public class VelocityAuthoringComponent : MonoBehaviour
{
}

public class VelocityAuthoringComponentBaker : Baker<VelocityAuthoringComponent>
{
    public override void Bake(VelocityAuthoringComponent authoring)
    {
        AddComponent(new Velocity());
    }
}

