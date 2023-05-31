using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Material = UnityEngine.Material;

namespace ImmediateMode
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public Mesh Mesh;
        public Material Material;
        public int NumSteps = 25;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new Config
                {
                    Material = authoring.Material,
                    Mesh = authoring.Mesh,
                    NumSteps = authoring.NumSteps,
                    ProjectionScale = 0.01f,
                });

                AddComponent<Shot>(entity);
            }
        }
    }

    [Serializable]
    public class Config : IComponentData
    {
        public int NumSteps;
        public Material Material;
        public Mesh Mesh;
        public float ProjectionScale;
    }

    public struct Shot : IComponentData
    {
        public bool TakeShot;
        public float3 Velocity;
    }

    public struct Projection : IComponentData
    {
    }
}
