// An representation of the trunk of a tree. This is a MonoBehaviour component that is on the Tree prefabs, so that
// less prefab modifications are needed during re-growth.
using Unity.Entities;
using UnityEngine;

namespace Unity.Physics
{
    public class TagTrunkAuthoring : MonoBehaviour
    {
        class TagTrunkBaker : Baker<TagTrunkAuthoring>
        {
            public override void Bake(TagTrunkAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, TreeState.Default);
                AddComponent(entity, new TreeTrunkTag());
                AddComponent(entity, new EnableTreeDeath());
                SetComponentEnabled<EnableTreeDeath>(entity, false); //always bake as disabled
            }
        }
    }

    public struct TreeTrunkTag : IComponentData {}
}
