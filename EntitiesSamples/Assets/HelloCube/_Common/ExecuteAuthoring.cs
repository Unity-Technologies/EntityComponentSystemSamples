using Unity.Entities;
using UnityEngine;

namespace HelloCube
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        public bool MainThread;
        public bool IJobEntity;
        public bool Aspects;
        public bool Prefabs;
        public bool IJobChunk;
        public bool Reparenting;
        public bool EnableableComponents;
        public bool GameObjectSync;
        public bool CrossQuery;
        public bool RandomSpawn;
        public bool FirstPersonController;
        public bool FixedTimestep;
        public bool StateChange;
        public bool ClosestTarget;

        class Baker : Baker<ExecuteAuthoring>
        {
            public override void Bake(ExecuteAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                if (authoring.MainThread) AddComponent<ExecuteMainThread>(entity);
                if (authoring.IJobEntity) AddComponent<ExecuteIJobEntity>(entity);
                if (authoring.Aspects) AddComponent<ExecuteAspects>(entity);
                if (authoring.Prefabs) AddComponent<ExecutePrefabs>(entity);
                if (authoring.IJobChunk) AddComponent<ExecuteIJobChunk>(entity);
                if (authoring.GameObjectSync) AddComponent<ExecuteGameObjectSync>(entity);
                if (authoring.Reparenting) AddComponent<ExecuteReparenting>(entity);
                if (authoring.EnableableComponents) AddComponent<ExecuteEnableableComponents>(entity);
                if (authoring.CrossQuery) AddComponent<ExecuteCrossQuery>(entity);
                if (authoring.RandomSpawn) AddComponent<ExecuteRandomSpawn>(entity);
                if (authoring.FirstPersonController) AddComponent<ExecuteFirstPersonController>(entity);
                if (authoring.FixedTimestep) AddComponent<ExecuteFixedTimestep>(entity);
                if (authoring.StateChange) AddComponent<ExecuteStateChange>(entity);
                if (authoring.ClosestTarget) AddComponent<ExecuteClosestTarget>(entity);
            }
        }
    }

    public struct ExecuteMainThread : IComponentData
    {
    }

    public struct ExecuteIJobEntity : IComponentData
    {
    }

    public struct ExecuteAspects : IComponentData
    {
    }

    public struct ExecutePrefabs : IComponentData
    {
    }

    public struct ExecuteIJobChunk : IComponentData
    {
    }

    public struct ExecuteGameObjectSync : IComponentData
    {
    }

    public struct ExecuteReparenting : IComponentData
    {
    }

    public struct ExecuteEnableableComponents : IComponentData
    {
    }

    public struct ExecuteCrossQuery : IComponentData
    {
    }

    public struct ExecuteRandomSpawn : IComponentData
    {
    }

    public struct ExecuteFirstPersonController : IComponentData
    {
    }

    public struct ExecuteFixedTimestep : IComponentData
    {
    }

    public struct ExecuteStateChange : IComponentData
    {
    }

    public struct ExecuteClosestTarget : IComponentData
    {
    }
}
