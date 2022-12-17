using System;
using Unity.Entities;
using UnityEngine;

namespace StateChange
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
                AddComponent(new Config
                {
                    Prefab = GetEntity(authoring.Prefab),
                    Size = authoring.Size,
                    Radius = authoring.Radius,
                    UpdateType = authoring.UpdateType,
                });

                switch (authoring.UpdateType)
                {
                    case Config.UpdateTypeEnum.ValueBranching:
                        AddComponent<SetStateValueChangeSystem.EnableSingleton>();
                        break;
                    case Config.UpdateTypeEnum.StructuralChange:
                        AddComponent<SetStateStructuralChangeSystem.EnableSingleton>();
                        break;
                    case Config.UpdateTypeEnum.Enableable:
                        AddComponent<SetStateEnableableSystem.EnableSingleton>();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
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
}