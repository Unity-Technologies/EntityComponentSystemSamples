using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using static Unity.Physics.Math;

public class RagdollGridDemo : BasePhysicsDemo
{
    protected override void Start()
    {
        base.Start();

        BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(new BoxGeometry
        {
            Center = float3.zero,
            Orientation = quaternion.identity,
            Size = new float3(0.2f, 1.0f, 0.2f),
            BevelRadius = 0.0f
        });

        // Make some ragdoll joints
        for (int i = 0; i < 10; i++)
        {
            // Create a body
            Entity body = CreateDynamicBody(
                new float3((i - 4.5f) * 1.0f, 0, 0), quaternion.identity, collider, float3.zero, float3.zero, 1.0f);

            // Create the ragdoll joint
            float3 pivotLocal = new float3(0, 0.5f, 0);
            float3 pivotInWorld = math.transform(GetBodyTransform(body), pivotLocal);
            float3 axisLocal = new float3(0, 1, 0);
            float3 perpendicularLocal = new float3(0, 0, 1);

            quaternion worldFromLocal = Quaternion.AngleAxis((i - 4.5f) * 20.0f, new float3(0, 0, 1));
            float3 axisWorld = math.mul(worldFromLocal, axisLocal);
            float3 perpendicularWorld = math.mul(worldFromLocal, perpendicularLocal);

            float maxConeAngle = (float)math.PI / 4.0f;
            var perpendicularAngle = new FloatRange(-math.PI / 2f, math.PI / 2f);
            var twistAngle = new FloatRange(-math.PI / 8f, math.PI / 8f);

            var localFrame = new BodyFrame { Axis = axisLocal, PerpendicularAxis = perpendicularLocal, Position = pivotLocal };
            var worldFrame = new BodyFrame { Axis = axisWorld, PerpendicularAxis = perpendicularWorld, Position = pivotInWorld };
            PhysicsJoint.CreateRagdoll(localFrame, worldFrame, maxConeAngle, perpendicularAngle, twistAngle, out var ragdoll0, out var ragdoll1);
            CreateJoint(ragdoll0, body, Entity.Null);
            CreateJoint(ragdoll1, body, Entity.Null);
        }
    }
}
