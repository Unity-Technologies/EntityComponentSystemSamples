using Unity.Entities;
using UnityEngine;

namespace KickBall
{
    public class BallAuthoring : MonoBehaviour
    {
        class Baker : Baker<BallAuthoring>
        {
            public override void Bake(BallAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent<Ball>(entity);
                AddComponent<Color>(entity);
            }
        }
    }

    public struct Ball : IComponentData
    {
    }
}