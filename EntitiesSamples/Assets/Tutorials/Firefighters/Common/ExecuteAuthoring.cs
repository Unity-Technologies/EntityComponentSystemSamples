using Unity.Entities;
using UnityEngine;

namespace Tutorials.Firefighters
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        public bool ExecuteHeatSystem;
        public bool ExecuteBotSystem;
        public bool ExecuteBucketSystem;
        public bool ExecuteTeamSystem;
        public bool ExecuteUISystem;
        public bool ExecuteAnimationSystem;
        
        class Baker : Baker<ExecuteAuthoring>
        {
            public override void Bake(ExecuteAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                if (authoring.ExecuteHeatSystem)
                {
                    AddComponent<ExecuteHeat>(entity);
                }

                if (authoring.ExecuteBotSystem)
                {
                    AddComponent<ExecuteBot>(entity);
                }

                if (authoring.ExecuteBucketSystem)
                {
                    AddComponent<ExecuteBucket>(entity);
                }

                if (authoring.ExecuteTeamSystem)
                {
                    AddComponent<ExecuteTeam>(entity);
                }
                
                if (authoring.ExecuteUISystem)
                {
                    AddComponent<ExecuteUI>(entity);
                }
                
                if (authoring.ExecuteAnimationSystem)
                {
                    AddComponent<ExecuteAnimation>(entity);
                }
            }
        }
    }

    public struct ExecuteHeat : IComponentData
    {
    }

    public struct ExecuteBot : IComponentData
    {
    }

    public struct ExecuteBucket : IComponentData
    {
    }

    public struct ExecuteTeam : IComponentData
    {
    }
    
    public struct ExecuteUI : IComponentData
    {
    }
    
    public struct ExecuteAnimation : IComponentData
    {
    }
}
