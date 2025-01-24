using Unity.Entities;
using UnityEngine;

namespace GravityWell
{
    public class BallAuthoring : MonoBehaviour
    {
        public class Baker : Baker<BallAuthoring>
        {
            public override void Bake(BallAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Ball>(entity);
            }
        }
    }

    public struct Ball : IComponentData
    {
    }
}