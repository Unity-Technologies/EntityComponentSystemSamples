using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

[GenerateAuthoringComponent]
public struct AcitvateBody : IComponentData
{
    public int FramesToActivateIn;
    public float3 ActivationDisplacement;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
public partial class ActivateBodySystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] {typeof(AcitvateBody)}
        }));
    }

    protected override void OnUpdate()
    {
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            Entities
                .WithName("ActivateBodies")
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((ref Entity e, ref Translation pos, ref AcitvateBody activationComponent) =>
                {
                    if (--activationComponent.FramesToActivateIn == 0)
                    {
                        commandBuffer.RemoveComponent<AcitvateBody>(e);
                        pos.Value += activationComponent.ActivationDisplacement;

                        // Bodies get out of trigger
                        if (activationComponent.ActivationDisplacement.y >= 5.0f)
                        {
                            commandBuffer.RemoveComponent<TriggerEventChecker>(e);
                        }
                        else if (activationComponent.ActivationDisplacement.x > 0)
                        {
                            // New bodies enter trigger
                            commandBuffer.AddComponent(e, new TriggerEventChecker
                            {
                                NumExpectedEvents = 1
                            });
                        }
                    }
                }).Run();

            commandBuffer.Playback(EntityManager);
        }
    }
}
