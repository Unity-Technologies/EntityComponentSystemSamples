using UnityEngine;
using Unity.Entities;

namespace HelloCube.JobEntity
{
    /* To ensure that the IJobEntity namespace systems only run in the IJobEntity scene,
     the IJobEntity systems require the existence of an Execute singleton to update. */
    [AddComponentMenu("HelloCube/JobEntityExecute")]
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