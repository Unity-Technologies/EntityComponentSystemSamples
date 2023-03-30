using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;

namespace Unity.Physics.SimulationEvents
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct DestroyBrokenJointsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RefRW<EndSimulationEntityCommandBufferSystem.Singleton> endSimulationEntityCommandBufferSystemSingleton = SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>();

            state.Dependency = new DestroyBrokenJointsJob
            {
                JointCompanionBuffer = SystemAPI.GetBufferLookup<PhysicsJointCompanion>(true),
                CommandBuffer = endSimulationEntityCommandBufferSystemSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged),
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        }

        [BurstCompile]
        private struct DestroyBrokenJointsJob : IImpulseEventsJob
        {
            [ReadOnly] public BufferLookup<PhysicsJointCompanion> JointCompanionBuffer;
            public EntityCommandBuffer CommandBuffer;

            public void Execute(ImpulseEvent impulseEvent)
            {
                Assertions.Assert.IsTrue(impulseEvent.Type == ConstraintType.Linear || impulseEvent.Type == ConstraintType.Angular, "Motorized constraints should not break!");
                Entity brokenJointEntity = impulseEvent.JointEntity;
                if (JointCompanionBuffer.HasBuffer(brokenJointEntity))
                {
                    var jointCompanionBuffer = JointCompanionBuffer[brokenJointEntity];
                    for (int i = 0; i < jointCompanionBuffer.Length; i++)
                    {
                        CommandBuffer.DestroyEntity(jointCompanionBuffer[i].JointEntity);
                    }
                }
                CommandBuffer.DestroyEntity(brokenJointEntity);
            }
        }
    }
}
