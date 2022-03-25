using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using UnityEngine;

namespace Unity.Physics.Tests
{
    public struct WritingPhysicsData : IComponentData {}

    public class PhysicsWritingScript : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new WritingPhysicsData());
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(BuildPhysicsWorld))]
    [UpdateBefore(typeof(StepPhysicsWorld))]
    public partial class WritePhysicsTagsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(WritingPhysicsData) }
            }));
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.RegisterPhysicsRuntimeSystemReadWrite();
        }

        protected override void OnUpdate()
        {
            Dependency = new WritePhysicsTagsJob
            {
                PhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld
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
