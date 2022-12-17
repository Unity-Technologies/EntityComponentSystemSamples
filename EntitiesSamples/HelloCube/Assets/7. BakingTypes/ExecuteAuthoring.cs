using UnityEngine;
using Unity.Entities;

namespace HelloCube.BakingTypes
{
    /* To ensure that the BakingTypes namespace systems only run in the BakingTypes scene,
     the BakingTypes systems require the existence of an Execute singleton to update. */
    [AddComponentMenu("HelloCube/BakingTypesExecute")]
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