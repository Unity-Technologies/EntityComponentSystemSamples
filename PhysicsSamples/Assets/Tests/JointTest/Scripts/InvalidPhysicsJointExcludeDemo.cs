using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

public class InvalidPhysicsJointExcludeDemoScene : SceneCreationSettings
{
    public float TimeToSwap = 0.5f;
}

public class InvalidPhysicsJointExcludeDemo : SceneCreationAuthoring<InvalidPhysicsJointExcludeDemoScene>
{
    public float TimeToSwap = 0.5f;

    public override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new InvalidPhysicsJointExcludeDemoScene
        {
            DynamicMaterial = DynamicMaterial,
            StaticMaterial = StaticMaterial,
            TimeToSwap = TimeToSwap
        });
    }
}

public struct InvalidPhysicsJointExcludeTimerEvent : IComponentData
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


public struct InvalidPhysicsJointExcludeBodies : IComponentData {}


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
public partial class InvalidPhysicsJointExcludeDemoSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;

        Entities
            .WithName("InvalidPhysicsJointExcludeTimerEvent")
            .ForEach((ref InvalidPhysicsJointExcludeTimerEvent timer) =>
            {
                timer.Tick(deltaTime);
            }).Run();

        // add/remove PhysicsExclude
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            Entities
                .WithName("InvalidPhysicsJointExcludeBodies_Exclude")
                .WithoutBurst()
                .WithAll<InvalidPhysicsJointExcludeBodies, PhysicsWorldIndex>()
                .ForEach((Entity entity, ref InvalidPhysicsJointExcludeTimerEvent timer) =>
                {
                    if (timer.Fired(true))
                    {
                        // If we want to support multiple worlds, we need to store PhysicsWorldIndex.Value somewhere
                        commandBuffer.RemoveComponent<PhysicsWorldIndex>(entity);
                    }
                }).Run();

            Entities
                .WithName("InvalidPhysicsJointExcludeBodies_Include")
                .WithoutBurst()
                .WithAll<InvalidPhysicsJointExcludeBodies>()
                .WithNone<PhysicsWorldIndex>()
                .ForEach((Entity entity, ref InvalidPhysicsJointExcludeTimerEvent timer) =>
                {
                    if (timer.Fired(true))
                    {
                        commandBuffer.AddSharedComponent(entity, new PhysicsWorldIndex());
                    }
                }).Run();

            commandBuffer.Playback(EntityManager);
        }
    }
}


public class InvalidPhyiscsJointExcludeDemoSceneCreationSystem : SceneCreationSystem<InvalidPhysicsJointExcludeDemoScene>
{
    public override void CreateScene(InvalidPhysicsJointExcludeDemoScene sceneSettings)
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

        var bodyAPos = new float3(2f, 5.0f, 2);
        var bodyBPos = new float3(2f, 6.0f, 2);

        // Add constrained dynamic/dynamic body pair that will have their bodies excluded
        bool buildThisSection = true;
        if (buildThisSection)
        {
            bodyAPos += new float3(1, 0, 0);
            bodyBPos += new float3(1, 0, 0);

            // Create a body
            Entity bodyA = CreateDynamicBody(bodyAPos, quaternion.identity, collider, float3.zero, float3.zero, 1.0f);
            Entity bodyB = CreateDynamicBody(bodyBPos, quaternion.identity, collider, float3.zero, float3.zero, 1.0f);

            for (int i = 0; i < 2; i++)
            {
                // Create the joint
                var joint = PhysicsJoint.CreateBallAndSocket(new float3(0, colliderSize, 0), new float3(0, -colliderSize, 0));
                var jointEntity = CreateJoint(joint, bodyA, bodyB);

                var pair = EntityManager.GetComponentData<PhysicsConstrainedBodyPair>(jointEntity);
                pair.EnableCollision = 1;
                EntityManager.SetComponentData(jointEntity, pair);
            }

            // add exclude components.
            EntityManager.AddComponentData(bodyA, new InvalidPhysicsJointExcludeBodies {});
            EntityManager.AddComponentData(bodyA, new InvalidPhysicsJointExcludeTimerEvent { TimeLimit = timeToSwap, Timer = timeToSwap });
            EntityManager.AddComponentData(bodyB, new InvalidPhysicsJointExcludeBodies {});
            EntityManager.AddComponentData(bodyB, new InvalidPhysicsJointExcludeTimerEvent { TimeLimit = timeToSwap, Timer = timeToSwap });
        }
    }
}
