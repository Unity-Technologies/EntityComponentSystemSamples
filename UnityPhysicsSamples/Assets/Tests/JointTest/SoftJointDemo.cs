using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Extensions;

public class SoftJointDemo : BasePhysicsDemo
{
    protected override void Start()
    {
        float3 gravity = float3.zero;
        base.init(gravity);

        // Enable the joint viewer
        SetDebugDisplay(new Unity.Physics.Authoring.PhysicsDebugDisplayData
        {
            DrawJoints = 1
        });

        // Make soft ball and sockets
        {
            BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(float3.zero, Quaternion.identity, new float3(0.2f, 0.2f, 0.2f), 0.0f);

            // Make joints with different spring frequency.  The leftmost joint should oscillate at 0.5hz, the next at 1hz, the next at 1.5hz, etc.
            for (int i = 0; i < 10; i++)
            {
                // Create a body
                float3 position = new float3((i - 4.5f) * 1.0f, 0, 0);
                float3 velocity = new float3(0, -10.0f, 0);
                Entity body = CreateDynamicBody(
                    position, quaternion.identity, collider, velocity, float3.zero, 1.0f);

                // Create the ball and socket joint
                float3 pivotLocal = float3.zero;
                float3 pivotInWorld = math.transform(GetBodyTransform(body), pivotLocal);

                BlobAssetReference<JointData> jointData;
                jointData = JointData.CreateBallAndSocket(pivotLocal, pivotInWorld);
                jointData.Value.Constraints[0].SpringDamping = 0.0f;
                jointData.Value.Constraints[0].SpringFrequency = 0.5f * (float)(i + 1);
                CreateJoint(jointData, body, Entity.Null);
            }
        }

        // Make soft limited hinges
        {
            BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(float3.zero, Quaternion.identity, new float3(0.4f, 0.1f, 0.6f), 0.0f);

            // First row has soft limit with hard hinge + pivot, second row has everything soft
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    // Create a body
                    float3 position = new float3((i - 4.5f) * 1.0f, 0, (j + 1) * 3.0f);
                    float3 velocity = new float3(0, -10.0f, 0);
                    float3 angularVelocity = new float3(0, 0, -10.0f);
                    Entity body = CreateDynamicBody(
                        position, quaternion.identity, collider, velocity, angularVelocity, 1.0f);

                    // Create the limited hinge joint
                    float3 pivotLocal = new float3(0, 0, 0);
                    float3 pivotInWorld = math.transform(GetBodyTransform(body), pivotLocal);
                    float3 axisLocal = new float3(0, 0, 1);
                    float3 axisInWorld = axisLocal;
                    float3 perpendicularLocal = new float3(0, 1, 0);
                    float3 perpendicularInWorld = perpendicularLocal;

                    BlobAssetReference<JointData> jointData;
                    jointData = JointData.CreateLimitedHinge(pivotLocal, pivotInWorld, axisLocal, axisInWorld, perpendicularLocal, perpendicularInWorld, 0.0f, 0.0f);

                    // First constraint is the limit, next two are the hinge and pivot
                    for (int k = 0; k < 1 + 2 * j; k++)
                    {
                        jointData.Value.Constraints[k].SpringDamping = 0.0f;
                        jointData.Value.Constraints[k].SpringFrequency = 0.5f * (float)(i + 1);
                    }

                    CreateJoint(jointData, body, Entity.Null);
                }
            }
        }

        // Make a soft prismatic
        {
            BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(float3.zero, Quaternion.identity, new float3(0.2f, 0.2f, 0.2f), 0.0f);

            // Create a body
            float3 position = new float3(0, 0, 9.0f);
            float3 velocity = new float3(50.0f, 0, 0);
            Entity body = CreateDynamicBody(
                position, quaternion.identity, collider, velocity, float3.zero, 1.0f);

            // Create the prismatic joint
            float3 pivotLocal = float3.zero;
            float3 pivotInWorld = math.transform(GetBodyTransform(body), pivotLocal);
            float3 axisLocal = new float3(1, 0, 0);
            float3 axisInWorld = axisLocal;
            float3 perpendicularLocal = new float3(0, 1, 0);
            float3 perpendicularInWorld = perpendicularLocal;

            BlobAssetReference<JointData> jointData;
            jointData = JointData.CreatePrismatic(pivotLocal, pivotInWorld, axisLocal, axisInWorld, perpendicularLocal, perpendicularInWorld, - 2.0f, 2.0f, 0.0f, 0.0f);
            jointData.Value.Constraints[0].SpringDamping = 0.0f;
            jointData.Value.Constraints[0].SpringFrequency = 5.0f;
            CreateJoint(jointData, body, Entity.Null);
        }
    }
}
