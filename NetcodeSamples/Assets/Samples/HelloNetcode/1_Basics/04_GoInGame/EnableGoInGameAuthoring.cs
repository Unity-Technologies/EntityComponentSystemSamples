using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnableGoInGame : IComponentData { }

    [DisallowMultipleComponent]
    public class EnableGoInGameAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnableGoInGameAuthoring>
        {
            public override void Bake(EnableGoInGameAuthoring authoring)
            {
                EnableGoInGame component = default(EnableGoInGame);
                AddComponent(component);
            }
        }
    }
}
