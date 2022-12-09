using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;

namespace Unity.Physics.SimulationEvents
{
    public struct ImpulseEventBufferData : IComponentData
    {
        public NativeList<ImpulseEvent> ImpulseEvents;
    }

    [UpdateInGroup(typeof(PhysicsSimulationGroup), OrderLast = true)]
    public partial struct ImpulseEventBufferSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentData(state.SystemHandle, new ImpulseEventBufferData
            {
                ImpulseEvents = new NativeList<ImpulseEvent>(Allocator.Persistent)
            });
        }

        public void OnDestroy(ref SystemState state)
        {
            ref var data = ref state.EntityManager.GetComponentDataRW<ImpulseEventBufferData>(state.SystemHandle).ValueRW;

            if (data.ImpulseEvents.IsCreated)
                data.ImpulseEvents.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            ref var data = ref state.EntityManager.GetComponentDataRW<ImpulseEventBufferData>(state.SystemHandle).ValueRW;

            data.ImpulseEvents.Clear();
            var job = new CollectImpulseEvents
            {
                ImpulseEvents = data.ImpulseEvents
            };

            state.Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public struct CollectImpulseEvents : IImpulseEventsJob
        {
            public NativeList<ImpulseEvent> ImpulseEvents;
            public void Execute(ImpulseEvent impulseEvent) => ImpulseEvents.Add(impulseEvent);
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    // Note EndSimulationEntityCommandBufferSystem is a managed type so we can't use ISystem
    public partial class DestroyBrokenJointsSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem m_EndSimulationEntityCommandBufferSystem;
        //private ImpulseEventBufferSystem m_ImpulseEventBufferSystem;

        [BurstCompile]
        private struct DestroyBrokenJointsJob : IJobParallelFor
        {
            [ReadOnly] public NativeList<ImpulseEvent> ImpulseEvents;
            [ReadOnly] public BufferLookup<PhysicsJointCompanion> JointCompanionBuffer;
            public EntityCommandBuffer.ParallelWriter commandBufferParallel;

            public void Execute(int index)
            {
                var brokenJointEntity = ImpulseEvents[index].JointEntity;
                if (JointCompanionBuffer.HasBuffer(brokenJointEntity))
                {
                    var jointCompanionBuffer = JointCompanionBuffer[brokenJointEntity];
                    for (int i = 0; i < jointCompanionBuffer.Length; ++i)
                        commandBufferParallel.DestroyEntity(index, jointCompanionBuffer[i].JointEntity);
                }

                commandBufferParallel.DestroyEntity(index, brokenJointEntity);
            }
        }

        protected override void OnCreate()
        {
            m_EndSimulationEntityCommandBufferSystem =
                World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var impulseEventBufferSystem = World.GetOrCreateSystem<ImpulseEventBufferSystem>();
            var impulseEventBufferData = EntityManager.GetComponentData<ImpulseEventBufferData>(impulseEventBufferSystem);
            if (impulseEventBufferData.ImpulseEvents.Length == 0)
                return;

            Dependency = new DestroyBrokenJointsJob
            {
                ImpulseEvents = impulseEventBufferData.ImpulseEvents,
                JointCompanionBuffer = GetBufferLookup<PhysicsJointCompanion>(true),
                commandBufferParallel =
                    m_EndSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()
            }.Schedule(impulseEventBufferData.ImpulseEvents.Length, 64, Dependency);
            Dependency.Complete();
            m_EndSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
