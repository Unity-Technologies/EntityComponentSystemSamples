#if UNITY_EDITOR

using Unity.Entities;
using UnityEngine;

namespace Samples.Boids
{
    public class BoidObstacleAuthoring : MonoBehaviour
    {
        public class BoidObstacleAuthoringBaker : Baker<BoidObstacleAuthoring>
        {
            public override void Bake(BoidObstacleAuthoring authoring)
            {
                AddComponent(new BoidObstacle());
            }
        }
    }

    public struct BoidObstacle : IComponentData
    {
    }
}
#endif
