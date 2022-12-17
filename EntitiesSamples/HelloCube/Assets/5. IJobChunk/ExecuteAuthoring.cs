using UnityEngine;
using Unity.Entities;

namespace HelloCube.JobChunk
{
    /* To ensure that the JobChunk namespace systems only run in the JobChunk scene,
     the JobChunk systems require the existence of an Execute singleton to update. */
    [AddComponentMenu("HelloCube/JobChunkExecute")]
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