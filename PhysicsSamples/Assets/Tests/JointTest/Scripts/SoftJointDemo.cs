using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using static Unity.Physics.Math;

public class SoftJointDemoScene : SceneCreationSettings {}

public class SoftJointDemo : SceneCreationAuthoring<SoftJointDemoScene> {}

public class SoftJointDemoCreationSystem : SceneCreationSystem<SoftJointDemoScene>
{
    public override void CreateScene(SoftJointDemoScene sceneSettings)
    {
        // Make soft ball and sockets
        {
            BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(new BoxGeometry
            {
                Center = float3.zero,
                Orientation = quaternion.identity,
                Size = new float3(0.2f, 0.2f, 0.2f),
                BevelRadius = 0.0f
            });
            CreatedColliders.Add(collider);

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

                var jointData = PhysicsJoint.CreateBallAndSocket(pivotLocal, pivotInWorld);
                var constraints = jointData.GetConstraints();
                var constraint = constraints[0];
                // Choose a small damping value instead of 0 to improve stability of the joints
                constraint.SpringDamping = 0.05f;
                constraint.SpringFrequency = 0.5f * (float)(i + 1);
                constraints[0] = constraint;
                jointData.SetConstraints(constraints);

                CreateJoint(jointData, body, Entity.Null);
            }
        }

        //Make soft limited hinges
        {
            BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(new BoxGeometry
            {
                Center = float3.zero,
                Orientation = quaternion.identity,
                Size = new float3(0.4f, 0.1f, 0.6f),
                BevelRadius = 0.0f
            });
            CreatedColliders.Add(collider);

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

                    var frameLocal = new BodyFrame { Axis = axisLocal, PerpendicularAxis = perpendicularLocal, Position = pivotLocal };
                    var frameWorld = new BodyFrame { Axis = axisInWorld, PerpendicularAxis = perpendicularInWorld, Position = pivotInWorld };
                    var jointData = PhysicsJoint.CreateLimitedHinge(frameLocal, frameWorld, default);

                    // First constraint is the limit, next two are the hinge and pivot
                    var constraints = jointData.GetConstraints();
                    for (int k = 0; k < 1 + 2 * j; k++)
                    {
                        var constraint = constraints[k];
                        // Choose a small damping value instead of 0 to improve stability of the joints
                        constraint.SpringDamping = 0.05f;
                        constraint.SpringFrequency = 0.5f * (i + 1);
                        constraints[k] = constraint;
                    }
                    jointData.SetConstraints(constraints);

                    CreateJoint(jointData, body, Entity.Null);
                }
            }
        }

        // Make a soft prismatic
        {
            BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(new BoxGeometry
            {
                Center = float3.zero,
                Orientation = quaternion.identity,
                Size = new float3(0.2f, 0.2f, 0.2f),
                BevelRadius = 0.0f
            });
            CreatedColliders.Add(collider);

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

            var localFrame = new BodyFrame { Axis = axisLocal, PerpendicularAxis = perpendicularLocal, Position = pivotLocal };
            var worldFrame = new BodyFrame { Axis = axisInWorld, PerpendicularAxis = perpendicularInWorld, Position = pivotInWorld };
            var jointData = PhysicsJoint.CreatePrismatic(localFrame, worldFrame, new FloatRange(-2f, 2f));
            var constraints = jointData.GetConstraints();
            var constraint = constraints[0];
            // Choose a small damping value instead of 0 to improve stability of the joints
            constraint.SpringDamping = 0.05f;
            constraint.SpringFrequency = 5.0f;
            constraints[0] = constraint;
            jointData.SetConstraints(constraints);
            CreateJoint(jointData, body, Entity.Null);
        }
    }
}
