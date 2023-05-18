using UnityEngine;
using Unity.Entities;

namespace Miscellaneous.Execute
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        public bool AnimationWithGameObjects;
        public bool ClosestTarget;
        public bool CrossQuery;
        public bool FirstPersonController;
        public bool FixedTimestep;
        public bool PrefabInitializer;
        public bool ProceduralMesh;
        public bool RandomSpawn;
        public bool RenderSwap;
        public bool ShaderGraph;
        public bool Splines;
        public bool StateChangeEnableable;
        public bool StateChangeStructural;
        public bool StateChangeValue;
        public bool StateChangeProfiling;
        public bool TextureUpdate;

        class Baker : Baker<ExecuteAuthoring>
        {
            public override void Bake(ExecuteAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                if (authoring.AnimationWithGameObjects) AddComponent<AnimationWithGameObjects>(entity);
                if (authoring.ClosestTarget) AddComponent<ClosestTarget>(entity);
                if (authoring.CrossQuery) AddComponent<CrossQuery>(entity);
                if (authoring.FirstPersonController) AddComponent<FirstPersonController>(entity);
                if (authoring.FixedTimestep) AddComponent<FixedTimestep>(entity);
                if (authoring.PrefabInitializer) AddComponent<PrefabInitializer>(entity);
                if (authoring.ProceduralMesh) AddComponent<ProceduralMesh>(entity);
                if (authoring.RandomSpawn) AddComponent<RandomSpawn>(entity);
                if (authoring.RenderSwap) AddComponent<RenderSwap>(entity);
                if (authoring.ShaderGraph) AddComponent<ShaderGraph>(entity);
                if (authoring.Splines) AddComponent<Splines>(entity);
                if (authoring.StateChangeEnableable) AddComponent<StateChangeEnableable>(entity);
                if (authoring.StateChangeStructural) AddComponent<StateChangeStructural>(entity);
                if (authoring.StateChangeValue) AddComponent<StateChangeValue>(entity);
                if (authoring.StateChangeProfiling) AddComponent<StateChangeProfiling>(entity);
                if (authoring.TextureUpdate) AddComponent<TextureUpdater>(entity);
            }
        }
    }

    public struct AnimationWithGameObjects : IComponentData
    {
    }

    public struct ClosestTarget : IComponentData
    {
    }

    public struct CrossQuery : IComponentData
    {
    }

    public struct FirstPersonController : IComponentData
    {
    }

    public struct FixedTimestep : IComponentData
    {
    }

    public struct PrefabInitializer : IComponentData
    {
    }

    public struct ProceduralMesh : IComponentData
    {
    }

    public struct RandomSpawn : IComponentData
    {
    }

    public struct RenderSwap : IComponentData
    {
    }

    public struct ShaderGraph : IComponentData
    {
    }

    public struct Splines : IComponentData
    {
    }

    public struct StateChangeEnableable : IComponentData
    {
    }

    public struct StateChangeStructural : IComponentData
    {
    }

    public struct StateChangeValue : IComponentData
    {
    }

    public struct StateChangeProfiling : IComponentData
    {
    }

    public struct TextureUpdater : IComponentData
    {
    }
}
