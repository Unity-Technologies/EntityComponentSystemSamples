using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

public struct ConvertToDifferentMotion : IComponentData
{
    public int ConvertIn;
    public ConversionSettings ConversionSettings;
}

[UnityEngine.DisallowMultipleComponent]
public class ConvertToDifferentMotionAuthoring : UnityEngine.MonoBehaviour
{
    [RegisterBinding(typeof(ConvertToDifferentMotion), "ConvertIn")]
    public int ConvertIn;
    [RegisterBinding(typeof(ConvertToDifferentMotion), "ConversionSettings")]
    public ConversionSettings ConversionSettings;

    class ConvertToDifferentMotionBaker : Baker<ConvertToDifferentMotionAuthoring>
    {
        public override void Bake(ConvertToDifferentMotionAuthoring authoring)
        {
            ConvertToDifferentMotion component = default(ConvertToDifferentMotion);
            component.ConvertIn = authoring.ConvertIn;
            component.ConversionSettings = authoring.ConversionSettings;
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}

public enum ConversionSettings : byte
{
    ConvertToStatic,
    ConvertToStaticDontInvalidateNumExpectedEvents,
    ConvertToDynamic
}

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(TriggerEventCheckerSystem))]
public partial struct ConvertToStaticSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConvertToDifferentMotion>();
    }

    public partial struct ConvertToDifferentMotionJob : IJobEntity
    {
        public EntityCommandBuffer CommandBuffer;

        public void Execute(Entity entity, ref ConvertToDifferentMotion tag, ref TriggerEventChecker checkerComponent, ref PhysicsCollider collider)
        {
            if (--tag.ConvertIn == 0)
            {
                if (tag.ConversionSettings == ConversionSettings.ConvertToStatic ||
                    tag.ConversionSettings == ConversionSettings.ConvertToStaticDontInvalidateNumExpectedEvents)
                {
                    CommandBuffer.RemoveComponent<PhysicsVelocity>(entity);

                    if (tag.ConversionSettings == ConversionSettings.ConvertToStatic)
                    {
                        checkerComponent.NumExpectedEvents = 0;
                    }
                }
                else
                {
                    CommandBuffer.AddComponent(entity, new PhysicsVelocity
                    {
                        Angular = float3.zero,
                        Linear = float3.zero
                    });

                    if (collider.Value.Value.Type == ColliderType.Compound)
                    {
                        unsafe
                        {
                            checkerComponent.NumExpectedEvents = ((CompoundCollider*)collider.ColliderPtr)->NumChildren;
                        }
                    }
                    else
                    {
                        checkerComponent.NumExpectedEvents = 1;
                    }
                }
                CommandBuffer.RemoveComponent<ConvertToDifferentMotion>(entity);
            }
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            var jobHandle = new ConvertToDifferentMotionJob
            {
                CommandBuffer = commandBuffer
            }.Schedule(state.Dependency);

            state.Dependency = jobHandle;
            jobHandle.Complete();

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
