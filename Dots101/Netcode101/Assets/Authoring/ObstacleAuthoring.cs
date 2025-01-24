using Unity.Entities;
using UnityEngine;

namespace KickBall
{
    public class ObstacleAuthoring : MonoBehaviour
    {
        class Baker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.None);
                AddComponent<Obstacle>(entity);
            }
        }
    }

    public struct Obstacle : IComponentData
    {
    }
}
