using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

public class InvalidPhysicsJointSwapDemoScene : SceneCreationSettings
{
    public float TimeToSwap = 0.25f;
}

public class InvalidPhysicsJointSwapDemo : SceneCreationAuthoring<InvalidPhysicsJointSwapDemoScene>
{
    public float TimeToSwap = 0.25f;

    class InvalidPhysicsJointSwapDemoBaker : Baker<InvalidPhysicsJointSwapDemo>
    {
        public override void Bake(InvalidPhysicsJointSwapDemo authoring)
        {
            AddComponentObject(new InvalidPhysicsJointSwapDemoScene
            {
                DynamicMaterial = authoring.DynamicMaterial,
                StaticMaterial = authoring.StaticMaterial,
                TimeToSwap = authoring.TimeToSwap
            });
        }
    }
}

public struct InvalidPhysicsJointSwapTimerEvent : IComponentData
{
    public float TimeLimit;
    internal float Timer;

    public void Reset() => Timer = TimeLimit;
    public void Tick(float deltaTime) => Timer -= deltaTime;
    public bool Fired(bool resetIfFired)
    {
        bool isFired = Timer < 0;
        if (isFired && resetIfFired) Reset();
        return isFired;
    }
}

public struct InvalidPhysicsJointKillBodies : IComponentData {}

public struct InvalidPhysicsJointSwapBodies : IComponentData {}

public struct InvalidPhysicsJointSwapMotionType : IComponentData
{
    public RigidTransform OriginalTransform;
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class InvalidPhysicsJointSwapDemoSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var timer in SystemAPI.Query<RefRW<InvalidPhysicsJointSwapTimerEvent>>())
        {
            timer.ValueRW.Tick(deltaTime);
        }

        // swap motion type
        foreach (var(timer, bodyPair) in SystemAPI.Query<RefRW<InvalidPhysicsJointSwapTimerEvent>, RefRW<PhysicsConstrainedBodyPair>>().WithAll<InvalidPhysicsJointSwapBodies>())
        {
            if (timer.ValueRW.Fired(true))
            {
                bodyPair.ValueRW = new PhysicsConstrainedBodyPair(bodyPair.ValueRW.EntityB, bodyPair.ValueRW.EntityA, bodyPair.ValueRW.EnableCollision != 0);
            }
        }

        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            foreach (var(timer, entity) in SystemAPI.Query<RefRW<InvalidPhysicsJointSwapTimerEvent>>().WithEntityAccess().WithAll<InvalidPhysicsJointSwapMotionType>().WithNone<PhysicsVelocity>())
            {
                if (timer.ValueRW.Fired(true))
                {
                    commandBuffer.AddComponent(entity, new PhysicsVelocity {});
                    commandBuffer.AddComponent(entity, PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 1));
                }
            }

#if !ENABLE_TRANSFORM_V1
            foreach (var(swapMotionType, timer, localTransform, valocity, entity) in SystemAPI
                     .Query<RefRW<InvalidPhysicsJointSwapMotionType>, RefRW<InvalidPhysicsJointSwapTimerEvent>,
                            RefRW<LocalTransform>, RefRO<PhysicsVelocity>>().WithEntityAccess())
#else
            foreach (var(swapMotionType, timer, position, rotation, velocity, entity) in SystemAPI.Query<RefRW<InvalidPhysicsJointSwapMotionType>, RefRW<InvalidPhysicsJointSwapTimerEvent>, RefRW<Translation>, RefRW<Rotation>, RefRO<PhysicsVelocity>>().WithEntityAccess())
#endif
            {
                if (timer.ValueRW.Fired(true))
                {
#if !ENABLE_TRANSFORM_V1
                    localTransform.ValueRW.Position = swapMotionType.ValueRW.OriginalTransform.pos;
                    localTransform.ValueRW.Rotation = swapMotionType.ValueRW.OriginalTransform.rot;
#else
                    position.ValueRW.Value = swapMotionType.ValueRW.OriginalTransform.pos;
                    rotation.ValueRW.Value = swapMotionType.ValueRW.OriginalTransform.rot;
#endif
                    commandBuffer.RemoveComponent<PhysicsVelocity>(entity);
                    commandBuffer.RemoveComponent<PhysicsMass>(entity);
                }
            }

