using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

[GenerateAuthoringComponent]
public struct ConvertToDifferentMotion : IComponentData
{
    public int ConvertIn;
    public ConversionSettings ConversionSettings;
}

public enum ConversionSettings : byte
{
    ConvertToStatic,
    ConvertToStaticDontInvalidateNumExpectedEvents,
    ConvertToDynamic
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(TriggerEventCheckerSystem))]
public partial class ConvertToStaticSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ConvertToDifferentMotion) }
        }));
    }

    protected override void OnUpdate()
    {
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            Entities
                .WithName("ConvertToDifferentMotion")
                .ForEach((ref Entity e, ref ConvertToDifferentMotion tag, ref TriggerEventChecker checkerComponent, ref PhysicsCollider pc) =>
                {
                    if (--tag.ConvertIn == 0)
                    {
                        if (tag.ConversionSettings == ConversionSettings.ConvertToStatic ||
                            tag.ConversionSettings == ConversionSettings.ConvertToStaticDontInvalidateNumExpectedEvents)
                        {
                            commandBuffer.RemoveComponent<PhysicsVelocity>(e);

                            if (tag.ConversionSettings == ConversionSettings.ConvertToStatic)
                            {
                                checkerComponent.NumExpectedEvents = 0;
                            }
                        }
                        else
                        {
                            commandBuffer.AddComponent(e, new PhysicsVelocity
                            {
                                Angular = float3.zero,
                                Linear = float3.zero
                            });

                            if (pc.Value.Value.Type == ColliderType.Compound)
                            {
                                unsafe
                                {
                                    checkerComponent.NumExpectedEvents = ((CompoundCollider*)pc.ColliderPtr)->NumChildren;
                                }
                            }
                            else
                            {
                                checkerComponent.NumExpectedEvents = 1;
                            }
                        }
                        commandBuffer.RemoveComponent<ConvertToDifferentMotion>(e);
                    }
                }).Run();
            commandBuffer.Playback(EntityManager);
        }
    }
}
