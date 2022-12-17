using Unity.Entities;
using UnityEngine;

namespace ClosestTarget
{
    public class TargetAuthoring : MonoBehaviour
    {
        class Baker : Baker<TargetAuthoring>
        {
            public override void Bake(TargetAuthoring authoring)
            {
                AddComponent<Target>();
            }
        }
    }

    public struct Target : IComponentData
    {
        public Entity Value;
    }
}