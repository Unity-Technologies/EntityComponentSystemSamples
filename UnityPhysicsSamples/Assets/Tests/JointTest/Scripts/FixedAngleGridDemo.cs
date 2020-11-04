using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

public class FixedAngleGridScene : SceneCreationSettings {};

public class FixedAngleGridDemo : SceneCreationAuthoring<FixedAngleGridScene> {}

public class FixAngleGridDemoSystem : SceneCreationSystem<FixedAngleGridScene>
{
    public override void CreateScene(FixedAngleGridScene sceneSettings)
    {
        BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(new BoxGeometry
        {
            Center = float3.zero,
            Orientation = quaternion.identity,
            Size = new float3(0.25f),
            BevelRadius = 0.0f
        });
        CreatedColliders.Add(collider);

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

            var jointData = new PhysicsJoint
            {
                BodyAFromJoint = new RigidTransform(orientationA, pivotLocal),
                BodyBFromJoint = new RigidTransform(orientationB, pivotInWorld)
            };
            jointData.SetConstraints(new FixedList128<Constraint>
            {
                Length = 2,
                [0] = Constraint.BallAndSocket(),
                [1] = new Constraint
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
