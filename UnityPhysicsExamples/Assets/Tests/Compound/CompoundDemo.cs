using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using static Unity.Physics.Math;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class CompoundDemo : BasePhysicsDemo
{
    protected unsafe override void Start()
    {
        //float3 gravity = new float3(0, -9.81f, 0);
        float3 gravity = float3.zero;
        base.init(gravity);

//         // Floor
//         {
//             BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(new float3(0, -0.1f, 0), Quaternion.identity, new float3(10.0f, 0.1f, 10.0f), 0.05f);
//             CreateStaticBody(float3.zero, quaternion.identity, collider);
//         }

        // Dynamic compound
        {
            var children = new NativeArray<CompoundCollider.ColliderBlobInstance>(3, Allocator.Temp)
            {
                [0] = new CompoundCollider.ColliderBlobInstance
                {
                    CompoundFromChild = new RigidTransform(quaternion.identity, new float3(-1, 0, 0)),
                    Collider = Unity.Physics.BoxCollider.Create(float3.zero, quaternion.identity, new float3(1), 0.05f)
                },
                [1] = new CompoundCollider.ColliderBlobInstance
                {
                    CompoundFromChild = RigidTransform.identity,
                    Collider = Unity.Physics.SphereCollider.Create(float3.zero, 0.5f)
                },
                [2] = new CompoundCollider.ColliderBlobInstance
                {
                    CompoundFromChild = new RigidTransform(quaternion.identity, new float3(1, 0, 0)),
                    Collider = Unity.Physics.BoxCollider.Create(float3.zero, quaternion.identity, new float3(1), 0.05f)
                }
            };

            BlobAssetReference<Unity.Physics.Collider> collider = CompoundCollider.Create(children);
            children.Dispose();

            CreateDynamicBody(new float3(0, 1, 0), quaternion.identity, collider, float3.zero, float3.zero, 1.0f);
        }
    }
}
