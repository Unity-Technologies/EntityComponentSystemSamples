using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyBodyPosVelData : IComponentData
    {
        public float3 ExpectedPosition;
        public float3 ExpectedVelocity;
        public quaternion ExpectedOrientation;
        public float3 ExpectedAngularVelocity;

        public int StartAtFrame;
        public float Tolerance;

        public bool CheckPosition;
        public bool CheckVelocity;
        public bool CheckOrientation;
        public bool CheckAngularVelocity;
    }

    public class VerifyBodyPosVel : MonoBehaviour
    {
        public int StartAtFrame;
        public float Tolerance = 0.01f;

        public bool CheckPosition;
        public float3 ExpectedPosition;
        public bool CheckVelocity;
        public float3 ExpectedVelocity;
        public bool CheckOrientation;
        public float3 ExpectedAxisOfRotation;
        public float ExpectedAngle;
        public bool CheckAngularVelocity;
        public float3 ExpectedAngularVelocity;

        class VerifyBodyPosVelBaker : Baker<VerifyBodyPosVel>
        {
            public override void Bake(VerifyBodyPosVel authoring)
            {
                AddComponent(new VerifyBodyPosVelData()
                {
                    ExpectedPosition = authoring.ExpectedPosition,
                    ExpectedVelocity = authoring.ExpectedVelocity,
                    ExpectedAngularVelocity = authoring.ExpectedAngularVelocity,
                    ExpectedOrientation = quaternion.AxisAngle(authoring.ExpectedAxisOfRotation, math.radians(authoring.ExpectedAngle)),
                    StartAtFrame = authoring.StartAtFrame,
                    Tolerance = authoring.Tolerance,
                    CheckPosition = authoring.CheckPosition,
                    CheckVelocity = authoring.CheckVelocity,
                    CheckOrientation = authoring.CheckOrientation,
                    CheckAngularVelocity = authoring.CheckAngularVelocity
                });
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial class VerifyBodyPosVelSystem : SystemBase
    {
        EntityQuery m_VerificationGroup;
        int m_FrameCount;

        protected override void OnCreate()
        {
            m_FrameCount = -1;
            RequireForUpdate<VerifyBodyPosVelData>();
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyBodyPosVelData) }
            });
        }

        protected override void OnUpdate()
        {
            m_FrameCount++;
            var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                VerifyBodyPosVelData data = EntityManager.GetComponentData<VerifyBodyPosVelData>(entity);
                if (data.StartAtFrame > m_FrameCount)
                {
                    // Desired frame not reached yet
                    continue;
                }

                if (data.CheckPosition)
                {
#if !ENABLE_TRANSFORM_V1
                    var translation = EntityManager.GetComponentData<LocalTransform>(entity).Position;
#else
                    var translation = EntityManager.GetComponentData<Translation>(entity).Value;
#endif
                    Assert.IsTrue(math.distance(translation, data.ExpectedPosition) <= data.Tolerance,
                        $"{m_FrameCount}: Actual position {translation} of Entity {entity.Index} is not close enough to expected position {data.ExpectedPosition}");
                }

                if (data.CheckVelocity || data.CheckAngularVelocity)
                {
                    var vel = EntityManager.GetComponentData<PhysicsVelocity>(entity);
                    Assert.IsTrue(data.CheckVelocity ? math.distance(vel.Linear, data.ExpectedVelocity) <= data.Tolerance : true,
                        $"{m_FrameCount}: Actual linear velocity {vel.Linear} of Entity {entity.Index} is not close enough to expected one {data.ExpectedVelocity}");
                    if (data.CheckAngularVelocity)
                    {
                        var mass = EntityManager.GetComponentData<PhysicsMass>(entity);
#if !ENABLE_TRANSFORM_V1
                        quaternion rot = EntityManager.GetComponentData<LocalTransform>(entity).Rotation;
#else
                        quaternion rot = EntityManager.GetComponentData<Rotation>(entity).Value;
#endif
                        var angVelWorldSpace = vel.GetAngularVelocityWorldSpace(mass, rot);
                        var expectedRadians = math.radians(data.ExpectedAngularVelocity);
                        Assert.IsTrue(math.distance(angVelWorldSpace, expectedRadians) <= data.Tolerance,
                            $"{m_FrameCount}: Actual angular velocity {angVelWorldSpace} of Entity {entity.Index} is not close enough to expected one {expectedRadians}");
                    }
                }

                if (data.CheckOrientation)
                {
#if !ENABLE_TRANSFORM_V1
                    var rot = EntityManager.GetComponentData<LocalTransform>(entity).Rotation;
#else
                    var rot = EntityManager.GetComponentData<Rotation>(entity).Value;
#endif
                    // Before comparing orientations, make sure we properly handle 180 deg rotations around an axis (it could be 180 around axis or -180 around -axis).
                    Assert.IsTrue(math.distance(AbsIfHalfCircle(rot).value, AbsIfHalfCircle(data.ExpectedOrientation).value) <= data.Tolerance,
                        $"{m_FrameCount}: Actual orientation {rot} of Entity {entity.Index} is not close enough to expected one {data.ExpectedOrientation}");
                }
            }
            entities.Dispose();
        }

        /// <summary>
        /// If the specified quaternion is 180 degrees around some axis, returns its absolute value (abs of each component)
        /// </summary>
        private static quaternion AbsIfHalfCircle(quaternion q)
        {
            const float eps = 0.0001f;
            return ((math.abs(q.value.x) >= 1.0f - eps) || (math.abs(q.value.z) >= 1.0f - eps) || (math.abs(q.value.z) >= 1.0f - eps)) ?
                new quaternion(math.abs(q.value.x), math.abs(q.value.y), math.abs(q.value.z), q.value.w) : q;
        }
    }
}
