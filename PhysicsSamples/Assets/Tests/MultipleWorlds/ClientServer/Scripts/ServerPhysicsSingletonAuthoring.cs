using Unity.Entities;
using UnityEngine;

public class ServerPhysicsSingletonAuthoring : MonoBehaviour {}

public struct ServerPhysicsSingleton : IComponentData {}

public class ServerPhysicsSingletonBaker : Baker<ServerPhysicsSingletonAuthoring>
{
    public override void Bake(ServerPhysicsSingletonAuthoring authoring)
    {
        AddComponent(new ServerPhysicsSingleton {});
    }
}
