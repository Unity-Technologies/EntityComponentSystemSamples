using Unity.Entities;

namespace Samples.HelloNetcode
{
    public struct EnableCharacterOrientation : IComponentData { }

    [UnityEngine.DisallowMultipleComponent]
    public class EnableCharacterOrientationAuthoring : UnityEngine.MonoBehaviour
    {
        class Baker : Baker<EnableCharacterOrientationAuthoring>
        {
            public override void Bake(EnableCharacterOrientationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnableCharacterOrientation>(entity);
            }
        }
    }
}
