using UnityEngine;
using Unity.Entities;

namespace HelloCube.BlobAssets
{
    /* To ensure that the BlobAssets namespace systems only run in the BlobAssets scene,
     the BlobAssets systems require the existence of an Execute singleton to update. */
    [AddComponentMenu("HelloCube/BlobAssetsExecute")]
    public class ExecuteAuthoring : MonoBehaviour
    {
        public class Baker : Baker<ExecuteAuthoring>
        {
            public override void Bake(ExecuteAuthoring authoring)
            {
                AddComponent<Execute>();
            }
        }
    }

    public struct Execute : IComponentData
    {
    }
}