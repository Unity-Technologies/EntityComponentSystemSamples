using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnableAnimation : IComponentData
    {
    }

    public class EnableAnimationAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnableAnimationAuthoring>
        {
            public override void Bake(EnableAnimationAuthoring authoring)
            {
                AddComponent<EnableAnimation>(GetEntity(TransformUsageFlags.None));
            }
        }
    }
}
