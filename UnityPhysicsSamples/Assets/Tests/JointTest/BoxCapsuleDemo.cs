using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BoxCollider = Unity.Physics.BoxCollider;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using Collider = Unity.Physics.Collider;
using Material = Unity.Physics.Material;

public class BoxCapsuleDemo : BasePhysicsDemo
{
    private static CollisionFilter filter(uint layer, uint disabled)
    {
        return new CollisionFilter
        {
            CollidesWith = layer,
            BelongsTo = ~disabled,
            GroupIndex = 0
        };
    }

    protected override void Start()
    {
        base.Start();
        //base.init(float3.zero); // no gravity

        // Enable the joint viewer
        SetDebugDisplay(new Unity.Physics.Authoring.PhysicsDebugDisplayData
        {
            DrawJoints = 1,
            DrawContacts = 1
        });

        const uint layerBody = (1 << 0);
        const uint layerHead = (1 << 1);
        const uint layerUpperArm = (1 << 2);
        const uint layerForearm = (1 << 3);
        const uint layerHand = (1 << 4);
        const uint layerThigh = (1 << 5);
        //         const uint layerCalf = (1 << 6);
        //         const uint layerFoot = (1 << 7);
        const int layerGround = (1 << 8);

        // Floor
        {
            BlobAssetReference<Collider> collider = BoxCollider.Create(float3.zero, Quaternion.identity, new float3(20.0f, 0.2f, 20.0f), 0.01f, filter(layerGround, 0));
            CreateStaticBody(new float3(0, -0.1f, 0), quaternion.identity, collider);
        }

        // Body
        float3 bodyHalfExtents = new float3(0.2f, 0.3f, 0.075f);
        float3 bodyPosition = new float3(0, 0.1f, 0);
        Entity body;
        {
            BlobAssetReference<Collider> collider = BoxCollider.Create(
                float3.zero, Quaternion.identity, 2.0f * bodyHalfExtents, 0.01f, filter(layerBody, layerHead | layerUpperArm | layerThigh));
            quaternion q = quaternion.AxisAngle(new float3(1, 0, 0), (float)math.PI / 2.0f);
            body = CreateDynamicBody(bodyPosition, quaternion.identity, collider, float3.zero, float3.zero, 10.0f);
            //body = createStaticBody(bodyPosition, quaternion.identity, collider);
        }

        // Arms
        {
            float handLength = 0.025f;
            float handRadius = 0.055f;
            BlobAssetReference<Collider> handCollider = CapsuleCollider.Create(new float3(-handLength / 2, 0, 0), new float3(handLength / 2, 0, 0), handRadius,
                filter(layerHand, layerForearm));

            for (int i = 0; i < 1; i++)
            {
                float s = i * 2 - 1.0f;

                float3 handPosition = new float3(0, 0.3f, 0);
                Entity hand = CreateDynamicBody(handPosition, quaternion.identity, handCollider, float3.zero, float3.zero, 10.0f);
            }
        }
    }
}
