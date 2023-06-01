using Unity.Entities;
using UnityEngine;

namespace Streaming.SceneManagement.Common
{
    // Authoring class to mark an entity as relevant. This is used in samples where the position of an entity
    // (e.g. the player or camera) indicates which scene/sections to load.
    public class RelevantEntityAuthoring : MonoBehaviour
    {
        class Baker : Baker<RelevantEntityAuthoring>
        {
            public override void Bake(RelevantEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<RelevantEntity>(entity);
            }
        }
    }

    public struct RelevantEntity : IComponentData
    {
    }
}
