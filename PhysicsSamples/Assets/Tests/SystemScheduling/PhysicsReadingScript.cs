using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using UnityEngine;

namespace Unity.Physics.Tests
{
    public struct ReadingPhysicsData : IComponentData {}

    public class PhysicsReadingScript : MonoBehaviour
    {
        class PhysicsReadingScriptBaker : Baker<PhysicsReadingScript>
        {
            public override void Bake(PhysicsReadingScript authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ReadingPhysicsData>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial struct ReadPhysicsTagsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ReadingPhysicsData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new ReadPhysicsTagsJob
            {
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld
            }.Schedule(state.Dependency);
        }

        struct ReadPhysicsTagsJob : IJob
        {
            [ReadOnly] public PhysicsWorld PhysicsWorld;

            public void Execute()
            {
                var bodies = PhysicsWorld.Bodies;
                for (int i = 0; i < bodies.Length; i++)
                {
                    //Default tags are 0, 1 should be written in WritingPhysicsTagsSystem to each of them
                    var body = bodies[i];
                    Assertions.Assert.AreEqual(1, body.CustomTags, "CustomTags should be 1 on all bodies!");
                }
            }
        }
    }
}