#if !ENABLE_TRANSFORM_V1
            foreach (var(timer, localTransform, velocity, entity) in SystemAPI
                     .Query<RefRW<InvalidPhysicsJointSwapTimerEvent>, RefRW<LocalTransform>,
                            RefRW<PhysicsVelocity>>().WithEntityAccess().WithAll<InvalidPhysicsJointKillBodies>())
#else
            foreach (var(timer, position, rotation, velocity, entity)
                     in SystemAPI.Query<RefRW<InvalidPhysicsJointSwapTimerEvent>, RefRW<Translation>, RefRW<Rotation>, RefRW<PhysicsVelocity>>().WithEntityAccess().WithAll<InvalidPhysicsJointKillBodies>())
#endif
            {
                if (timer.ValueRW.Fired(true))
                {
                    commandBuffer.DestroyEntity(entity);
                }
            }

            commandBuffer.Playback(EntityManager);
        }
    }
}

public class InvalidPhysicsJointSwapDemoSceneCreationSystem : SceneCreationSystem<InvalidPhysicsJointSwapDemoScene>
{
    public override void CreateScene(InvalidPhysicsJointSwapDemoScene sceneSettings)
    {
        float colliderSize = 0.25f;

        BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(new BoxGeometry
        {
            Center = float3.zero,
            Orientation = quaternion.identity,
            Size = new float3(colliderSize),
            BevelRadius = 0.0f
        });
        CreatedColliders.Add(collider);

        float timeToSwap = sceneSettings.TimeToSwap;

        // Add two constrained dynamic bodies that will have their bodies swapped
        bool buildThisSection = true;
        if (buildThisSection)
        {
            // Create a body
            Entity bodyA = CreateDynamicBody(new float3(2f, 5.0f, 0), quaternion.identity, collider, float3.zero, float3.zero, 1.0f);
            Entity bodyB = CreateDynamicBody(new float3(2f, 6.0f, 0), quaternion.identity, collider, float3.zero, float3.zero, 1.0f);

            for (int i = 0; i < 2; i++)
            {
                // Create the joint
                var joint = PhysicsJoint.CreateBallAndSocket(new float3(0, colliderSize, 0), new float3(0, -colliderSize, 0));
                var jointEntity = CreateJoint(joint, bodyA, bodyB);

                var pair = EntityManager.GetComponentData<PhysicsConstrainedBodyPair>(jointEntity);
                pair.EnableCollision = 1;
                EntityManager.SetComponentData(jointEntity, pair);

                if (1 == i)
                {
                    // add swap and timer components.
                    EntityManager.AddComponentData(jointEntity, new InvalidPhysicsJointSwapBodies {});
                    EntityManager.AddComponentData(jointEntity, new InvalidPhysicsJointSwapTimerEvent { TimeLimit = timeToSwap, Timer = timeToSwap });
                }
            }

            EntityManager.AddComponentData(bodyA, new InvalidPhysicsJointKillBodies {});
            EntityManager.AddComponentData(bodyA, new InvalidPhysicsJointSwapTimerEvent { TimeLimit = timeToSwap * 2.0f, Timer = timeToSwap * 2.0f });
        }

        // Add constrained static/dynamic body pair that will have their bodies swapped
        buildThisSection = true;
        if (buildThisSection)
        {
            // Create a body
            var staticTransform = new RigidTransform(quaternion.identity, new float3(3f, 5.0f, 0));
            Entity bodyS = CreateStaticBody(staticTransform.pos, staticTransform.rot, collider);
            Entity bodyD = CreateDynamicBody(new float3(3f, 6.0f, 0), quaternion.identity, collider, float3.zero, float3.zero, 1.0f);

            for (int i = 0; i < 2; i++)
            {
                // Create the joint
                var joint = PhysicsJoint.CreateBallAndSocket(new float3(0, colliderSize, 0), new float3(0, -colliderSize, 0));
                var jointEntity = CreateJoint(joint, bodyS, bodyD);

                var pair = EntityManager.GetComponentData<PhysicsConstrainedBodyPair>(jointEntity);
                pair.EnableCollision = 1;
                EntityManager.SetComponentData(jointEntity, pair);
            }

            // add swap and timer components.
            EntityManager.AddComponentData(bodyS, new InvalidPhysicsJointSwapMotionType { OriginalTransform = staticTransform });
            EntityManager.AddComponentData(bodyS, new InvalidPhysicsJointSwapTimerEvent { TimeLimit = timeToSwap, Timer = timeToSwap });
        }
    }
}
