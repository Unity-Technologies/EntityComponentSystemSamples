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
                AddComponent<ReadingPhysicsData>();
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial class ReadPhysicsTagsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<ReadingPhysicsData>();
        }

        protected override void OnUpdate()
        {
            Dependency = new ReadPhysicsTagsJob
            {
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld
            }.Schedule(Dependency);
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
