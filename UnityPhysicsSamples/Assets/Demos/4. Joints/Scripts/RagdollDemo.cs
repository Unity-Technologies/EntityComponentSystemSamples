using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using static Unity.Physics.Math;

public class RagdollDemo : BasePhysicsDemo
{
    public UnityEngine.Mesh torsoMesh;
    public UnityEngine.Mesh renderMesh;

    public int numberOfRagdolls = 1;
    [Range(0, 1)] public float rangeGain = 1.0f;

    private enum layer
    {
        Torso = (1 << 0),
        Head = (1 << 1),
        Pelvis = (1 << 2),
        UpperArm = (1 << 3),
        Forearm = (1 << 4),
        Hand = (1 << 5),
        Thigh = (1 << 6),
        Calf = (1 << 7),
        Foot = (1 << 8),
        Ground = (1 << 9)
    };

    private static CollisionFilter layerFilter(layer layer, layer disabled)
    {
        return new CollisionFilter
        {
            BelongsTo = (uint)layer,
            CollidesWith = ~(uint)disabled,
            GroupIndex = 0
        };
    }

    private static CollisionFilter groupFilter(int groupIndex)
    {
        return new CollisionFilter
        {
            BelongsTo = 4294967295,
            CollidesWith = 4294967295,
            GroupIndex = groupIndex
        };
    }

    private void SwapRenderMesh(Entity entity, bool isTorso)
    {
        var entityManager = BasePhysicsDemo.DefaultWorld.EntityManager;

        entityManager.RemoveComponent<RenderMesh>(entity);
        entityManager.AddSharedComponentData(entity, new RenderMesh
        {
            mesh = isTorso ? torsoMesh : renderMesh,
            material = dynamicMaterial
        });
        if (!isTorso)
        {
            var renderBounds = entityManager.GetComponentData<RenderBounds>(entity);
            entityManager.AddComponentData<NonUniformScale>(entity, new NonUniformScale
            {
                Value = renderBounds.Value.Size,
            });
        }
    }

    private void CreateRagdoll(float3 positionOffset, quaternion rotationOffset, int ragdollIndex = 1, bool internalCollisions = false, float rangeGain = 1.0f)
    {
        CreateRagdoll(positionOffset, rotationOffset, float3.zero, ragdollIndex, internalCollisions, rangeGain);
    }

