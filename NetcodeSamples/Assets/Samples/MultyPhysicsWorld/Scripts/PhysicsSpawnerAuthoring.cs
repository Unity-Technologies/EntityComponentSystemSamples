using Unity.Entities;
using UnityEngine;

public struct PhysicsSpawner : IComponentData
{
    public Entity prefab;
}

[DisallowMultipleComponent]
public class PhysicsSpawnerAuthoring : MonoBehaviour
{
    public GameObject prefab;

    class PhysicsSpawnerBaker : Baker<PhysicsSpawnerAuthoring>
    {
        public override void Bake(PhysicsSpawnerAuthoring authoring)
        {
            PhysicsSpawner component = default(PhysicsSpawner);
            component.prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
