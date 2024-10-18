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
    [RegisterBinding(typeof(ActivateBody), "ActivationDisplacement")]
    public float3 ActivationDisplacement;

    class AcitvateBodyBaker : Baker<ActivateBodyAuthoring>
    {
        public override void Bake(ActivateBodyAuthoring authoring)
        {
            ActivateBody component = default(ActivateBody);
            component.FramesToActivateIn = authoring.FramesToActivateIn;
            component.ActivationDisplacement = authoring.ActivationDisplacement;
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct ActivateBodySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ActivateBody>();
    }

    public partial struct ActivateBodyJob : IJobEntity
    {
        public EntityCommandBuffer CommandBuffer;


        public void Execute(Entity entity, ref LocalTransform localTransform, ref ActivateBody activateBody)
        {
            if (--activateBody.FramesToActivateIn == 0)
            {
                CommandBuffer.RemoveComponent<ActivateBody>(entity);


                localTransform.Position += activateBody.ActivationDisplacement;

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

    public void OnUpdate(ref SystemState state)
    {
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            var jobHandle = new ActivateBodyJob
            {
                CommandBuffer = commandBuffer
            }.Schedule(state.Dependency);

            state.Dependency = jobHandle;
            jobHandle.Complete();

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
