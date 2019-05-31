using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BoxCollider = Unity.Physics.BoxCollider;
using Collider = Unity.Physics.Collider;
using Material = Unity.Physics.Material;

public class LimitedHingeDemo : BasePhysicsDemo
{
    protected override unsafe void Start()
    {
        init(float3.zero); // no gravity

        // Enable the joint viewer
        SetDebugDisplay(new Unity.Physics.Authoring.PhysicsDebugDisplayData
        {
            DrawJoints = 1
        });

        Entity* entities = stackalloc Entity[2];
        entities[1] = Entity.Null;
        for (int i = 0; i < 1; i++)
        {
            CollisionFilter filter = new CollisionFilter
            {
                CollidesWith = (uint)(1 << i),
                BelongsTo = (uint)~(1 << (1 - i))
            };
            BlobAssetReference<Collider> collider = BoxCollider.Create(
                float3.zero, Quaternion.identity, new float3(1.0f, 0.2f, 0.2f), 0.0f, filter);
            entities[i] = CreateDynamicBody(float3.zero, quaternion.identity, collider, float3.zero, new float3(0, 1 - i, 0), 1.0f);
        }

        float3 pivot = float3.zero;
        float3 axis = new float3(0, 1, 0);
        float3 perpendicular = new float3(0, 0, 1);

        //BlobAssetReference<JointData> hingeData = JointData.CreateLimitedHinge(pivot, pivot, axis, axis, perpendicular, perpendicular, 0.2f, (float)math.PI);
        BlobAssetReference<JointData> hingeData = JointData.CreateLimitedHinge(pivot, pivot, axis, axis, perpendicular, perpendicular, -(float)math.PI, -0.2f);
        CreateJoint(hingeData, entities[0], entities[1]);
    }
}
