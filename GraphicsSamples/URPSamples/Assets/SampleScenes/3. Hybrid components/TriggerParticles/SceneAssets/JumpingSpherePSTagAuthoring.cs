using Unity.Entities;
using UnityEngine;

public struct JumpingSpherePSTag : IComponentData
{
}

[DisallowMultipleComponent]
public class JumpingSpherePSTagAuthoring : MonoBehaviour
{
    class JumpingSpherePSTagBaker : Baker<JumpingSpherePSTagAuthoring>
    {
        public override void Bake(JumpingSpherePSTagAuthoring authoring)
        {
            JumpingSpherePSTag component = default(JumpingSpherePSTag);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
        }
    }
}
