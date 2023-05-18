using Unity.Entities;
using UnityEngine;

public struct JumpingSphereTag : IComponentData
{
}

[DisallowMultipleComponent]
public class JumpingSphereTagAuthoring : MonoBehaviour
{
    class JumpingSphereTagBaker : Baker<JumpingSphereTagAuthoring>
    {
        public override void Bake(JumpingSphereTagAuthoring authoring)
        {
            JumpingSphereTag component = default(JumpingSphereTag);
            AddComponent(component);
        }
    }
}
