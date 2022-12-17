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

    public partial struct ActivateBodyJob : IJobEntity
    {
        public EntityCommandBuffer CommandBuffer;

#if !ENABLE_TRANSFORM_V1
        public void Execute(Entity entity, ref LocalTransform localTransform, ref ActivateBody activateBody)
#else
        public void Execute(Entity entity, ref Translation localPosition, ref ActivateBody activateBody)
#endif
        {
            if (--activateBody.FramesToActivateIn == 0)
            {
                CommandBuffer.RemoveComponent<ActivateBody>(entity);

#if !ENABLE_TRANSFORM_V1
                localTransform.Position += activateBody.ActivationDisplacement;
#else
                localPosition.Value += activateBody.ActivationDisplacement;
#endif
                // Bodies get out of trigger
                if (activateBody.ActivationDisplacement.y >= 5.0f)
                {
                    CommandBuffer.RemoveComponent<TriggerEventChecker>(entity);
                }
                else if (activateBody.ActivationDisplacement.x > 0)
                {
                    // New bodies enter trigger
                    CommandBuffer.AddComponent(entity, new TriggerEventChecker
                    {
                        NumExpectedEvents = 1
                    });
                }
            }
        }
    }

    protected override void OnUpdate()
    {
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            new ActivateBodyJob
            {
                CommandBuffer = commandBuffer
            }.Run();

            commandBuffer.Playback(EntityManager);
        }
    }
}
