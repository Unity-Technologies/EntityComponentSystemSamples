using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.Reparenting
{
    public partial struct ReparentingSystem : ISystem
    {
        bool attached;
        float timer;
        const float interval = 0.7f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            timer = interval;
            attached = true;
            state.RequireForUpdate<Execute.Reparenting>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            timer -= SystemAPI.Time.DeltaTime;
            if (timer > 0)
            {
                return;
            }
            timer = interval;

            var rotatorEntity = SystemAPI.GetSingletonEntity<RotationSpeed>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            if (attached)
            {
                // Detach all children from the rotator by removing the Parent component from the children.
                // (The next time TransformSystemGroup updates, it will update the Child buffer and transforms accordingly.)

                DynamicBuffer<Child> children = SystemAPI.GetBuffer<Child>(rotatorEntity);
                for (int i = 0; i < children.Length; i++)
                {
                    // Using an ECB is the best option here because calling EntityManager.RemoveComponent()
                    // instead would invalidate the DynamicBuffer, meaning we'd have to re-retrieve
                    // the DynamicBuffer after every EntityManager.RemoveComponent() call.
                    ecb.RemoveComponent<Parent>(children[i].Value);
                }
            }
            else
            {
                // Attach all the small cubes to the rotator by adding a Parent component to the cubes.
                // (The next time TransformSystemGroup updates, it will update the Child buffer and transforms accordingly.)

                foreach (var (transform, entity) in
                         SystemAPI.Query<RefRO<LocalTransform>>()
                             .WithNone<RotationSpeed>()
                             .WithEntityAccess())
                {
                    ecb.AddComponent(entity, new Parent { Value = rotatorEntity });
                }
            }

            ecb.Playback(state.EntityManager);

            attached = !attached;
        }
    }
}
