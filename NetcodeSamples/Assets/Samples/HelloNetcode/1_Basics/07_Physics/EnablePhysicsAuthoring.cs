using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnablePhysics : IComponentData { }

    [DisallowMultipleComponent]
    public class EnablePhysicsAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnablePhysicsAuthoring>
        {
            public override void Bake(EnablePhysicsAuthoring authoring)
            {
                EnablePhysics component = default(EnablePhysics);
                AddComponent(component);
            }
        }
    }
}
