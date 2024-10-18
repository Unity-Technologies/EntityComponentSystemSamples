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
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct DestroyTriggerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DestroyTrigger>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    public partial struct DestroyTriggerJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorld World;
        public EntityCommandBuffer CommandBuffer;
        public ComponentLookup<TriggerEventChecker> TriggerEventComponentLookup;

        public void Execute(Entity entity, ref DestroyTrigger destroyComponent)
        {
            if (--destroyComponent.FramesToDestroyIn != 0)
            {
                return;
            }

            CommandBuffer.DestroyEntity(entity);

            int triggerRbIndex = World.GetRigidBodyIndex(entity);
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
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency.Complete();

        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            var jobHandle = new DestroyTriggerJob
            {
                World = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                CommandBuffer = commandBuffer,
                TriggerEventComponentLookup = SystemAPI.GetComponentLookup<TriggerEventChecker>()
            }.Schedule(state.Dependency);

            state.Dependency = jobHandle;
            jobHandle.Complete();

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
