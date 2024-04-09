using Unity.Entities;
using UnityEngine;

namespace Streaming.SceneManagement.Common
{
    // Authoring class to mark an entity as relevant. This is used in samples where the position of an entity
    // (e.g. the player or camera) indicates which scene/sections to load.
    public class RelevantAuthoring : MonoBehaviour
    {
        class Baker : Baker<RelevantAuthoring>
        {
            public override void Bake(RelevantAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Relevant>(entity);
            }
        }
    }

    public struct Relevant : IComponentData
    {
    }
}
