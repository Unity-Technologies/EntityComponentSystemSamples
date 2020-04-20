using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class InvalidPhysicsJointDemo : BasePhysicsDemo
{
    protected override void Start()
    {
        base.Start();

        // Enable the joint viewer
        SetDebugDisplay(new Unity.Physics.Authoring.PhysicsDebugDisplayData
        {
            DrawJoints = 1
        });

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

            // add timeout on dynamic body after 15 frames.
            manager.AddComponentData(body, new LifeTime { Value = 15 });

            // Create the joint
            float3 pivotLocal = float3.zero;
            BlobAssetReference<JointData> jointData = JointData.CreateBallAndSocket(pivotLocal, pivotWorld);
            var jointEntity = CreateJoint(jointData, body, Entity.Null);

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
            BlobAssetReference<JointData> jointData = JointData.CreateBallAndSocket(pivotLocal, pivotLocal);
            var jointEntity = CreateJoint(jointData, bodyA, bodyB);

            // add timeout on joint entity after 15 frames.
            manager.AddComponentData(jointEntity, new LifeTime { Value = 15 });
        }
    }
}
