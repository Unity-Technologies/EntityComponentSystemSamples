using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics
{
    // When a sphere reaches the EndingPosition it will be teleported back to the StartingPosition
    public struct TeleportObject : IComponentData
    {
        public float3 StartingPosition;
        public float3 EndingPosition;
    }

    public class TeleportObjectAuthoring : MonoBehaviour
    {
        public float Offset = 15f; // Offset subtracted from the y position of the sphere

        class TeleportObjectBaker : Baker<TeleportObjectAuthoring>
        {
            public override void Bake(TeleportObjectAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TeleportObject
                {
                    StartingPosition = authoring.transform.position,
                    EndingPosition = authoring.transform.position - new Vector3(0, authoring.Offset, 0)
                });
            }
        }
    }
}
