using Unity.Entities;
using UnityEngine;

public struct CubeSpawner : IComponentData
{
    public Entity Cube;
    public Entity Config;
}

[DisallowMultipleComponent]
public class CubeSpawnerAuthoring : MonoBehaviour
{
    public GameObject Cube;
    public GameObject Config;

    class Baker : Baker<CubeSpawnerAuthoring>
    {
        public override void Bake(CubeSpawnerAuthoring authoring)
        {
            CubeSpawner component = default(CubeSpawner);
            component.Cube = GetEntity(authoring.Cube, TransformUsageFlags.Dynamic);
            component.Config = GetEntity(authoring.Config, TransformUsageFlags.None);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}

