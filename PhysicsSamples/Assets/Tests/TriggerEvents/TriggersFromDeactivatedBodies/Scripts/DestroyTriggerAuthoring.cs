using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

public struct DestroyTrigger : IComponentData
{
    public int FramesToDestroyIn;
}

[UnityEngine.DisallowMultipleComponent]
public class DestroyTriggerAuthoring : UnityEngine.MonoBehaviour
{
    [RegisterBinding(typeof(DestroyTrigger), "FramesToDestroyIn")]
    public int FramesToDestroyIn;

    class DestroyTriggerBaker : Baker<DestroyTriggerAuthoring>
    {
        public override void Bake(DestroyTriggerAuthoring authoring)
        {
            DestroyTrigger component = default(DestroyTrigger);
            component.FramesToDestroyIn = authoring.FramesToDestroyIn;
            AddComponent(component);
        }
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class DestroyTriggerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<DestroyTrigger>();
    }

    protected override void OnUpdate()
    {
        Dependency.Complete();

        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            var physicsWorld = GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
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
