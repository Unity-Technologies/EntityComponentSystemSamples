using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using static Unity.Physics.Math;
using BoxCollider = Unity.Physics.BoxCollider;
using Collider = Unity.Physics.Collider;

public class TwistGridDemo : BasePhysicsDemo
{
    protected override unsafe void Start()
    {
        init();

        BlobAssetReference<Collider> collider = BoxCollider.Create(new BoxGeometry
        {
            Center = float3.zero,
            Orientation = quaternion.identity,
            Size = new float3(0.2f, 0.2f, 0.2f),
            BevelRadius = 0.0f
        });

        // Make some 1d angular limit joints
        const int size = 6;
        const float speed = 1.0f;
        int iDbg = 0;
        int jDbg = 3;
        for (int i = 0; i < size; i++)
        {
            quaternion q1 = quaternion.AxisAngle(new float3(1, 0, 0), (float)math.PI * 2.0f * i / size);
            for (int j = 0; j < size; j++)
            {
                if (iDbg >= 0 && jDbg >= 0 && (i != iDbg || j != jDbg))
                {
                    continue;
                }

                // Choose the limited axis
                quaternion q2 = quaternion.AxisAngle(math.mul(q1, new float3(0, 1, 0)),
                    (float)math.PI * ((float)j / size));

                // Create a body with some angular velocity about the axis
                float3 pos = new float3(i - (size - 1) / 2.0f, 0, j - (size - 1) / 2.0f);
                Entity body = CreateDynamicBody(pos, quaternion.identity, collider, float3.zero,
                    math.mul(q2, new float3(speed, 0, 0)), 1.0f);

                // Create a 1D angular limit about the axis
                float3x3 rotationB = float3x3.identity;
                float3x3 rotationA = math.mul(new float3x3(q2), rotationB);
                var jointData = new PhysicsJoint
                {
                    BodyAFromJoint = new RigidTransform(rotationA, new float3(0, 0, 0)),
                    BodyBFromJoint = new RigidTransform(rotationB, pos)
                };
                jointData.SetConstraints(new FixedList128<Constraint>
                {
                    Length = 1,
                    [0] = Constraint.Twist(0, new FloatRange(-math.PI / 4f, math.PI / 4f))
                });

                CreateJoint(jointData, body, Entity.Null);
            }
        }
    }
}
