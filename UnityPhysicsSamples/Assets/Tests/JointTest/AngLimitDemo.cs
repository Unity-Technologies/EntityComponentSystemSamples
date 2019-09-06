using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Extensions;
using static Unity.Physics.Math;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class AngLimitDemo : BasePhysicsDemo
{
    protected unsafe override void Start()
    {
        float3 gravity = float3.zero;
        base.init(gravity);

        // Enable the joint viewer
        SetDebugDisplay(new Unity.Physics.Authoring.PhysicsDebugDisplayData
        {
            DrawJoints = 1,
        });

        // static body
        Entity staticEntity;
        {
            BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(new BoxGeometry
            {
                Center = float3.zero,
                Orientation = quaternion.identity,
                Size = new float3(1.0f),
                BevelRadius = 0.01f
            });
            staticEntity = CreateStaticBody(float3.zero, quaternion.identity, collider);
        }

        // dynamic body
        Entity dynamicEntity;
        {
            BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.CapsuleCollider.Create(new CapsuleGeometry
            {
                Vertex0 = new float3(-0.5f, 0, 0),
                Vertex1 = new float3(0.5f, 0, 0),
                Radius = 0.05f
            });
            float3 angularVelocity = new float3(0, 1.0f, 0);
            dynamicEntity = CreateDynamicBody(new float3(2, 0, 0), quaternion.identity, collider, float3.zero, angularVelocity, 1.0f);
        }

        // 1D angular limit
        //{
        //    float3 pivotA = new float3(-0.5f, 0, 0);
        //    float3 pivotB = GetBodyTransform(staticEntity).Inverse().TransformPoint(GetBodyTransform(dynamicEntity).TransformPoint(pivotA));

        //    BlobAssetReference<JointData> jointData;
        //    using (var allocator = new BlobAllocator(-1))
        //    {
        //        Constraint angLimit = Constraint.Twist(0, -(float)math.PI / 4.0f, (float)math.PI / 4.0f);
        //        ref JointData data = ref allocator.ConstructRoot<JointData>();
        //        data.Init(
        //            new MTransform(float3x3.identity, pivotA),
        //            new MTransform(float3x3.identity, pivotB),
        //            &angLimit, 1);
        //        jointData = allocator.CreateBlobAssetReference<JointData>(Allocator.Persistent);
        //    }

        //    CreateJoint(jointData, dynamicEntity, staticEntity);
        //}
    }
}
