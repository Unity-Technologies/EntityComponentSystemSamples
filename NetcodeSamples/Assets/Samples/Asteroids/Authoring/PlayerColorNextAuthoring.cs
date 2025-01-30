using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerColorNextAuthoring : MonoBehaviour
{
    class Baker : Baker<PlayerColorNextAuthoring>
    {
        public override void Bake(PlayerColorNextAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerColorNext() { Value = 1 });
        }
    }
}
