using Unity.Physics;
using Unity.Entities;
using Unity.Mathematics;
using BoxCollider = Unity.Physics.BoxCollider;
using Collider = Unity.Physics.Collider;
using Material = Unity.Physics.Material;
using static Unity.Physics.Math;

public class LimitedHingeDemo : BasePhysicsDemo
{
    protected override unsafe void Start()
    {
        init(); // no gravity

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
                new BoxGeometry
                {
                    Center = float3.zero,
                    Orientation = quaternion.identity,
                    Size = new float3(1.0f, 0.2f, 0.2f),
                    BevelRadius = 0.0f
                },
                filter, Material.Default);
            entities[i] = CreateDynamicBody(float3.zero, quaternion.identity, collider, float3.zero, new float3(0, 1 - i, 0), 1.0f);
        }

        var jointFrame = new BodyFrame { Axis = new float3(0, 1, 0), PerpendicularAxis = new float3(0, 0, 1) };
        PhysicsJoint hingeData =
            PhysicsJoint.CreateLimitedHinge(jointFrame, jointFrame, new FloatRange(-math.PI, -0.2f));
        CreateJoint(hingeData, entities[0], entities[1]);
    }
}
