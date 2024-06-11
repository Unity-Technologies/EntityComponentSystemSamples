using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using UnityEngine;

namespace Unity.Physics.Tests
{
    public struct WritingPhysicsData : IComponentData {}

    public class PhysicsWritingScript : MonoBehaviour
    {
        class PhysicsWritingScriptBaker : Baker<PhysicsWritingScript>
        {
            public override void Bake(PhysicsWritingScript authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<WritingPhysicsData>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsInitializeGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial struct WritePhysicsTagsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WritingPhysicsData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new WritePhysicsTagsJob
            {
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld
            }.Schedule(state.Dependency);
        }

        struct WritePhysicsTagsJob : IJob
        {
            public PhysicsWorld PhysicsWorld;

            public void Execute()
            {
                var bodies = PhysicsWorld.Bodies;
                for (int i = 0; i < bodies.Length; i++)
                {
                    // Default tags are 0, write 1 to each of them
                    var body = bodies[i];
                    body.CustomTags = 1;
                    bodies[i] = body;
                }
            }
        }
    }
}
