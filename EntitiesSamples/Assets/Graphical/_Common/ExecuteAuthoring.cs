using UnityEngine;
using Unity.Entities;

namespace Graphical
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        public bool AnimationWithGameObjects;
        public bool RenderSwap;
        public bool ShaderGraph;
        public bool Splines;
        public bool TextureUpdate;
        public bool WireframeBlob;

        class Baker : Baker<ExecuteAuthoring>
        {
            public override void Bake(ExecuteAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                if (authoring.AnimationWithGameObjects) AddComponent<ExecuteAnimationWithGameObjects>(entity);
                if (authoring.RenderSwap) AddComponent<ExecuteRenderSwap>(entity);
                if (authoring.ShaderGraph) AddComponent<ExecuteShaderGraph>(entity);
                if (authoring.Splines) AddComponent<ExecuteSplines>(entity);
                if (authoring.WireframeBlob) AddComponent<ExecuteWireframeBlob>(entity);
            }
        }
    }

    public struct ExecuteAnimationWithGameObjects : IComponentData
    {
    }

    public struct ExecuteRenderSwap : IComponentData
    {
    }

    public struct ExecuteShaderGraph : IComponentData
    {
    }

    public struct ExecuteSplines : IComponentData
    {
    }

    public struct ExecuteWireframeBlob : IComponentData
    {
    }

}
