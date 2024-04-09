using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace HelloCube.StateChange
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public uint Size;
        public float Radius;
        public Mode Mode;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new Config
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                    Size = authoring.Size,
                    Radius = authoring.Radius,
                    Mode = authoring.Mode,
                });
                AddComponent<Hit>(entity);
#if UNITY_EDITOR
                AddComponent<StateChangeProfilerModule.FrameData>(entity);
#endif
            }
        }
    }

    public struct Config : IComponentData
    {
        public Entity Prefab;
        public uint Size;
        public float Radius;
        public Mode Mode;
    }

    public struct Hit : IComponentData
    {
        public float3 Value;
        public bool HitChanged;
    }

    public struct Spin : IComponentData, IEnableableComponent
    {
        public bool IsSpinning;
    }

    public enum Mode
    {
        VALUE = 1,
        STRUCTURAL_CHANGE = 2,
        ENABLEABLE_COMPONENT = 3
    }
}
