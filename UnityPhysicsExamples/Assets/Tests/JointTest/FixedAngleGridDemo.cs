using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Extensions;
using static Unity.Physics.Math;

public class FixedAngleGridDemo : BasePhysicsDemo
{
    protected override void Start()
    {
        base.Start();

        // Enable the joint viewer
        SetDebugDisplay(new Unity.Physics.Authoring.PhysicsDebugDisplayData
        {
            DrawJoints = 1
        });

        BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(float3.zero, Quaternion.identity, new float3(0.25f), 0.0f);

        quaternion orientationA = quaternion.identity;
        bool identityA = true;
        if (!identityA)
        {
            orientationA = quaternion.AxisAngle(new float3(0, 1, 0), (float)math.PI * 3.0f / 2.0f);
        }

        quaternion orientationB = quaternion.identity;
        bool identityB = true;
        if (!identityB)
        {
            orientationB = quaternion.AxisAngle(math.normalize(new float3(1)), (float)math.PI / 4.0f);
        }

        // Make some joints with fixed position, limited 3D angle
        for (int i = 0; i < 10; i++)
        {
            // Create a body
            Entity body = CreateDynamicBody(
                new float3((i - 4.5f) * 1.0f, 0, 0), quaternion.identity, collider, float3.zero, float3.zero, 1.0f);

            // Create the ragdoll joint
            float3 pivotLocal = float3.zero;
            float3 pivotInWorld = math.transform(GetBodyTransform(body), pivotLocal);

            quaternion worldFromLocal = Quaternion.AngleAxis((i - 4.5f) * 20.0f, new float3(0, 0, 1));

            BlobAssetReference<JointData> jointData = JointData.Create(
                new MTransform(orientationA, pivotLocal),
                new MTransform(orientationB, pivotInWorld),
                new Constraint[]
                {
                    Constraint.BallAndSocket(),
                    new Constraint
                    {
                        ConstrainedAxes = new bool3(true, true, true),
                        Type = ConstraintType.Angular,
                        Min = math.max(i - 5, 0) * 0.1f,
                        Max = i * 0.1f,
                        SpringDamping = Constraint.DefaultSpringDamping,
                        SpringFrequency = Constraint.DefaultSpringFrequency
                    }
                });
            CreateJoint(jointData, body, Entity.Null);
        }
    }
}
