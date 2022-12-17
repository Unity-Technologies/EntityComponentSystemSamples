using Unity.Entities;
using UnityEngine;

public class PlayerIdAuthoringComponent : MonoBehaviour
{
}

public class PlayerIdAuthoringComponentBaker : Baker<PlayerIdAuthoringComponent>
{
    public override void Bake(PlayerIdAuthoringComponent authoring)
    {
        AddComponent(new PlayerIdComponentData());
    }
}
