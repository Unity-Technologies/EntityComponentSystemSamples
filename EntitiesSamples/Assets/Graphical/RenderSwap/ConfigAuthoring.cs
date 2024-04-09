using Unity.Entities;
using UnityEngine;

namespace Graphical.RenderSwap
{
    public class ConfigAuthoring : MonoBehaviour
    {
        [Range(1, 1000)] public int Size;

        public GameObject StateOn;
        public GameObject StateOff;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Config
                {
                    Size = authoring.Size,
                    StateOn = GetEntity(authoring.StateOn, TransformUsageFlags.Dynamic),
                    StateOff = GetEntity(authoring.StateOff, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    public struct Config : IComponentData
    {
        public int Size;
        public Entity StateOn;
        public Entity StateOff;
    }
}
