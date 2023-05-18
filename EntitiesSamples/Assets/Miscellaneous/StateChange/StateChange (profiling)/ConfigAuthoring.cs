using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Miscellaneous.StateChange
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public uint Size;
        public float Radius;
        public Config.UpdateTypeEnum UpdateType;

        public class ConfigBaker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new Config
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                    Size = authoring.Size,
                    Radius = authoring.Radius,
                    UpdateType = authoring.UpdateType,
                });

                switch (authoring.UpdateType)
                {
                    case Config.UpdateTypeEnum.ValueBranching:
                        AddComponent<SetStateValueChangeSystem.EnableSingleton>(entity);
                        break;
                    case Config.UpdateTypeEnum.StructuralChange:
                        AddComponent<SetStateStructuralChangeSystem.EnableSingleton>(entity);
                        break;
                    case Config.UpdateTypeEnum.Enableable:
                        AddComponent<SetStateEnableableSystem.EnableSingleton>(entity);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                AddComponent<Hit>(entity);
            }
        }
    }

    public struct Config : IComponentData
    {
        public enum UpdateTypeEnum
        {
            ValueBranching,
            StructuralChange,
            Enableable,
        }

        public Entity Prefab;
        public uint Size;
        public float Radius;
        public UpdateTypeEnum UpdateType;
    }

    public struct Hit : IComponentData
    {
        public float3 Value;
    }

}
