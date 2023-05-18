using Unity.Entities;
using UnityEngine;

public class ServerPhysicsSingletonAuthoring : MonoBehaviour {}

public struct ServerPhysicsSingleton : IComponentData {}

public class ServerPhysicsSingletonBaker : Baker<ServerPhysicsSingletonAuthoring>
{
    public override void Bake(ServerPhysicsSingletonAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ServerPhysicsSingleton {});
    }
}
