using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Modify
{
    public class ColliderTypeAuthoring : MonoBehaviour
    {
        public GameObject PhysicsColliderPrefabA;
        public GameObject PhysicsColliderPrefabB;
        [Range(0, 10)] public float TimeToSwap = 1.0f;

        class Baker : Baker<ColliderTypeAuthoring>
        {
            public override void Bake(ColliderTypeAuthoring authoring)
            {
                if (authoring.PhysicsColliderPrefabA == null || authoring.PhysicsColliderPrefabB == null)
                {
                    return;
                }

                var entityA = GetEntity(authoring.PhysicsColliderPrefabA, TransformUsageFlags.Dynamic);
                var entityB = GetEntity(authoring.PhysicsColliderPrefabB, TransformUsageFlags.Dynamic);

                if (entityA == Entity.Null || entityB == Entity.Null)
                {
                    return;
                }

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ChangeColliderType()
                {
                    // These 2 are filled in in the baking system
                    //ColliderA = colliderA,
                    //ColliderB = colliderB,
                    EntityA = entityA,
                    EntityB = entityB,
                    TimeToSwap = authoring.TimeToSwap,
                    LocalTime = authoring.TimeToSwap,
                });
            }
        }
    }

    public struct ChangeColliderType : IComponentData
    {
        public PhysicsCollider ColliderA;
        public PhysicsCollider ColliderB;
        public Entity EntityA;
        public Entity EntityB;
        public float TimeToSwap;
        internal float LocalTime;
    }
}
