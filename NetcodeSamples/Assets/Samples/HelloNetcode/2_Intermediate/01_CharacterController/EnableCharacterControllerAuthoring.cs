using Unity.Entities;

namespace Samples.HelloNetcode
{
    public struct EnableCharacterController : IComponentData { }

    [UnityEngine.DisallowMultipleComponent]
    public class EnableCharacterControllerAuthoring : UnityEngine.MonoBehaviour
    {
        class Baker : Baker<EnableCharacterControllerAuthoring>
        {
            public override void Bake(EnableCharacterControllerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnableCharacterController>(entity);
            }
        }
    }
}
