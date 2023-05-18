using Unity.Entities;
using UnityEngine;

public struct NetCubeSpawner : IComponentData
{
    public Entity Cube;
}

[DisallowMultipleComponent]
public class NetCubeSpawnerAuthoring : MonoBehaviour
{
    public GameObject Cube;

    class NetCubeSpawnerBaker : Baker<NetCubeSpawnerAuthoring>
    {
        public override void Bake(NetCubeSpawnerAuthoring authoring)
        {
            NetCubeSpawner component = default(NetCubeSpawner);
            component.Cube = GetEntity(authoring.Cube, TransformUsageFlags.Dynamic);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}

