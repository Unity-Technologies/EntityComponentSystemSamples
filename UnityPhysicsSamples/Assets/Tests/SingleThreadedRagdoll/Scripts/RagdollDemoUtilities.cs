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
        public int BodyAIndex;
        public int BodyBIndex;
        public BlobAssetReference<JointData> JointData;
        public bool EnabledCollisions;
    }

    public static BodyInfo CreateBody(GameObject gameObject)
    {
        var basicBodyInfo = gameObject.GetComponent<BasicBodyInfo>();
        var transform = gameObject.GetComponent<Transform>();
        var scale = transform.localScale;
        BlobAssetReference<Unity.Physics.Collider> collider = default;

        switch (basicBodyInfo.Type)
        {
            case BodyType.Sphere:
                Assert.IsTrue(scale.x == scale.y && scale.y == scale.z);
                float radius = 0.5f * scale.x;
                collider = Unity.Physics.SphereCollider.Create(
                    new SphereGeometry
                    {
                        Center = float3.zero,
                        Radius = radius
                    });
                break;
            case BodyType.Box:
                float3 size = scale * 1.0f;
                collider = Unity.Physics.BoxCollider.Create(
                    new BoxGeometry
                    {
                        Center = float3.zero,
                        Orientation = quaternion.identity,
                        Size = size,
                        BevelRadius = 0.0f
                    });
                break;
            case BodyType.ConvexHull:
                var mesh = gameObject.GetComponent<MeshFilter>().mesh;
                NativeArray<float3> points = new NativeArray<float3>(mesh.vertexCount, Allocator.Temp);
                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    points[i] = mesh.vertices[i];
                }
                ConvexHullGenerationParameters def = ConvexHullGenerationParameters.Default;
                def.BevelRadius = 0.0f;
                collider = Unity.Physics.ConvexCollider.Create(
                    points, def, CollisionFilter.Default);
                break;
            case BodyType.Capsule:

                var capsuleTransform = gameObject.GetComponent<Transform>();
                Assert.IsTrue(capsuleTransform.localScale.x == capsuleTransform.localScale.z);

                var capsuleRadius = capsuleTransform.localScale.x / 2;
                var capsuleGeometry = new CapsuleGeometry
                {
                    Radius = capsuleRadius,
                    Vertex0 = new float3(0, capsuleTransform.localScale.y - capsuleRadius, 0f),
                    Vertex1 = new float3(0, -1.0f * (capsuleTransform.localScale.y - capsuleRadius), 0f)
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
            Orientation = transform.rotation,
            Position = transform.position,
            IsDynamic = isDynamic
        };
    }

    public static void CreateNeck(GameObject torso, GameObject head, out BlobAssetReference<JointData> jointData0, out BlobAssetReference<JointData> jointData1)
    {
        var headTransform = head.GetComponent<Transform>();
        float headRadius = 0.5f * headTransform.localScale.x;

        float3 pivotHead = new float3(0, -headRadius, 0);
        var torsoTransform = torso.GetComponent<Transform>();
        var torsoRigidTransform = new RigidTransform(torsoTransform.rotation, torsoTransform.position);

        var headRigidTransform = new RigidTransform(headTransform.rotation, headTransform.position);

        float3 pivotTorso = math.transform(math.inverse(torsoRigidTransform), math.transform(headRigidTransform, pivotHead));
        float3 axis = new float3(0, 1, 0);
        float3 perpendicular = new float3(0, 0, 1);
        float coneAngle = math.PI / 5.0f;
        var perpendicularAngle = new FloatRange { Max = math.PI };
        var twistAngle = new FloatRange(-math.PI / 3f, math.PI / 3f);

        var jointFrameTorso = new JointFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotTorso };
        var jointFrameHead = new JointFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotHead };

        JointData.CreateRagdoll(jointFrameTorso, jointFrameHead, coneAngle, perpendicularAngle, twistAngle, out jointData0, out jointData1);
    }

    public static void CreateShoulder(GameObject torso, GameObject upperArm, out BlobAssetReference<JointData> jointData0, out BlobAssetReference<JointData> jointData1)
    {
        float armLength = 2 * upperArm.transform.localScale.y;

        float sign = math.sign(-upperArm.transform.position.x);

        var torsoRigidTransform = new RigidTransform(torso.transform.rotation, torso.transform.position);
        var upperArmRigidTransform = new RigidTransform(upperArm.transform.rotation, upperArm.transform.position);

        float3 pivotArm = new float3(sign * armLength / 2.0f, 0, 0);
        pivotArm = math.rotate(math.inverse(upperArmRigidTransform.rot), pivotArm);
        float3 pivotBody = math.transform(math.inverse(torsoRigidTransform), math.transform(upperArmRigidTransform, pivotArm));

        float3 twistAxis = new float3(-1.0f * sign, 0, 0);
        float3 perpendicularAxis = new float3(0, 0, 1);

        float3 twistAxisArm = math.rotate(math.inverse(upperArmRigidTransform), twistAxis);
        float3 twistAxisTorso = math.rotate(math.inverse(torsoRigidTransform), twistAxis);

        float3 perpendicularAxisArm = math.rotate(math.inverse(upperArmRigidTransform), perpendicularAxis);
        float3 perpendicularAxisTorso = math.rotate(math.inverse(torsoRigidTransform), perpendicularAxis);

        float coneAngle = math.PI / 4.0f;
        var perpendicularAngle = new FloatRange(math.PI / 6f, math.PI * 5f / 6f);
        var twistAngle = new FloatRange(-0.0872665f, 0.0872665f);

        var jointFrameBody = new JointFrame { Axis = twistAxisTorso, PerpendicularAxis = perpendicularAxisTorso, Position = pivotBody };
        var jointFrameArm = new JointFrame { Axis = twistAxisArm, PerpendicularAxis = perpendicularAxisArm, Position = pivotArm };

        JointData.CreateRagdoll(jointFrameBody, jointFrameArm, coneAngle, perpendicularAngle, twistAngle, out jointData0, out jointData1);
    }

    public static void CreateWaist(GameObject torso, GameObject pelvis, out BlobAssetReference<JointData> jointData0, out BlobAssetReference<JointData> jointData1)
    {
        float3 pivotTorso = float3.zero;

        RigidTransform pelvisTransform = new RigidTransform(pelvis.transform.rotation, pelvis.transform.position);
        RigidTransform torsoTransform = new RigidTransform(torso.transform.rotation, torso.transform.position);

        float3 pivotPelvis = math.transform(math.inverse(pelvisTransform), math.transform(torsoTransform, pivotTorso));
        float3 axisPelvis = new float3(0, 0, -1);
        float3 axisTorso = axisPelvis;

        float3 axisPerpendicular = new float3(1, 0, 0);

        float3 perpendicularPelvis = math.rotate(math.inverse(pelvisTransform), axisPerpendicular);
        float3 perpendicularTorso = math.rotate(math.inverse(torsoTransform), axisPerpendicular);

        float coneAngle = 0.0872665f;
        var perpendicularAngle = new FloatRange { Max = math.PI };
        var twistAngle = new FloatRange(-0.01f, 0.1f);

        var jointFrameTorso = new JointFrame { Axis = axisTorso, PerpendicularAxis = perpendicularTorso, Position = pivotTorso };
        var jointFramePelvis = new JointFrame { Axis = axisPelvis, PerpendicularAxis = perpendicularPelvis, Position = pivotPelvis };

        JointData.CreateRagdoll(jointFrameTorso, jointFramePelvis, coneAngle, perpendicularAngle, twistAngle, out jointData0, out jointData1);
    }

    public static void CreateHip(GameObject pelvis, GameObject upperLeg, out BlobAssetReference<JointData> jointData0, out BlobAssetReference<JointData> jointData1)
    {
        float upperLegHeight = 2.0f * upperLeg.transform.localScale.y;

        var pelvisTransform = new RigidTransform(pelvis.transform.rotation, pelvis.transform.position);
        var upperLegTransform = new RigidTransform(upperLeg.transform.rotation, upperLeg.transform.position);

        float3 pivotLeg = new float3(0, upperLegHeight / 2.0f, 0);
        float3 pivotPelvis = math.transform(math.inverse(pelvisTransform), math.transform(upperLegTransform, pivotLeg));

        float3 twistAxis = new float3(0, -1, 0);
        float3 perpendicularAxis = new float3(1, 0, 0);

        float3 twistAxisLeg = math.rotate(math.inverse(upperLegTransform), twistAxis);
        float3 twistAxisPelvis = math.rotate(math.inverse(pelvisTransform), twistAxis);

        float3 perpendicularAxisLeg = math.rotate(math.inverse(upperLegTransform), perpendicularAxis);
        float3 perpendicularAxisPelvis = math.rotate(math.inverse(pelvisTransform), perpendicularAxis);

        float coneAngle = math.PI / 4.0f;
        var perpendicularAngle = new FloatRange(math.PI / 3f, math.PI * 2f / 3f);
        var twistAngle = new FloatRange(-0.2f, 0.2f);

        var jointFramePelvis = new JointFrame { Axis = twistAxisPelvis, PerpendicularAxis = perpendicularAxisPelvis, Position = pivotPelvis };
        var jointFrameLeg = new JointFrame { Axis = twistAxisLeg, PerpendicularAxis = perpendicularAxisLeg, Position = pivotLeg };

        JointData.CreateRagdoll(jointFramePelvis, jointFrameLeg, coneAngle, perpendicularAngle, twistAngle, out jointData0, out jointData1);
    }

    public static BlobAssetReference<JointData> CreateElbow(GameObject upperArm, GameObject lowerArm)
    {
        float upperArmLength = 2 * upperArm.transform.localScale.y;
        float lowerArmLength = 2 * lowerArm.transform.localScale.y;

        float sign = math.sign(-1.0f * upperArm.transform.position.x);

        float3 pivotUpper = new float3(-1.0f * sign * upperArmLength / 2.0f, 0, 0);
        float3 pivotLower = new float3(sign * lowerArmLength / 2.0f, 0, 0);
        pivotUpper = math.rotate(math.inverse(upperArm.transform.rotation), pivotUpper);
        pivotLower = math.rotate(math.inverse(lowerArm.transform.rotation), pivotLower);
        float3 axis = new float3(0, 0, 1);
        float3 perpendicular = new float3(0, 1, 0);

        var jointFrameUpperArm = new JointFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotUpper };
        var jointFrameLowerArm = new JointFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotLower };

        return JointData.CreateLimitedHinge(jointFrameUpperArm, jointFrameLowerArm, new FloatRange { Max = 3f });
    }

    public static BlobAssetReference<JointData> CreateWrist(GameObject lowerArm, GameObject hand)
    {
        float armLength = 2.0f * lowerArm.transform.localScale.y;
        float handLength = 2.0f * hand.transform.localScale.y;

        float sign = math.sign(-1.0f * lowerArm.transform.position.x);

        float3 pivotFore = new float3(0, -1.0f * sign * armLength / 2.0f, 0);
        float3 pivotHand = new float3(0, sign * handLength / 2.0f, 0);
        float3 axis = new float3(0, 0, 1);
        float3 perpendicular = new float3(0, 1, 0);

        var jointFrameForearm = new JointFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotFore };
        var jointFrameHand = new JointFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotHand };

        return JointData.CreateLimitedHinge(jointFrameForearm, jointFrameHand, new FloatRange(-0.3f, 0.6f));
    }

    public static BlobAssetReference<JointData> CreateKnee(GameObject upperLeg, GameObject lowerLeg)
    {
        float upperLegHeight = 2.0f * upperLeg.transform.localScale.y;

        float3 pivotUpperLeg = new float3(0, -upperLegHeight / 2.0f, 0);

        var lowerLegTransform = new RigidTransform(lowerLeg.transform.rotation, lowerLeg.transform.position);
        var upperLegTransform = new RigidTransform(upperLeg.transform.rotation, upperLeg.transform.position);

        float3 pivotLowerLeg = math.transform(math.inverse(lowerLegTransform), math.transform(upperLegTransform, pivotUpperLeg));

        float3 axis = new float3(1, 0, 0);
        float3 perpendicular = new float3(0, 0, 1);

        var jointFrameUpperLeg = new JointFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotUpperLeg };
        var jointFrameLowerLeg = new JointFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotLowerLeg };

        return JointData.CreateLimitedHinge(jointFrameUpperLeg, jointFrameLowerLeg, new FloatRange { Min = -1.2f });
    }

    public static BlobAssetReference<JointData> CreateAnkle(GameObject lowerLeg, GameObject foot)
    {
        float lowerLegLength = 2.0f * lowerLeg.transform.localScale.y;

        var lowerLegTransform = new RigidTransform(lowerLeg.transform.rotation, lowerLeg.transform.position);
        var footTransform = new RigidTransform(foot.transform.rotation, foot.transform.position);

        float3 pivotLowerLeg = new float3(0, -lowerLegLength / 2.0f, 0);
        float3 pivotFoot = math.transform(math.inverse(footTransform), math.transform(lowerLegTransform, pivotLowerLeg));

        float3 axis = new float3(1, 0, 0);
        float3 perpendicular = new float3(0, 0, 1);

        var jointFrameLowerLeg = new JointFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotLowerLeg };
        var jointFrameFoot = new JointFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotFoot };

        return JointData.CreateLimitedHinge(jointFrameLowerLeg, jointFrameFoot, new FloatRange(-0.4f, 0.1f));
    }
}
