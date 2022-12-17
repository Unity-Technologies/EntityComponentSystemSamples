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
                AddComponent<WritingPhysicsData>();
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsInitializeGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial class WritePhysicsTagsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<WritingPhysicsData>();
        }

        protected override void OnUpdate()
        {
            Dependency = new WritePhysicsTagsJob
            {
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld
            }.Schedule(Dependency);
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
