using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnablePrespawn : IComponentData { }

    [DisallowMultipleComponent]
    public class EnablePrespawnAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnablePrespawnAuthoring>
        {
            public override void Bake(EnablePrespawnAuthoring authoring)
            {
                EnablePrespawn component = default(EnablePrespawn);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
