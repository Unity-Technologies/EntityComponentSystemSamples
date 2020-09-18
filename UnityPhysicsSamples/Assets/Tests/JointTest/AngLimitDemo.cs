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
        base.init();


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
        {
            var limit = math.radians(45.0f);

            var worldFromStatic = GetBodyTransform(staticEntity);
            var worldFromDynamic = GetBodyTransform(dynamicEntity);
            var StaticFromDynamic = math.mul(math.inverse(worldFromStatic), worldFromDynamic);

            float3 pivotFromDynamic = new float3(0, 0, 0);
            float3 pivotFromStatic = math.transform(StaticFromDynamic, pivotFromDynamic);

            var jointData = new PhysicsJoint
            {
                BodyAFromJoint = new RigidTransform(quaternion.identity, pivotFromDynamic),
                BodyBFromJoint = new RigidTransform(quaternion.identity, pivotFromStatic)
            };
            jointData.SetConstraints(new FixedList128<Constraint>
            {
                new Constraint
                {
                    ConstrainedAxes = new bool3(true, false, false),
                    Type = ConstraintType.Angular,
                    Min = -limit,
                    Max = limit,
                    SpringFrequency = Constraint.DefaultSpringFrequency,
                    SpringDamping = Constraint.DefaultSpringDamping,
                },
                Constraint.BallAndSocket(),
            });
            CreateJoint(jointData, dynamicEntity, staticEntity);
        }
    }
}
