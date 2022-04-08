using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using UnityEngine;

namespace Unity.Physics.Tests
{
    public struct ReadingPhysicsData : IComponentData {}

    public class PhysicsReadingScript : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new ReadingPhysicsData());
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(StepPhysicsWorld))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial class ReadPhysicsTagsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ReadingPhysicsData) }
            }));
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.RegisterPhysicsRuntimeSystemReadOnly();
        }

        protected override void OnUpdate()
        {
            Dependency = new ReadPhysicsTagsJob
            {
                PhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld
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
                    // Default tags are 0, 1 should be written in WritingPhysicsTagsSystem to each of them
                    var body = bodies[i];
                    Assertions.Assert.AreEqual(1, body.CustomTags, "CustomTags should be 1 on all bodies!");
                }
            }
        }
    }
}
