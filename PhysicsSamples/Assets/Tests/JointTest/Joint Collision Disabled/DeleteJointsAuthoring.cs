using Unity.Entities;
using UnityEngine;

public class DeleteJointsAuthoring : MonoBehaviour
{
    class DeleteJointsAuthoringBaker : Baker<DeleteJointsAuthoring>
    {
        public override void Bake(DeleteJointsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new DeleteJoints());
        }
    }
}

public struct DeleteJoints : IComponentData
{
}
