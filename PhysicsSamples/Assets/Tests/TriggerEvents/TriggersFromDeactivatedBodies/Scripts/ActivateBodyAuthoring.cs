using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

public struct ActivateBody : IComponentData
{
    public int FramesToActivateIn;
    public float3 ActivationDisplacement;
}

[UnityEngine.DisallowMultipleComponent]
public class ActivateBodyAuthoring : UnityEngine.MonoBehaviour
{
    [RegisterBinding(typeof(ActivateBody), "FramesToActivateIn")]
    public int FramesToActivateIn;
    [RegisterBinding(typeof(ActivateBody), "ActivationDisplacement.x", true)]
    [RegisterBinding(typeof(ActivateBody), "ActivationDisplacement.y", true)]
    [RegisterBinding(typeof(ActivateBody), "ActivationDisplacement.z", true)]
    public float3 ActivationDisplacement;

    class AcitvateBodyBaker : Baker<ActivateBodyAuthoring>
    {
        public override void Bake(ActivateBodyAuthoring authoring)
        {
            ActivateBody component = default(ActivateBody);
            component.FramesToActivateIn = authoring.FramesToActivateIn;
            component.ActivationDisplacement = authoring.ActivationDisplacement;
            AddComponent(component);
        }
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class ActivateBodySystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<ActivateBody>();
    }

    protected override void OnUpdate()
    {
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            Entities
                .WithName("ActivateBodies")
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((ref Entity e, ref Translation pos, ref ActivateBody activationComponent) =>
                {
                    if (--activationComponent.FramesToActivateIn == 0)
                    {
                        commandBuffer.RemoveComponent<ActivateBody>(e);
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
