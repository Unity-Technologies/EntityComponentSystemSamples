using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Baking.Execute
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        public bool BakingTypes;
        public bool BlobAssetBaker;
        public bool BlobAssetBakingSystem;

        class Baker : Baker<ExecuteAuthoring>
        {
            public override void Bake(ExecuteAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                if (authoring.BakingTypes) AddComponent<BakingTypes>(entity);
                if (authoring.BlobAssetBaker) AddComponent<BlobAssetBaker>(entity);
                if (authoring.BlobAssetBakingSystem) AddComponent<BlobAssetBakingSystem>(entity);
            }
        }
    }

    public struct BakingTypes : IComponentData
    {
    }

    public struct BlobAssetBaker : IComponentData
    {
    }

    public struct BlobAssetBakingSystem : IComponentData
    {
    }
}
