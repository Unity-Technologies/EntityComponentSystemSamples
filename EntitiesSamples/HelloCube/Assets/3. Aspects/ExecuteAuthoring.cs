using UnityEngine;
using Unity.Entities;

namespace HelloCube.Aspects
{
    /* To ensure that the Aspects namespace systems only run in the Aspects scene,
     the Aspects systems require the existence of an Execute singleton to update. */
    [AddComponentMenu("HelloCube/AspectsExecute")]
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