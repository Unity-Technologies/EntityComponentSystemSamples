using UnityEngine;
using Unity.Entities;

namespace HelloCube.GameObjectSync
{
    /* To ensure that the GameObjectSync namespace systems only run in the GameObjectSync scene,
     the GameObjectSync systems require the existence of an Execute singleton to update. */
    [AddComponentMenu("HelloCube/GameObjectSyncExecute")]
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