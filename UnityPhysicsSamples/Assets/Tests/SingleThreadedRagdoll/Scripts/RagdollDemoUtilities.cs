using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Assertions;
using static BasicBodyInfo;
using static Unity.Physics.Math;

public static class RagdollDemoUtilities
{
    public struct BodyInfo
    {
        public float3 Position;
        public quaternion Orientation;
        public float3 LinearVelocity;
        public float3 AngularVelocity;
        public float Mass;
        public BlobAssetReference<Unity.Physics.Collider> Collider;
        public bool IsDynamic;
    }

    public struct JointInfo
    {
        public int BodyIndexA;
        public int BodyIndexB;
        public PhysicsJoint JointData;
        public bool EnableCollision;
    }

    public static BodyInfo CreateBody(GameObject gameObject)
    {
        var bounds = gameObject.GetComponent<MeshRenderer>().bounds;
        var basicBodyInfo = gameObject.GetComponent<BasicBodyInfo>();
        BlobAssetReference<Unity.Physics.Collider> collider = default;

        switch (basicBodyInfo.Type)
        {
            case BodyType.Sphere:
                float radius = math.cmax(bounds.extents);
                collider = Unity.Physics.SphereCollider.Create(
                    new SphereGeometry
                    {
                        Center = float3.zero,
                        Radius = radius
                    });
                break;
            case BodyType.Box:
                collider = Unity.Physics.BoxCollider.Create(
                    new BoxGeometry
                    {
                        Center = float3.zero,
                        Orientation = quaternion.identity,
                        Size = bounds.size,
                        BevelRadius = 0.0f
                    });
                break;
            case BodyType.ConvexHull:
                var mesh = gameObject.GetComponent<MeshFilter>().mesh;
                var scale = gameObject.transform.lossyScale;
                NativeArray<float3> points = new NativeArray<float3>(mesh.vertexCount, Allocator.Temp);
                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    points[i] = mesh.vertices[i];
                    points[i] *= scale;
                }
                ConvexHullGenerationParameters def = ConvexHullGenerationParameters.Default;
                def.BevelRadius = 0.0f;
                collider = Unity.Physics.ConvexCollider.Create(
                    points, def, CollisionFilter.Default);
                break;
            case BodyType.Capsule:
                var capsuleRadius = math.cmin(bounds.extents);
                var capsuleLength = math.cmax(bounds.extents);
                var capsuleGeometry = new CapsuleGeometry
                {
                    Radius = capsuleRadius,
                    Vertex0 = new float3(0, capsuleLength - capsuleRadius, 0f),
                    Vertex1 = new float3(0, -1.0f * (capsuleLength - capsuleRadius), 0f)
                };
                collider = Unity.Physics.CapsuleCollider.Create(capsuleGeometry);
                break;
            default:
                Assert.IsTrue(false, "Invalid body type");
                break;
        }

        bool isDynamic = !gameObject.GetComponent<BasicBodyInfo>().IsStatic;

        return new BodyInfo
        {
            Mass = isDynamic ? basicBodyInfo.Mass : 0f,
            Collider = collider,
            AngularVelocity = float3.zero,
            LinearVelocity = float3.zero,
            Orientation = gameObject.transform.rotation,
            Position = gameObject.transform.position,
            IsDynamic = isDynamic
        };
    }

    public static PhysicsJoint CreateJoint(GameObject parentBody, GameObject childBody, BasicJointInfo.BasicJointType jointType)
    {
        var bodyPBounds = parentBody.GetComponent<MeshRenderer>().bounds;
        var bodyCBounds = childBody.GetComponent<MeshRenderer>().bounds;

        var pointConPWorld = bodyPBounds.ClosestPoint(bodyCBounds.center);
        var pointPonCWorld = bodyCBounds.ClosestPoint(bodyPBounds.center);

        var bodyPTransform = new RigidTransform(parentBody.transform.rotation, parentBody.transform.position);// was torso
        var bodyCTransform = new RigidTransform(childBody.transform.rotation, childBody.transform.position);// was head

        PhysicsJoint jointData = default;
        switch (jointType)
        {
            case BasicJointInfo.BasicJointType.BallAndSocket:
            {
                var pivotP = math.transform(math.inverse(bodyPTransform), pointConPWorld);
                var pivotC = math.transform(math.inverse(bodyCTransform), pointConPWorld);
                jointData = PhysicsJoint.CreateBallAndSocket(pivotP, pivotC);
            }
            break;
            case BasicJointInfo.BasicJointType.Distance:
            {
                var pivotP = math.transform(math.inverse(bodyPTransform), pointConPWorld);
                var pivotC = math.transform(math.inverse(bodyCTransform), pointPonCWorld);
                var range = new FloatRange(0, math.distance(pointConPWorld, pointPonCWorld));
                jointData = PhysicsJoint.CreateLimitedDistance(pivotP, pivotC, range);
            }
            break;
            case BasicJointInfo.BasicJointType.Hinge:
            {
                var commonPivotPointWorld = math.lerp(pointConPWorld, pointPonCWorld, 0.5f);

                // assume a vertical hinge joint
                var axisP = math.rotate(math.inverse(bodyPTransform.rot), math.up());
                var axisC = math.rotate(math.inverse(bodyCTransform.rot), math.up());

                float3 perpendicularAxisA, perpendicularAxisB;
                Math.CalculatePerpendicularNormalized(axisP, out perpendicularAxisA, out _);
                Math.CalculatePerpendicularNormalized(axisC, out perpendicularAxisB, out _);

                var pivotP = math.transform(math.inverse(bodyPTransform), commonPivotPointWorld);
                var pivotC = math.transform(math.inverse(bodyCTransform), commonPivotPointWorld);
                var jointFrameP = new BodyFrame { Axis = axisP, PerpendicularAxis = perpendicularAxisA, Position = pivotP };
                var jointFrameC = new BodyFrame { Axis = axisC, PerpendicularAxis = perpendicularAxisB, Position = pivotC };
                var range = new FloatRange(math.radians(-90), math.radians(90.0f));
                jointData = PhysicsJoint.CreateLimitedHinge(jointFrameP, jointFrameC, range);
            }
            break;
            default:
                break;
        }
        return jointData;
    }
}
