// A representation of the top of a tree. This is a MonoBehaviour component that is on the Tree prefabs, so that
// less prefab modifications are needed during re-growth.
using Unity.Entities;
using UnityEngine;

namespace Unity.Physics
{
    public class TreeTopAuthoring : MonoBehaviour
    {
        class TreeTopBaker : Baker<TreeTopAuthoring>
        {
            public override void Bake(TreeTopAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, TreeState.Default);
                AddComponent(entity, new TreeTopTag());
                AddComponent(entity, new EnableTreeDeath());
                SetComponentEnabled<EnableTreeDeath>(entity, false); //always bake as disabled
                AddComponent(entity, new EnableTreeGrowth());
                SetComponentEnabled<EnableTreeGrowth>(entity, false); //always bake as disabled
            }
        }
    }
    public struct TreeTopTag : IComponentData {}
    public struct EnableTreeDeath : IComponentData, IEnableableComponent {}
    public struct EnableTreeGrowth : IComponentData, IEnableableComponent {}
}
