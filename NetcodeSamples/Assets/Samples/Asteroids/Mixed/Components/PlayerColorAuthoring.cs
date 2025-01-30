using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[GhostComponent(PrefabType = GhostPrefabType.All)]
public struct PlayerColor : IComponentData
{
    [GhostField]
    public int Value;
}

[DisallowMultipleComponent]
public class PlayerColorAuthoring : MonoBehaviour
{
    class Baker : Baker<PlayerColorAuthoring>
    {
        public override void Bake(PlayerColorAuthoring authoring)
        {
            PlayerColor component = default(PlayerColor);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
