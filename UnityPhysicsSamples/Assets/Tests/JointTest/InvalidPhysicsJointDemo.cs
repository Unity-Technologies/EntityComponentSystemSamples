using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

public class InvalidPhysicsJointDemo : BasePhysicsDemo
{
    protected override void Start()
    {
        base.Start();

        BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(new BoxGeometry
        {
            Center = float3.zero,
            Orientation = quaternion.identity,
            Size = new float3(0.25f),
            BevelRadius = 0.0f
        });

        var manager = DefaultWorld.EntityManager;

        // Add a dynamic body constrained to the world that will die
        // Once the dynamic body is destroyed the joint will be invalid
        {
            // Create a dynamic body
            float3 pivotWorld = new float3(-2f, 0, 0);
            Entity body = CreateDynamicBody(pivotWorld, quaternion.identity, collider, float3.zero, float3.zero, 1.0f);

            // create extra dynamic body to trigger havok sync after the first one is destroyed
            CreateDynamicBody(pivotWorld * 2.0f, quaternion.identity, collider, float3.zero, float3.zero, 1.0f);

            // add timeout on dynamic body after 15 frames.
            manager.AddComponentData(body, new LifeTime { Value = 15 });

            // Create the joint
            float3 pivotLocal = float3.zero;
            var joint = PhysicsJoint.CreateBallAndSocket(pivotLocal, pivotWorld);
            var jointEntity = CreateJoint(joint, body, Entity.Null);

            // add timeout on joint entity after 30 frames.
            manager.AddComponentData(jointEntity, new LifeTime { Value = 30 });
        }

        // Add two static bodies constrained together
        // The joint is invalid immediately
        {
            // Create a body
            Entity bodyA = CreateStaticBody(new float3(0, 0.0f, 0), quaternion.identity, collider);
            Entity bodyB = CreateStaticBody(new float3(0, 1.0f, 0), quaternion.identity, collider);

            // Create the joint
            float3 pivotLocal = float3.zero;
            var joint = PhysicsJoint.CreateBallAndSocket(pivotLocal, pivotLocal);
            var jointEntity = CreateJoint(joint, bodyA, bodyB);

            // add timeout on joint entity after 15 frames.
            manager.AddComponentData(jointEntity, new LifeTime { Value = 15 });
        }

        // Add two dynamic bodies constrained together with 0 dimension
        {
            // Create a body
            Entity bodyA = CreateDynamicBody(new float3(0, 5.0f, 0), quaternion.identity, collider, float3.zero, float3.zero, 1.0f);
            Entity bodyB = CreateDynamicBody(new float3(0, 6.0f, 0), quaternion.identity, collider, float3.zero, float3.zero, 1.0f);

            // Create the joint
            var joint = PhysicsJoint.CreateLimitedDOF(RigidTransform.identity, new bool3(false), new bool3(false));
            CreateJoint(joint, bodyA, bodyB);
        }
    }
}
