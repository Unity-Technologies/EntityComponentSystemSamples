using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

public class InvalidPhysicsJointSwapDemoScene : SceneCreationSettings {}

public class InvalidPhysicsJointSwapDemo : SceneCreationAuthoring<InvalidPhysicsJointSwapDemoScene> {}

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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
public class InvalidPhysicsJointSwapDemoSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;

        Entities
            .WithName("InvalidPhysicsJointTimerEvent")
            .ForEach((ref InvalidPhysicsJointSwapTimerEvent timer) =>
            {
                timer.Tick(deltaTime);
            }).Run();

        // swap motion type
        {
            Entities
                .WithName("InvalidPhysicsJointSwapBodies")
                .WithAll<InvalidPhysicsJointSwapBodies>()
                .ForEach((ref InvalidPhysicsJointSwapTimerEvent timer, ref PhysicsConstrainedBodyPair bodyPair) =>
                {
                    if (timer.Fired(true))
                    {
                        bodyPair = new PhysicsConstrainedBodyPair(bodyPair.EntityB, bodyPair.EntityA, bodyPair.EnableCollision != 0);
                    }
                }).Run();


            using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
            {
                Entities
                    .WithName("InvalidPhysicsJointSwapMotionType_S2D")
                    .WithoutBurst()
                    .WithAll<InvalidPhysicsJointSwapMotionType>()
                    .WithNone<PhysicsVelocity>()
                    .ForEach((Entity entity, ref InvalidPhysicsJointSwapTimerEvent timer) =>
                    {
                        if (timer.Fired(true))
                        {
                            commandBuffer.AddComponent(entity, new PhysicsVelocity {});
                            commandBuffer.AddComponent(entity, PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 1));
                        }
                    }).Run();

                Entities
                    .WithName("InvalidPhysicsJointSwapMotionType_D2S")
                    .WithoutBurst()
                    .ForEach((Entity entity,
                        ref InvalidPhysicsJointSwapMotionType swapMotionType,
                        ref InvalidPhysicsJointSwapTimerEvent timer,
                        ref Translation position, ref Rotation rotation,
                        in PhysicsVelocity physicsVelocity) =>
                        {
                            if (timer.Fired(true))
                            {
                                position.Value = swapMotionType.OriginalTransform.pos;
                                rotation.Value = swapMotionType.OriginalTransform.rot;
                                commandBuffer.RemoveComponent<PhysicsVelocity>(entity);
                                commandBuffer.RemoveComponent<PhysicsMass>(entity);
                            }
                        }).Run();

                Entities
                    .WithName("InvalidPhysicsJointKillBodies")
                    .WithoutBurst()
                    .WithAll<InvalidPhysicsJointKillBodies>()
                    .ForEach((Entity entity,
                        ref InvalidPhysicsJointSwapTimerEvent timer,
                        ref Translation position, ref Rotation rotation,
                        in PhysicsVelocity physicsVelocity) =>
                        {
                            if (timer.Fired(true))
                            {
                                commandBuffer.DestroyEntity(entity);
                            }
                        }).Run();

                commandBuffer.Playback(EntityManager);
            }
        }
    }
}

public class InvalidPhyiscsJointSwapDemoSceneCreationSystem : SceneCreationSystem<InvalidPhysicsJointSwapDemoScene>
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

        float timeToSwap = 0.25f;

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