    private void CreateRagdoll(float3 positionOffset, quaternion rotationOffset, float3 initialVelocity, int ragdollIndex = 1, bool internalCollisions = false, float rangeGain = 1.0f)
    {
        var entityManager = BasePhysicsDemo.DefaultWorld.EntityManager;

        var entities = new NativeList<Entity>(Allocator.Temp);
        var rangeModifier = new float2(math.max(0, math.min(rangeGain, 1)));

        // Head
        float headRadius = 0.1f;
        float3 headPosition = new float3(0, 1.8f, headRadius);
        Entity head;
        {
            CollisionFilter filter = internalCollisions ? layerFilter(layer.Head, layer.Torso) : groupFilter(-ragdollIndex);
            BlobAssetReference<Unity.Physics.Collider> headCollider = Unity.Physics.CapsuleCollider.Create(new CapsuleGeometry
            {
                Vertex0 = new float3(0, 0, 0),
                Vertex1 = new float3(0, 0, headRadius / 4),
                Radius = headRadius
            }, filter);
            head = CreateDynamicBody(headPosition, quaternion.identity, headCollider, float3.zero, float3.zero, 5.0f);
        }
        entities.Add(head);

        // Torso
        float3 torsoSize;
        float3 torsoPosition;
        Entity torso;
        {
            //UnityEngine.Mesh torsoMesh = (UnityEngine.Mesh)Resources.Load("torso", typeof(UnityEngine.Mesh));
            torsoSize = torsoMesh.bounds.size;
            torsoPosition = headPosition - new float3(0, headRadius * 3.0f / 4.0f + torsoSize.y, 0);

            CollisionFilter filter = internalCollisions ? layerFilter(layer.Torso, layer.Thigh | layer.Head | layer.UpperArm | layer.Pelvis) : groupFilter(-ragdollIndex);

            NativeArray<float3> points = new NativeArray<float3>(torsoMesh.vertices.Length, Allocator.TempJob);
            for (int i = 0; i < torsoMesh.vertices.Length; i++)
            {
                points[i] = torsoMesh.vertices[i];
            }
            BlobAssetReference<Unity.Physics.Collider> collider = ConvexCollider.Create(
                points, ConvexHullGenerationParameters.Default, CollisionFilter.Default
            );
            points.Dispose();
            collider.Value.Filter = filter;
            torso = CreateDynamicBody(torsoPosition, quaternion.identity, collider, float3.zero, float3.zero, 20.0f);
        }
        entities.Add(torso);

        // Neck
        {
            float3 pivotHead = new float3(0, -headRadius, 0);
            float3 pivotTorso = math.transform(math.inverse(GetBodyTransform(torso)), math.transform(GetBodyTransform(head), pivotHead));
            float3 axisHead = new float3(0, 0, 1);
            float3 perpendicular = new float3(1, 0, 0);
            FloatRange coneAngle = new FloatRange(math.radians(0), math.radians(45)) * rangeModifier;
            FloatRange perpendicularAngle = new FloatRange(math.radians(-30), math.radians(+30)) * rangeModifier;
            FloatRange twistAngle = new FloatRange(math.radians(-5), math.radians(5)) * rangeModifier;

            var axisTorso = math.rotate(math.inverse(GetBodyTransform(torso).rot), math.rotate(GetBodyTransform(head).rot, axisHead));
            axisTorso = math.rotate(quaternion.AxisAngle(perpendicular, math.radians(10)), axisTorso);

            var headFrame = new BodyFrame { Axis = axisHead, PerpendicularAxis = perpendicular, Position = pivotHead };
            var torsoFrame = new BodyFrame { Axis = axisTorso, PerpendicularAxis = perpendicular, Position = pivotTorso };

            PhysicsJoint.CreateRagdoll(headFrame, torsoFrame, coneAngle.Max, perpendicularAngle, twistAngle, out var ragdoll0, out var ragdoll1);
            CreateJoint(ragdoll0, head, torso);
            CreateJoint(ragdoll1, head, torso);
        }

        // Arms
        {
            float armLength = 0.25f;
            float armRadius = 0.05f;
            CollisionFilter armUpperFilter = internalCollisions ? layerFilter(layer.UpperArm, layer.Torso | layer.Forearm) : groupFilter(-ragdollIndex);
            CollisionFilter armLowerFilter = internalCollisions ? layerFilter(layer.Forearm, layer.UpperArm | layer.Hand) : groupFilter(-ragdollIndex);

            BlobAssetReference<Unity.Physics.Collider> upperArmCollider = Unity.Physics.CapsuleCollider.Create(new CapsuleGeometry
            {
                Vertex0 = new float3(-armLength / 2, 0, 0),
                Vertex1 = new float3(armLength / 2, 0, 0),
                Radius = armRadius
            }, armUpperFilter);
            BlobAssetReference<Unity.Physics.Collider> foreArmCollider = Unity.Physics.CapsuleCollider.Create(new CapsuleGeometry
            {
                Vertex0 = new float3(-armLength / 2, 0, 0),
                Vertex1 = new float3(armLength / 2, 0, 0),
                Radius = armRadius
            }, armLowerFilter);

            float handLength = 0.025f;
            float handRadius = 0.055f;
            CollisionFilter handFilter = internalCollisions ? layerFilter(layer.Hand, layer.Forearm) : groupFilter(-ragdollIndex);

            BlobAssetReference<Unity.Physics.Collider> handCollider = Unity.Physics.CapsuleCollider.Create(new CapsuleGeometry
            {
                Vertex0 = new float3(-handLength / 2, 0, 0),
                Vertex1 = new float3(handLength / 2, 0, 0),
                Radius = handRadius
            }, handFilter);

            for (int i = 0; i < 2; i++)
            {
                float s = i * 2 - 1.0f;

                float3 upperArmPosition = torsoPosition + new float3(s * (torsoSize.x + armLength) / 2.0f, 0.9f * torsoSize.y - armRadius, 0.0f);
                Entity upperArm = CreateDynamicBody(upperArmPosition, quaternion.identity, upperArmCollider, float3.zero, float3.zero, 10.0f);
                float3 foreArmPosition = upperArmPosition + new float3(armLength * s, 0, 0);
                Entity foreArm = CreateDynamicBody(foreArmPosition, quaternion.identity, foreArmCollider, float3.zero, float3.zero, 5.0f);
                float3 handPosition = foreArmPosition + new float3((armLength + handLength) / 2.0f * s, 0, 0);
                Entity hand = CreateDynamicBody(handPosition, quaternion.identity, handCollider, float3.zero, float3.zero, 2.0f);

                entities.Add(upperArm);
                entities.Add(foreArm);
                entities.Add(hand);

                // shoulder
                {
                    float3 pivotArm = new float3(-s * armLength / 2.0f, 0, 0);
                    float3 pivotTorso = math.transform(math.inverse(GetBodyTransform(torso)), math.transform(GetBodyTransform(upperArm), pivotArm));
                    float3 axisArm = new float3(-s, 0, 0);
                    float3 perpendicularArm = new float3(0, 1, 0);
                    FloatRange coneAngle = new FloatRange(math.radians(0), math.radians(80)) * rangeModifier;
                    FloatRange perpendicularAngle = new FloatRange(math.radians(-70), math.radians(20)) * rangeModifier;
                    FloatRange twistAngle = new FloatRange(math.radians(-5), math.radians(5)) * rangeModifier;

                    var axisTorso = math.rotate(math.inverse(GetBodyTransform(torso).rot), math.rotate(GetBodyTransform(upperArm).rot, axisArm));
                    axisTorso = math.rotate(quaternion.AxisAngle(perpendicularArm, math.radians(-s * 45.0f)), axisTorso);

                    var armFrame = new BodyFrame { Axis = axisArm, PerpendicularAxis = perpendicularArm, Position = pivotArm };
                    var bodyFrame = new BodyFrame { Axis = axisTorso, PerpendicularAxis = perpendicularArm, Position = pivotTorso };

                    PhysicsJoint.CreateRagdoll(armFrame, bodyFrame, coneAngle.Max, perpendicularAngle, twistAngle, out var ragdoll0, out var ragdoll1);
                    CreateJoint(ragdoll0, upperArm, torso);
                    CreateJoint(ragdoll1, upperArm, torso);
                }

                // elbow
                {
                    float3 pivotUpper = new float3(s * armLength / 2.0f, 0, 0);
                    float3 pivotFore = -pivotUpper;
                    float3 axis = new float3(0, -s, 0);
                    float3 perpendicular = new float3(-s, 0, 0);

                    var lowerArmFrame = new BodyFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotFore };
                    var upperArmFrame = new BodyFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotUpper };
                    var hingeRange = new FloatRange(math.radians(0), math.radians(100));
                    hingeRange = (hingeRange - new float2(hingeRange.Mid)) * rangeModifier + hingeRange.Mid;
                    PhysicsJoint hinge = PhysicsJoint.CreateLimitedHinge(lowerArmFrame, upperArmFrame, hingeRange);
                    CreateJoint(hinge, foreArm, upperArm);
                }

                // wrist
                {
                    float3 pivotFore = new float3(s * armLength / 2.0f, 0, 0);
                    float3 pivotHand = new float3(-s * handLength / 2.0f, 0, 0);
                    float3 axis = new float3(0, 0, -s);
                    float3 perpendicular = new float3(0, 0, 1);

                    var handFrame = new BodyFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotHand };
                    var forearmFrame = new BodyFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotFore };
                    var hingeRange = new FloatRange(math.radians(0), math.radians(135)) * rangeModifier;
                    PhysicsJoint hinge = PhysicsJoint.CreateLimitedHinge(handFrame, forearmFrame, hingeRange);
                    CreateJoint(hinge, hand, foreArm);
                }
            }
        }

        // Pelvis
        float pelvisRadius = 0.08f;
        float pelvisLength = 0.22f;
        float3 pelvisPosition = torsoPosition - new float3(0, pelvisRadius * 0.75f, 0.0f);
        Entity pelvis;
        {
            CollisionFilter filter = internalCollisions ? layerFilter(layer.Pelvis, layer.Torso | layer.Thigh) : groupFilter(-ragdollIndex);
            BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.CapsuleCollider.Create(new CapsuleGeometry
            {
                Vertex0 = new float3(-pelvisLength / 2, 0, 0),
                Vertex1 = new float3(pelvisLength / 2, 0, 0),
                Radius = pelvisRadius
            }, filter);
            pelvis = CreateDynamicBody(pelvisPosition, quaternion.identity, collider, float3.zero, float3.zero, 15.0f);
        }
        entities.Add(pelvis);

        // Waist
        {
            float3 pivotTorso = float3.zero;
            float3 pivotPelvis = math.transform(math.inverse(GetBodyTransform(pelvis)), math.transform(GetBodyTransform(torso), pivotTorso));
            float3 axis = new float3(0, 1, 0);
            float3 perpendicular = new float3(0, 0, 1);
            FloatRange coneAngle = new FloatRange(math.radians(0), math.radians(5)) * rangeModifier;
            FloatRange perpendicularAngle = new FloatRange(math.radians(-5), math.radians(90)) * rangeModifier;
            FloatRange twistAngle = new FloatRange(-math.radians(-5), math.radians(5)) * rangeModifier;

            var pelvisFrame = new BodyFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotPelvis };
            var torsoFrame = new BodyFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotTorso };
            PhysicsJoint.CreateRagdoll(pelvisFrame, torsoFrame, coneAngle.Max, perpendicularAngle, twistAngle, out var ragdoll0, out var ragdoll1);
            CreateJoint(ragdoll0, pelvis, torso);
            CreateJoint(ragdoll1, pelvis, torso);
        }

        // Legs
        {
            float thighLength = 0.32f;
            float thighRadius = 0.08f;
            CollisionFilter thighFilter = internalCollisions ? layerFilter(layer.Thigh, layer.Pelvis | layer.Calf) : groupFilter(-ragdollIndex);
            BlobAssetReference<Unity.Physics.Collider> thighCollider = Unity.Physics.CapsuleCollider.Create(new CapsuleGeometry
            {
                Vertex0 = new float3(0, -thighLength / 2, 0),
                Vertex1 = new float3(0, thighLength / 2, 0),
                Radius = thighRadius
            }, thighFilter);

            float calfLength = 0.32f;
            float calfRadius = 0.06f;
            CollisionFilter calfFilter = internalCollisions ? layerFilter(layer.Calf, layer.Thigh | layer.Foot) : groupFilter(-ragdollIndex);
            BlobAssetReference<Unity.Physics.Collider> calfCollider = Unity.Physics.CapsuleCollider.Create(new CapsuleGeometry
            {
                Vertex0 = new float3(0, -calfLength / 2, 0),
                Vertex1 = new float3(0, calfLength / 2, 0),
                Radius = calfRadius
            }, calfFilter);

            float footLength = 0.08f;
            float footRadius = 0.06f;
            CollisionFilter footFilter = internalCollisions ? layerFilter(layer.Foot, layer.Calf) : groupFilter(-ragdollIndex);
            BlobAssetReference<Unity.Physics.Collider> footCollider = Unity.Physics.CapsuleCollider.Create(new CapsuleGeometry
            {
                Vertex0 = new float3(0),
                Vertex1 = new float3(0, 0, footLength),
                Radius = footRadius
            }, footFilter);

            for (int i = 0; i < 2; i++)
            {
                float s = i * 2 - 1.0f;

                float3 thighPosition = pelvisPosition + new float3(s * pelvisLength / 2.0f, -thighLength / 2.0f, 0.0f);
                Entity thigh = CreateDynamicBody(thighPosition, quaternion.identity, thighCollider, float3.zero, float3.zero, 10.0f);
                float3 calfPosition = thighPosition + new float3(0, -(thighLength + calfLength) / 2.0f, 0);
                Entity calf = CreateDynamicBody(calfPosition, quaternion.identity, calfCollider, float3.zero, float3.zero, 5.0f);
                float3 footPosition = calfPosition + new float3(0, -calfLength / 2.0f, 0);
                Entity foot = CreateDynamicBody(footPosition, quaternion.identity, footCollider, float3.zero, float3.zero, 2.0f);

                entities.Add(thigh);
                entities.Add(calf);
                entities.Add(foot);

                // hip
                {
                    float3 pivotThigh = new float3(0, thighLength / 2.0f, 0);
                    float3 pivotPelvis = math.transform(math.inverse(GetBodyTransform(pelvis)), math.transform(GetBodyTransform(thigh), pivotThigh));
                    float3 axisLeg = new float3(0, -1, 0);
                    float3 perpendicularLeg = new float3(-s, 0, 0);
                    FloatRange coneAngle = new FloatRange(math.radians(0), math.radians(60)) * rangeModifier;
                    FloatRange perpendicularAngle = new FloatRange(math.radians(-10), math.radians(40)) * rangeModifier;
                    FloatRange twistAngle = new FloatRange(-math.radians(5), math.radians(5)) * rangeModifier;

                    var axisPelvis = math.rotate(math.inverse(GetBodyTransform(pelvis).rot), math.rotate(GetBodyTransform(thigh).rot, axisLeg));
                    axisPelvis = math.rotate(quaternion.AxisAngle(perpendicularLeg, math.radians(s * 45.0f)), axisPelvis);

                    var upperLegFrame = new BodyFrame { Axis = axisLeg, PerpendicularAxis = perpendicularLeg, Position = pivotThigh };
                    var pelvisFrame = new BodyFrame { Axis = axisPelvis, PerpendicularAxis = perpendicularLeg, Position = pivotPelvis };

                    PhysicsJoint.CreateRagdoll(upperLegFrame, pelvisFrame, coneAngle.Max, perpendicularAngle, twistAngle, out var ragdoll0, out var ragdoll1);
                    CreateJoint(ragdoll0, thigh, pelvis);
                    CreateJoint(ragdoll1, thigh, pelvis);
                }

                // knee
                {
                    float3 pivotThigh = new float3(0, -thighLength / 2.0f, 0);
                    float3 pivotCalf = math.transform(math.inverse(GetBodyTransform(calf)), math.transform(GetBodyTransform(thigh), pivotThigh));
                    float3 axis = new float3(-1, 0, 0);
                    float3 perpendicular = new float3(0, 0, 1);

                    var lowerLegFrame = new BodyFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotCalf };
                    var upperLegFrame = new BodyFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotThigh };
                    var hingeRange = new FloatRange(math.radians(-90), math.radians(0));
                    hingeRange = (hingeRange - new float2(hingeRange.Mid)) * rangeModifier + hingeRange.Mid;
                    PhysicsJoint hinge = PhysicsJoint.CreateLimitedHinge(lowerLegFrame, upperLegFrame, hingeRange);
                    CreateJoint(hinge, calf, thigh);
                }

                // ankle
                {
                    float3 pivotCalf = new float3(0, -calfLength / 2.0f, 0);
                    float3 pivotFoot = float3.zero;
                    float3 axis = new float3(-1, 0, 0);
                    float3 perpendicular = new float3(0, 0, 1);

                    var footFrame = new BodyFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotFoot };
                    var lowerLegFrame = new BodyFrame { Axis = axis, PerpendicularAxis = perpendicular, Position = pivotCalf };
                    var hingeRange = new FloatRange(math.radians(-5), math.radians(5)) * rangeModifier;
                    PhysicsJoint hinge = PhysicsJoint.CreateLimitedHinge(footFrame, lowerLegFrame, hingeRange);
                    CreateJoint(hinge, foot, calf);
                }
            }
        }

        // reposition with offset information
        if (entities.Length > 0)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];

                bool isTorso = (i == 1);
                SwapRenderMesh(e, isTorso);

                Translation positionComponent = entityManager.GetComponentData<Translation>(e);
                Rotation rotationComponent = entityManager.GetComponentData<Rotation>(e);
                PhysicsVelocity velocityComponent = entityManager.GetComponentData<PhysicsVelocity>(e);

                float3 position = positionComponent.Value;
                quaternion rotation = rotationComponent.Value;

                float3 localPosition = position - pelvisPosition;
                localPosition = math.rotate(rotationOffset, localPosition);

                position = localPosition + pelvisPosition + positionOffset;
                rotation = math.mul(rotation, rotationOffset);

                positionComponent.Value = position;
                rotationComponent.Value = rotation;

                velocityComponent.Linear = initialVelocity;

                entityManager.SetComponentData<PhysicsVelocity>(e, velocityComponent);
                entityManager.SetComponentData<Translation>(e, positionComponent);
                entityManager.SetComponentData<Rotation>(e, rotationComponent);
            }
        }
    }

    protected override void Start()
    {
        base.init();

        // Enable the joint viewer
        //         SetDebugDisplay(new Unity.Physics.Authoring.PhysicsDebugDisplayComponentData
        //         {
        //             DrawJoints = 1
        //         });


        for (int i = 0; i < numberOfRagdolls; i++)
        {
            int xOffset = (i % 2) == 0 ? -1 : 1;
            int yOffset = 2 * (i / 2);
            int xSpeed = -xOffset * 5;

            var position = new float3(xOffset * 5, yOffset, xOffset * 0.1f);
            var rotation = quaternion.Euler(math.radians(45), -xOffset * math.radians(90), 0);
            var velocity = new float3(xSpeed, math.abs(xSpeed), 0);

            position = math.transform(new RigidTransform(transform.rotation, transform.position), position);
            rotation = math.mul(transform.rotation, rotation);
            velocity = math.rotate(transform.rotation, velocity);

            CreateRagdoll(
                positionOffset: position, rotationOffset: rotation, initialVelocity: velocity,
                ragdollIndex: i + 1, rangeGain: rangeGain);
        }
    }
}
