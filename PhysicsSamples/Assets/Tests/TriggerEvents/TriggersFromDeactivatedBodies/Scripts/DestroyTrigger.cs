using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

[GenerateAuthoringComponent]
public struct DestroyTrigger : IComponentData
{
    public int FramesToDestroyIn;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
public partial class DestroyTriggerSystem : SystemBase
{
    private BuildPhysicsWorld m_BuildPhysicsWorld;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(DestroyTrigger) }
        }));
    }

    protected override void OnUpdate()
    {
        Dependency.Complete();

        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            var physicsWorld = m_BuildPhysicsWorld.PhysicsWorld;
            Entities
                .WithName("DestroyTriggerJob")
                .WithReadOnly(physicsWorld)
                .WithBurst()
                .ForEach((ref Entity e, ref DestroyTrigger destroyComponent) =>
                {
                    if (--destroyComponent.FramesToDestroyIn != 0)
                    {
                        return;
                    }

                    commandBuffer.DestroyEntity(e);

                    int triggerRbIndex = physicsWorld.GetRigidBodyIndex(e);
                    RigidBody triggerBody = physicsWorld.Bodies[triggerRbIndex];

                    // Remove the TriggerEventCheckerComponent of all overlapping bodies
                    OverlapAabbInput input = new OverlapAabbInput
                    {
                        Aabb = triggerBody.CalculateAabb(),
                        Filter = CollisionFilter.Default
                    };

                    NativeList<int> overlappingBodies = new NativeList<int>(Allocator.Temp);

                    physicsWorld.CollisionWorld.OverlapAabb(input, ref overlappingBodies);

                    for (int i = 0; i < overlappingBodies.Length; i++)
                    {
                        Entity overlappingEntity = physicsWorld.Bodies[overlappingBodies[i]].Entity;
                        if (HasComponent<TriggerEventChecker>(overlappingEntity))
                        {
                            commandBuffer.RemoveComponent<TriggerEventChecker>(overlappingEntity);
                        }
                    }
                }).Run();

            commandBuffer.Playback(EntityManager);
        }
    }
}
