using Unity.Burst;
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

    public partial struct DestroyTriggerJob : IJobEntity
    {
        public PhysicsWorld World;
        public EntityCommandBuffer CommandBuffer;
        public ComponentLookup<TriggerEventChecker> TriggerEventComponentLookup;

        public void Execute(Entity e, ref DestroyTrigger destroyComponent)
        {
            if (--destroyComponent.FramesToDestroyIn != 0)
            {
                return;
            }

            CommandBuffer.DestroyEntity(e);

            int triggerRbIndex = World.GetRigidBodyIndex(e);
            RigidBody triggerBody = World.Bodies[triggerRbIndex];

            // Remove the TriggerEventCheckerComponent of all overlapping bodies
            OverlapAabbInput input = new OverlapAabbInput
            {
                Aabb = triggerBody.CalculateAabb(),
                Filter = CollisionFilter.Default
            };

            NativeList<int> overlappingBodies = new NativeList<int>(Allocator.Temp);

            World.CollisionWorld.OverlapAabb(input, ref overlappingBodies);

            for (int i = 0; i < overlappingBodies.Length; i++)
            {
                Entity overlappingEntity = World.Bodies[overlappingBodies[i]].Entity;
                if (TriggerEventComponentLookup.HasComponent(overlappingEntity))
                {
                    CommandBuffer.RemoveComponent<TriggerEventChecker>(overlappingEntity);
                }
            }
        }
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        Dependency.Complete();

        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            new DestroyTriggerJob
            {
                World = physicsWorld,
                CommandBuffer = commandBuffer,
                TriggerEventComponentLookup = SystemAPI.GetComponentLookup<TriggerEventChecker>()
            }.Run();

            commandBuffer.Playback(EntityManager);
        }
    }
}
