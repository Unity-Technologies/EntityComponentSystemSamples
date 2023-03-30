using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class BulletTagComponentAuthoring : MonoBehaviour
{
    class Baker : Baker<BulletTagComponentAuthoring>
    {
        public override void Bake(BulletTagComponentAuthoring authoring)
        {
            BulletTagComponent component = default(BulletTagComponent);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
